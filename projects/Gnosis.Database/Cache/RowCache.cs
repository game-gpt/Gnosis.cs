using Gnosis.Database.Core;

namespace Gnosis.Database.Cache;

public sealed class RowCache : IDisposable
{
    #region 字段

    private readonly int _maxEntryCount;
    private readonly Dictionary<DatabaseKey, CachedRow> _cache;
    private readonly LinkedList<DatabaseKey> _lruList;
    private readonly object _lock = new();
    private long _hits;
    private long _misses;
    private bool _disposed;

    #endregion

    #region 构造函数

    public RowCache(int maxEntryCount = 65536)
    {
        _maxEntryCount = maxEntryCount;
        _cache = new Dictionary<DatabaseKey, CachedRow>(maxEntryCount);
        _lruList = new LinkedList<DatabaseKey>();
    }

    #endregion

    #region 属性

    public int EntryCount
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    public double HitRate
    {
        get
        {
            var total = _hits + _misses;
            return total > 0 ? (double)_hits / total : 0;
        }
    }

    #endregion

    #region 公开方法

    public DatabaseValue? Get(DatabaseKey key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                if (cached.IsExpired)
                {
                    RemoveInternal(key);
                    _misses++;
                    return null;
                }

                _lruList.Remove(cached.LruNode);
                _lruList.AddFirst(cached.LruNode);
                _hits++;
                return cached.Value;
            }

            _misses++;
            return null;
        }
    }

    public void Put(DatabaseKey key, DatabaseValue value, TimeSpan? ttl = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var existing))
            {
                _lruList.Remove(existing.LruNode);
                var node = _lruList.AddFirst(key);
                _cache[key] = new CachedRow(value, node, ttl.HasValue ? new Timestamp(Timestamp.Now.Value.Add(ttl.Value)) : null);
                return;
            }

            EvictIfNeeded();

            var lruNode = _lruList.AddFirst(key);
            _cache[key] = new CachedRow(value, lruNode, ttl.HasValue ? new Timestamp(Timestamp.Now.Value.Add(ttl.Value)) : null);
        }
    }

    public void Invalidate(DatabaseKey key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            RemoveInternal(key);
        }
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _cache.Clear();
            _lruList.Clear();
            _hits = 0;
            _misses = 0;
        }
    }

    #endregion

    #region 私有方法

    private void RemoveInternal(DatabaseKey key)
    {
        if (_cache.Remove(key, out var cached))
        {
            _lruList.Remove(cached.LruNode);
        }
    }

    private void EvictIfNeeded()
    {
        while (_cache.Count >= _maxEntryCount && _lruList.Count > 0)
        {
            var last = _lruList.Last!;
            var key = last.Value;
            _lruList.RemoveLast();
            _cache.Remove(key);
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _cache.Clear();
            _lruList.Clear();
        }

        _disposed = true;
    }

    #endregion

    #region 嵌套类型

    private readonly record struct CachedRow(
        DatabaseValue Value,
        LinkedListNode<DatabaseKey> LruNode,
        Timestamp? ExpiresAt)
    {
        public bool IsExpired => ExpiresAt.HasValue && Timestamp.Now > ExpiresAt.Value;
    }

    #endregion
}
