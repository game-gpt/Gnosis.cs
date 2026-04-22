using Gnosis.Core;
using Gnosis.Infrastructure;

namespace Gnosis.Database.Core;

public interface ITransaction : IDisposable
{
    TransactionId Id { get; }

    IsolationLevel IsolationLevel { get; }

    Timestamp StartTime { get; }

    bool IsReadOnly { get; }

    bool IsCommitted { get; }

    bool IsRolledBack { get; }

    ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default);

    ValueTask PutAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default);

    ValueTask CommitAsync(CancellationToken cancellationToken = default);

    void Rollback();
}
