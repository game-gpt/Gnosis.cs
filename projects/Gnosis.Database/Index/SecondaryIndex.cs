using Gnosis.Database.BTree;
using Gnosis.Database.Core;

namespace Gnosis.Database.Index;

public sealed class SecondaryIndex : ISecondaryIndex
{
    #region 字段

    private readonly BTreeIndex _index;
    private readonly Dictionary<DatabaseKey, HashSet<DatabaseKey>> _secondaryToPrimary;

    #endregion

    #region 构造函数

    public SecondaryIndex(string name, DatabaseKey indexKey, int order = 128)
    {
        Name = name;
        IndexKey = indexKey;
        _index = new BTreeIndex(order);
        _secondaryToPrimary = new Dictionary<DatabaseKey, HashSet<DatabaseKey>>();
    }

    #endregion

    #region 属性

    public string Name { get; }

    public DatabaseKey IndexKey { get; }

    #endregion

    #region 公开方法

    public async ValueTask<IEnumerable<DatabaseEntry>> QueryAsync(DatabaseKey secondaryKey, CancellationToken cancellationToken = default)
    {
        var result = await _index.SearchAsync(secondaryKey, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return [];
        }

        return [new DatabaseEntry(secondaryKey, result.Value)];
    }

    public async ValueTask UpdateIndexAsync(DatabaseKey primaryKey, DatabaseKey secondaryKey, DatabaseValue value, CancellationToken cancellationToken = default)
    {
        if (!_secondaryToPrimary.TryGetValue(secondaryKey, out var primaryKeys))
        {
            primaryKeys = new HashSet<DatabaseKey>();
            _secondaryToPrimary[secondaryKey] = primaryKeys;
        }

        primaryKeys.Add(primaryKey);

        await _index.InsertAsync(secondaryKey, value, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask RemoveFromIndexAsync(DatabaseKey primaryKey, DatabaseKey secondaryKey, CancellationToken cancellationToken = default)
    {
        if (_secondaryToPrimary.TryGetValue(secondaryKey, out var primaryKeys))
        {
            primaryKeys.Remove(primaryKey);

            if (primaryKeys.Count == 0)
            {
                _secondaryToPrimary.Remove(secondaryKey);
                await _index.DeleteAsync(secondaryKey, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    #endregion
}
