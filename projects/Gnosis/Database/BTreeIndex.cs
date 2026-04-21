using Gnosis.Database.BTree;

namespace Gnosis.Database;

public sealed class BTreeIndex : IBTree
{
    private long _count = 0;

    public BTreeIndex(int order)
    {
        Order = order;
    }

    public int Order { get; }

    public int Height => throw new NotImplementedException();

    public long Count => _count;

    public IBTreeCursor CreateCursor()
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> InsertAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<DatabaseValue?> SearchAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<(bool Success, BTreeMergeResult Result)> TryMergeAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<(bool Success, BTreeSplitResult Result)> TrySplitAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }
}
