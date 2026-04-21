using Gnosis.Database.BTree;
using Gnosis.Database.Core;
using Gnosis.Database.SHM;
using Gnosis.Database.WAL;

namespace Gnosis.Database;

public sealed class GenesisKvDatabase : IKvDatabase
{
    private readonly IBTree _btree;
    private readonly IWriteAheadLog _wal;
    private readonly ISharedMemory _shm;
    private DatabaseStatistics _statistics;

    public GenesisKvDatabase(IBTree btree, IWriteAheadLog wal, ISharedMemory shm, DatabaseOptions options)
    {
        _btree = btree;
        _wal = wal;
        _shm = shm;
        _statistics = DatabaseStatistics.Zero;
        Options = options;
    }

    public DatabaseOptions Options { get; }

    public DatabaseStatistics Statistics => _statistics;

    public ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot)
    {
        throw new NotImplementedException();
    }

    public ISnapshot CreateSnapshot()
    {
        throw new NotImplementedException();
    }

    public ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask PutAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> ExistsAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ICursor Seek(DatabaseKey key)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _btree.Dispose();
        _wal.Dispose();
        _shm.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
