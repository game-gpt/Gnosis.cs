using Gnosis.Database.Core;
using Gnosis.Database.PageManager;

namespace Gnosis.Database.Cache;

public sealed class PageReadAhead : IDisposable
{
    #region 字段

    private readonly IPageManager _pageManager;
    private readonly PageCache _pageCache;
    private readonly int _readAheadSize;
    private PageId _lastSequentialPageId;
    private int _sequentialAccessCount;
    private bool _disposed;

    #endregion

    #region 构造函数

    public PageReadAhead(IPageManager pageManager, PageCache pageCache, int readAheadSize = 8)
    {
        _pageManager = pageManager;
        _pageCache = pageCache;
        _readAheadSize = readAheadSize;
        _lastSequentialPageId = PageId.Invalid;
        _sequentialAccessCount = 0;
    }

    #endregion

    #region 公开方法

    public async ValueTask<Page?> GetPageAsync(PageId pageId, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        DetectSequentialAccess(pageId);

        var cached = _pageCache.Get(pageId);
        if (cached is not null)
        {
            return cached.Value;
        }

        var page = await _pageManager.ReadPageAsync(pageId, cancellationToken).ConfigureAwait(false);
        _pageCache.Put(pageId, page);

        if (_sequentialAccessCount >= 3)
        {
            _ = PrefetchAsync(pageId, cancellationToken);
        }

        return page;
    }

    #endregion

    #region 私有方法

    private void DetectSequentialAccess(PageId pageId)
    {
        if (_lastSequentialPageId.Value >= 0 && pageId.Value == _lastSequentialPageId.Value + 1)
        {
            _sequentialAccessCount++;
        }
        else
        {
            _sequentialAccessCount = 0;
        }

        _lastSequentialPageId = pageId;
    }

    private async ValueTask PrefetchAsync(PageId startPageId, CancellationToken cancellationToken)
    {
        for (var i = 1; i <= _readAheadSize; i++)
        {
            var prefetchId = new PageId(startPageId.Value + i);

            if (_pageCache.Get(prefetchId) is not null)
            {
                continue;
            }

            try
            {
                var page = await _pageManager.ReadPageAsync(prefetchId, cancellationToken).ConfigureAwait(false);
                _pageCache.Put(prefetchId, page);
            }
            catch (ArgumentOutOfRangeException)
            {
                break;
            }
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _disposed = true;
    }

    #endregion
}
