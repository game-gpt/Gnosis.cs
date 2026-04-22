using Gnosis.Database.Core;

namespace Gnosis.Database.Compaction;

public interface ICompactionContext
{
    long TotalPages { get; }

    long FreePages { get; }

    long UsedPages { get; }

    double FragmentationRatio => TotalPages > 0 ? (double)FreePages / TotalPages : 0;

    ValueTask MarkPageFreeAsync(PageId pageId, CancellationToken cancellationToken = default);

    ValueTask ReclaimPagesAsync(int count, CancellationToken cancellationToken = default);
}
