using Gnosis.Database.Core;

namespace Gnosis.Database.Compaction;

public interface ICompactionStrategy
{
    CompactionMode Mode { get; }

    ValueTask CompactAsync(ICompactionContext context, CancellationToken cancellationToken = default);
}
