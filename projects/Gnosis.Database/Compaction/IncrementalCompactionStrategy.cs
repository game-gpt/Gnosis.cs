using Gnosis.Database.Core;

namespace Gnosis.Database.Compaction;

public sealed class IncrementalCompactionStrategy : ICompactionStrategy
{
    #region 属性

    public CompactionMode Mode => CompactionMode.Incremental;

    #endregion

    #region 公开方法

    public async ValueTask CompactAsync(ICompactionContext context, CancellationToken cancellationToken = default)
    {
        if (context.FragmentationRatio < 0.3)
        {
            return;
        }

        var pagesToReclaim = (int)(context.FreePages * 0.5);
        if (pagesToReclaim <= 0)
        {
            return;
        }

        await context.ReclaimPagesAsync(pagesToReclaim, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
