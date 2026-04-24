using System.Runtime.CompilerServices;

namespace Gnosis.Database.Editor;

public sealed class IncrementalHistory
{
    #region 字段

    private readonly Core.IKvDatabase _database;
    private readonly string _keyPrefix;
    private readonly List<HistoryDelta> _pendingDeltas;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private byte[]? _lastSnapshot;
    private long _currentVersion;

    #endregion

    #region 构造函数

    public IncrementalHistory(Core.IKvDatabase database, string keyPrefix = "inc-history:")
    {
        _database = database;
        _keyPrefix = keyPrefix;
        _pendingDeltas = new List<HistoryDelta>();
        _currentVersion = 0;
    }

    #endregion

    #region 属性

    public long CurrentVersion => _currentVersion;

    #endregion

    #region 记录变更

    public async ValueTask RecordChangeAsync(string path, byte[] before, byte[] after, CancellationToken ct = default)
    {
        var delta = ComputeDelta(path, before, after);

        await _lock.WaitAsync(ct);
        try
        {
            _pendingDeltas.Add(delta);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask FlushAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_pendingDeltas.Count == 0) return;

            var version = Interlocked.Increment(ref _currentVersion);
            var key = Core.DatabaseKey.FromString($"{_keyPrefix}v{version:D20}");

            var batch = new HistoryBatch
            {
                Version = version,
                Timestamp = DateTimeOffset.UtcNow,
                Deltas = _pendingDeltas.ToArray()
            };

            var value = Core.DatabaseValue.FromObject(batch);
            await _database.PutAsync(key, value, ct);

            _pendingDeltas.Clear();
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 回放

    public async IAsyncEnumerable<HistoryDelta> ReplayAsync(
        long fromVersion = 0, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var prefix = Core.DatabaseKey.FromString(_keyPrefix);
        var cursor = _database.Seek(prefix);

        while (cursor.MoveNext())
        {
            var batch = cursor.Current.Value.ToObject<HistoryBatch>();
            if (batch is not null && batch.Version >= fromVersion)
            {
                foreach (var delta in batch.Deltas)
                {
                    yield return delta;
                }
            }
        }

        cursor.Dispose();
    }

    #endregion

    #region 差异计算

    private static HistoryDelta ComputeDelta(string path, byte[] before, byte[] after)
    {
        if (before.Length == 0)
        {
            return new HistoryDelta
            {
                Path = path,
                Operation = DeltaOperation.Add,
                Offset = 0,
                Data = after
            };
        }

        if (after.Length == 0)
        {
            return new HistoryDelta
            {
                Path = path,
                Operation = DeltaOperation.Remove,
                Offset = 0,
                Data = Array.Empty<byte>()
            };
        }

        var commonPrefix = 0;
        var minLen = Math.Min(before.Length, after.Length);
        while (commonPrefix < minLen && before[commonPrefix] == after[commonPrefix])
        {
            commonPrefix++;
        }

        var commonSuffix = 0;
        var maxSuffix = Math.Min(before.Length - commonPrefix, after.Length - commonPrefix);
        while (commonSuffix < maxSuffix && before[before.Length - 1 - commonSuffix] == after[after.Length - 1 - commonSuffix])
        {
            commonSuffix++;
        }

        var changedData = after.AsSpan(commonPrefix, after.Length - commonPrefix - commonSuffix).ToArray();

        return new HistoryDelta
        {
            Path = path,
            Operation = DeltaOperation.Modify,
            Offset = commonPrefix,
            OldLength = before.Length - commonPrefix - commonSuffix,
            Data = changedData
        };
    }

    #endregion

    #region 内部类型

    public sealed class HistoryDelta
    {
        public string Path { get; set; } = "";

        public DeltaOperation Operation { get; set; }

        public int Offset { get; set; }

        public int OldLength { get; set; }

        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    public sealed class HistoryBatch
    {
        public long Version { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public HistoryDelta[] Deltas { get; set; } = Array.Empty<HistoryDelta>();
    }

    public enum DeltaOperation
    {
        Add,
        Modify,
        Remove
    }

    #endregion
}
