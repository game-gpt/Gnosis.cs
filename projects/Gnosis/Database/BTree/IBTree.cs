namespace Gnosis.Database.BTree;

public interface IBTree : IDisposable
{
    int Order { get; }

    int Height { get; }

    long Count { get; }

    ValueTask<bool> InsertAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default);

    ValueTask<DatabaseValue?> SearchAsync(DatabaseKey key, CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default);

    IBTreeCursor CreateCursor();

    ValueTask<(bool Success, BTreeSplitResult Result)> TrySplitAsync(DatabaseKey key, CancellationToken cancellationToken = default);

    ValueTask<(bool Success, BTreeMergeResult Result)> TryMergeAsync(DatabaseKey key, CancellationToken cancellationToken = default);
}
