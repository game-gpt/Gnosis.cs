using GnosisDatabaseCore = Gnosis.Database.Core;

namespace Gnosis.Database.Editor;

public sealed class EditorCache
{
    #region 字段

    private readonly GnosisDatabaseCore.IKvDatabase _database;
    private readonly EditorCacheOptions _options;
    private readonly Dictionary<GnosisDatabaseCore.DatabaseKey, CacheEntry> _hotCache;
    private readonly Dictionary<GnosisDatabaseCore.DatabaseKey, CacheEntry> _warmCache;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private long _cacheHits;
    private long _cacheMisses;

    #endregion

    #region 构造函数

    public EditorCache(GnosisDatabaseCore.IKvDatabase database, EditorCacheOptions? options = null)
    {
        _database = database;
        _options = options ?? EditorCacheOptions.Default;
        _hotCache = new Dictionary<GnosisDatabaseCore.DatabaseKey, CacheEntry>();
        _warmCache = new Dictionary<GnosisDatabaseCore.DatabaseKey, CacheEntry>();
    }

    #endregion

    #region 属性

    public long CacheHits => _cacheHits;

    public long CacheMisses => _cacheMisses;

    public double HitRate => (_cacheHits + _cacheMisses) > 0 ? (double)_cacheHits / (_cacheHits + _cacheMisses) : 0;

    #endregion

    #region 读取

    public async ValueTask<GnosisDatabaseCore.DatabaseValue> GetAsync(
        GnosisDatabaseCore.DatabaseKey key, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_hotCache.TryGetValue(key, out var hotEntry) && !hotEntry.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                return hotEntry.Value;
            }

            if (_warmCache.TryGetValue(key, out var warmEntry) && !warmEntry.IsExpired)
            {
                Interlocked.Increment(ref _cacheHits);
                PromoteToHot(key, warmEntry);
                return warmEntry.Value;
            }
        }
        finally
        {
            _lock.Release();
        }

        Interlocked.Increment(ref _cacheMisses);

        var dbValue = await _database.GetAsync(key, ct);
        if (dbValue is not null)
        {
            await PutToCacheAsync(key, dbValue);
        }

        return dbValue;
    }

    #endregion

    #region 写入

    public async ValueTask PutAsync(GnosisDatabaseCore.DatabaseKey key,
        GnosisDatabaseCore.DatabaseValue value, CancellationToken ct = default)
    {
        await _database.PutAsync(key, value, ct);
        await PutToCacheAsync(key, value);
    }

    #endregion

    #region 删除

    public async ValueTask<bool> DeleteAsync(GnosisDatabaseCore.DatabaseKey key, CancellationToken ct = default)
    {
        var deleted = await _database.DeleteAsync(key, ct);

        if (deleted)
        {
            await _lock.WaitAsync(ct);
            try
            {
                _hotCache.Remove(key);
                _warmCache.Remove(key);
            }
            finally
            {
                _lock.Release();
            }
        }

        return deleted;
    }

    #endregion

    #region 失效

    public async ValueTask InvalidateAsync(GnosisDatabaseCore.DatabaseKey key, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _hotCache.Remove(key);
            _warmCache.Remove(key);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask InvalidatePrefixAsync(GnosisDatabaseCore.DatabaseKey prefix, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var hotToRemove = _hotCache.Keys.Where(k => k.StartsWith(prefix)).ToList();
            var warmToRemove = _warmCache.Keys.Where(k => k.StartsWith(prefix)).ToList();

            foreach (var key in hotToRemove) _hotCache.Remove(key);
            foreach (var key in warmToRemove) _warmCache.Remove(key);
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 预加载

    public async ValueTask PreloadAsync(CancellationToken ct = default)
    {
        if (!_options.EnablePreload) return;

        foreach (var prefix in _options.PreloadPrefixes)
        {
            var dbKey = GnosisDatabaseCore.DatabaseKey.FromString(prefix);
            var cursor = _database.Seek(dbKey);

            while (cursor.MoveNext())
            {
                var entry = cursor.Current;
                await PutToCacheAsync(entry.Key, entry.Value);
            }

            cursor.Dispose();
        }
    }

    #endregion

    #region 内部方法

    private async ValueTask PutToCacheAsync(GnosisDatabaseCore.DatabaseKey key,
        GnosisDatabaseCore.DatabaseValue value)
    {
        var entry = new CacheEntry(value, DateTimeOffset.UtcNow.Add(_options.HotKeyTtl));

        await _lock.WaitAsync();
        try
        {
            if (_hotCache.Count >= _options.MaxHotEntries)
            {
                DemoteColdest();
            }

            _hotCache[key] = entry;
            _warmCache.Remove(key);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void PromoteToHot(GnosisDatabaseCore.DatabaseKey key, CacheEntry entry)
    {
        if (_hotCache.Count >= _options.MaxHotEntries)
        {
            DemoteColdest();
        }

        _hotCache[key] = entry with { ExpiresAt = DateTimeOffset.UtcNow.Add(_options.HotKeyTtl) };
        _warmCache.Remove(key);
    }

    private void DemoteColdest()
    {
        if (_hotCache.Count == 0) return;

        var oldest = _hotCache.OrderBy(kvp => kvp.Value.LastAccess).First();

        if (_warmCache.Count >= _options.MaxWarmEntries)
        {
            var warmOldest = _warmCache.OrderBy(kvp => kvp.Value.LastAccess).First();
            _warmCache.Remove(warmOldest.Key);
        }

        _warmCache[oldest.Key] = oldest.Value;
        _hotCache.Remove(oldest.Key);
    }

    #endregion

    #region 内部类型

    private sealed record CacheEntry(GnosisDatabaseCore.DatabaseValue Value, DateTimeOffset ExpiresAt)
    {
        public DateTimeOffset LastAccess { get; init; } = DateTimeOffset.UtcNow;

        public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;
    }

    #endregion
}
