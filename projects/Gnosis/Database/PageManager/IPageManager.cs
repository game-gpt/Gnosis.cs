using Gnosis.Database.Core;

namespace Gnosis.Database.PageManager;

public interface IPageManager : IDisposable, IAsyncDisposable
{
    int PageSize { get; }

    long TotalPages { get; }

    ValueTask<PageId> AllocatePageAsync(CancellationToken cancellationToken = default);

    ValueTask<Page> ReadPageAsync(PageId pageId, CancellationToken cancellationToken = default);

    ValueTask WritePageAsync(PageId pageId, Page page, CancellationToken cancellationToken = default);

    ValueTask FreePageAsync(PageId pageId, CancellationToken cancellationToken = default);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);

    IFreePageList FreeList { get; }
}
