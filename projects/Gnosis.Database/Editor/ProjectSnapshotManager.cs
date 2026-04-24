using System.Runtime.CompilerServices;

namespace Gnosis.Database.Editor;

public sealed class ProjectSnapshotManager
{
    #region 字段

    private readonly Core.IKvDatabase _database;
    private readonly string _keyPrefix;
    private readonly SemaphoreSlim _lock = new(1, 1);

    #endregion

    #region 构造函数

    public ProjectSnapshotManager(Core.IKvDatabase database, string keyPrefix = "snapshot:")
    {
        _database = database;
        _keyPrefix = keyPrefix;
    }

    #endregion

    #region 创建快照

    public async ValueTask<ProjectSnapshot> CreateSnapshotAsync(string name, string description, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var timestamp = DateTimeOffset.UtcNow;
            var snapshotId = $"{name}:{timestamp:yyyyMMddHHmmss}";
            var key = Core.DatabaseKey.FromString($"{_keyPrefix}meta:{snapshotId}");

            var entries = new List<SnapshotEntry>();
            var dataPrefix = Core.DatabaseKey.FromString($"{_keyPrefix}data:{snapshotId}:");

            var dbCursor = _database.Seek(Core.DatabaseKey.FromString(""));
            while (dbCursor.MoveNext())
            {
                var current = dbCursor.Current;
                var keyStr = current.Key.ToString();

                if (keyStr is not null && !keyStr.StartsWith(_keyPrefix))
                {
                    entries.Add(new SnapshotEntry
                    {
                        Key = keyStr,
                        ValueHash = ComputeHash(current.Value.Bytes.Span)
                    });
                }
            }
            dbCursor.Dispose();

            var snapshot = new ProjectSnapshot
            {
                Id = snapshotId,
                Name = name,
                Description = description,
                CreatedAt = timestamp,
                Entries = entries.ToArray()
            };

            var metaValue = Core.DatabaseValue.FromObject(snapshot);
            await _database.PutAsync(key, metaValue, ct);

            foreach (var entry in entries)
            {
                var dataKey = Core.DatabaseKey.FromString($"{_keyPrefix}data:{snapshotId}:{entry.Key}");
                var dataCursor = _database.Seek(Core.DatabaseKey.FromString(entry.Key));
                if (dataCursor.MoveNext())
                {
                    await _database.PutAsync(dataKey, dataCursor.Current.Value, ct);
                }
                dataCursor.Dispose();
            }

            return snapshot;
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 列出快照

    public async IAsyncEnumerable<ProjectSnapshot> ListSnapshotsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var prefix = Core.DatabaseKey.FromString($"{_keyPrefix}meta:");
        var cursor = _database.Seek(prefix);

        while (cursor.MoveNext())
        {
            var snapshot = cursor.Current.Value.ToObject<ProjectSnapshot>();
            if (snapshot is not null)
            {
                yield return snapshot;
            }
        }

        cursor.Dispose();
    }

    #endregion

    #region 回溯快照

    public async ValueTask RestoreSnapshotAsync(string snapshotId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var metaKey = Core.DatabaseKey.FromString($"{_keyPrefix}meta:{snapshotId}");
            var metaResult = await _database.GetAsync(metaKey, ct);
            if (!metaResult.HasValue || metaResult.Value.IsEmpty)
            {
                throw new InvalidOperationException($"快照不存在：{snapshotId}");
            }

            var snapshot = metaResult.Value.ToObject<ProjectSnapshot>();
            if (snapshot is null)
            {
                throw new InvalidOperationException($"快照数据损坏：{snapshotId}");
            }

            var dataPrefix = $"{_keyPrefix}data:{snapshotId}:";
            var dataKeyPrefix = Core.DatabaseKey.FromString(dataPrefix);
            var cursor = _database.Seek(dataKeyPrefix);

            while (cursor.MoveNext())
            {
                var current = cursor.Current;
                var keyStr = current.Key.ToString();
                if (keyStr is not null)
                {
                    var originalKey = keyStr[dataPrefix.Length..];
                    var restoreKey = Core.DatabaseKey.FromString(originalKey);
                    await _database.PutAsync(restoreKey, current.Value, ct);
                }
            }

            cursor.Dispose();
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 删除快照

    public async ValueTask<bool> DeleteSnapshotAsync(string snapshotId, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var metaKey = Core.DatabaseKey.FromString($"{_keyPrefix}meta:{snapshotId}");
            var deleted = await _database.DeleteAsync(metaKey, ct);

            if (deleted)
            {
                var dataPrefix = Core.DatabaseKey.FromString($"{_keyPrefix}data:{snapshotId}:");
                var cursor = _database.Seek(dataPrefix);
                while (cursor.MoveNext())
                {
                    await _database.DeleteAsync(cursor.Current.Key, ct);
                }
                cursor.Dispose();
            }

            return deleted;
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 内部方法

    private static ulong ComputeHash(ReadOnlySpan<byte> data)
    {
        ulong hash = 14695981039346656037;
        foreach (var b in data)
        {
            hash ^= b;
            hash *= 1099511628211;
        }
        return hash;
    }

    #endregion

    #region 内部类型

    public sealed class ProjectSnapshot
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public string Description { get; set; } = "";

        public DateTimeOffset CreatedAt { get; set; }

        public SnapshotEntry[] Entries { get; set; } = Array.Empty<SnapshotEntry>();
    }

    public sealed class SnapshotEntry
    {
        public string Key { get; set; } = "";

        public ulong ValueHash { get; set; }
    }

    #endregion
}
