using Gnosis.Database.BTree;
using Gnosis.Database.Core;

namespace Gnosis.Database.Index;

public interface ISecondaryIndex
{
    string Name { get; }

    DatabaseKey IndexKey { get; }

    ValueTask<IEnumerable<DatabaseEntry>> QueryAsync(DatabaseKey secondaryKey, CancellationToken cancellationToken = default);

    ValueTask UpdateIndexAsync(DatabaseKey primaryKey, DatabaseKey secondaryKey, DatabaseValue value, CancellationToken cancellationToken = default);

    ValueTask RemoveFromIndexAsync(DatabaseKey primaryKey, DatabaseKey secondaryKey, CancellationToken cancellationToken = default);
}
