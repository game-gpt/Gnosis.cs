namespace Gnosis.Database.Core;

public interface IKvDatabase : IDisposable, IAsyncDisposable
{
    ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot);

    ISnapshot CreateSnapshot();

    ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default);

    ValueTask PutAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default);

    ValueTask<bool> ExistsAsync(DatabaseKey key, CancellationToken cancellationToken = default);

    ICursor Seek(DatabaseKey key);

    DatabaseOptions Options { get; }

    DatabaseStatistics Statistics { get; }
}
