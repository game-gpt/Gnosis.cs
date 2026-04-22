using Gnosis.Database.Core;
using Gnosis.Database.PageManager;

namespace Gnosis.Database.Cache;

public sealed class PageCache : IDisposable
{
    #region 字段

    private readonly int _maxPageCount;
    private readonly int _pageSize;
    private readonly Dictionary<PageId, CachedPage> _cache;
    private readonly LinkedList<PageId> _lruList;
    private readonly object _lock = new();
    private bool _disposed;

    #endregion

    #region 构造函数

    public PageCache(int maxPageCount, int pageSize = 4096)
    {
        _maxPageCount = maxPageCount;
        _pageSize = pageSize;
        _cache = new Dictionary<PageId, CachedPage>(maxPageCount);
        _lruList = new LinkedList<PageId>();
    }

    #endregion

    #region 属性

    public int CachedPageCount
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    public long UsedMemory
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count * _pageSize;
            }
        }
    }

    #endregion

    #region 公开方法

    public Page? Get(PageId pageId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (_cache.TryGetValue(pageId, out var cached))
            {
                _lruList.Remove(cached.LruNode);
                _lruList.AddFirst(cached.LruNode);
                return cached.Page;
            }

            return null;
        }
    }

    public void Put(PageId pageId, Page page)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (_cache.TryGetValue(pageId, out var existing))
            {
                _lruList.Remove(existing.LruNode);
                _lruList.AddFirst(existing.LruNode);
                _cache[pageId] = new CachedPage(page, _lruList.First!);
                return;
            }

            EvictIfNeeded();

            var node = _lruList.AddFirst(pageId);
            _cache[pageId] = new CachedPage(page, node);
        }
    }

    public void Invalidate(PageId pageId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            if (_cache.Remove(pageId, out var cached))
            {
                _lruList.Remove(cached.LruNode);
            }
        }
    }

    public void Clear()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _cache.Clear();
            _lruList.Clear();
        }
    }

    #endregion

    #region 私有方法

    private void EvictIfNeeded()
    {
        while (_cache.Count >= _maxPageCount && _lruList.Count > 0)
        {
            var last = _lruList.Last!;
            var pageId = last.Value;
            _lruList.RemoveLast();
            _cache.Remove(pageId);
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

    private readonly record struct CachedPage(Page Page, LinkedListNode<PageId> LruNode);

    #endregion
}
