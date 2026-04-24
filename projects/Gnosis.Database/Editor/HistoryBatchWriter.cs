using System.Runtime.CompilerServices;
using GnosisDatabaseCore = Gnosis.Database.Core;

namespace Gnosis.Database.Editor;

public sealed class HistoryBatchWriter
{
    #region 字段

    private readonly GnosisDatabaseCore.IKvDatabase _database;
    private readonly List<HistoryEntry> _pendingEntries;
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private readonly int _batchSize;
    private readonly string _keyPrefix;

    #endregion

    #region 构造函数

    public HistoryBatchWriter(GnosisDatabaseCore.IKvDatabase database, string keyPrefix = "history:", int batchSize = 100)
    {
        _database = database;
        _keyPrefix = keyPrefix;
        _batchSize = batchSize;
        _pendingEntries = new List<HistoryEntry>(batchSize);
    }

    #endregion

    #region 追加

    public async ValueTask AppendAsync(string operationType, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        var entry = new HistoryEntry
        {
            Sequence = DateTime.UtcNow.Ticks,
            OperationType = operationType,
            Data = data
        };

        _pendingEntries.Add(entry);

        if (_pendingEntries.Count >= _batchSize)
        {
            await FlushAsync(ct);
        }
    }

    #endregion

    #region 刷盘

    public async ValueTask FlushAsync(CancellationToken ct = default)
    {
        if (_pendingEntries.Count == 0) return;

        await _flushLock.WaitAsync(ct);
        try
        {
            using var tx = _database.BeginTransaction();

            foreach (var entry in _pendingEntries)
            {
                var key = GnosisDatabaseCore.DatabaseKey.FromString($"{_keyPrefix}{entry.Sequence:D20}");
                var value = GnosisDatabaseCore.DatabaseValue.FromObject(entry);
                await tx.PutAsync(key, value, ct);
            }

            await tx.CommitAsync(ct);
            _pendingEntries.Clear();
        }
        finally
        {
            _flushLock.Release();
        }
    }

    #endregion

    #region 查询

    public async IAsyncEnumerable<HistoryEntry> ReadHistoryAsync(
        int maxEntries = 1000, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var prefix = GnosisDatabaseCore.DatabaseKey.FromString(_keyPrefix);
        var cursor = _database.Seek(prefix);
        var count = 0;

        while (cursor.MoveNext() && count < maxEntries)
        {
            var current = cursor.Current;
            var entry = current.Value.ToObject<HistoryEntry>();
            if (entry is not null)
            {
                yield return entry;
                count++;
            }
        }

        cursor.Dispose();
    }

    #endregion

    #region 内部类型

    public sealed class HistoryEntry
    {
        public long Sequence { get; set; }

        public string OperationType { get; set; } = "";

        public ReadOnlyMemory<byte> Data { get; set; }
    }

    #endregion
}
