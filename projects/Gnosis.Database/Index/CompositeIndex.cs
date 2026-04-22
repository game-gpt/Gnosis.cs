using Gnosis.Database.BTree;
using Gnosis.Database.Core;

namespace Gnosis.Database.Index;

public sealed class CompositeIndex
{
    #region 字段

    private readonly BTreeIndex _index;
    private readonly string[] _fieldNames;

    #endregion

    #region 构造函数

    public CompositeIndex(string name, string[] fieldNames, int order = 128)
    {
        Name = name;
        _fieldNames = fieldNames;
        _index = new BTreeIndex(order);
    }

    #endregion

    #region 属性

    public string Name { get; }

    public IReadOnlyList<string> FieldNames => _fieldNames;

    #endregion

    #region 公开方法

    public async ValueTask<IEnumerable<DatabaseEntry>> QueryAsync(DatabaseKey compositeKey, CancellationToken cancellationToken = default)
    {
        var result = await _index.SearchAsync(compositeKey, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return [];
        }

        return [new DatabaseEntry(compositeKey, result.Value)];
    }

    public async ValueTask<IEnumerable<DatabaseEntry>> RangeQueryAsync(DatabaseKey start, DatabaseKey end, CancellationToken cancellationToken = default)
    {
        var results = new List<DatabaseEntry>();
        var cursor = _index.CreateCursor();

        if (!cursor.Seek(start))
        {
            return results;
        }

        while (cursor.IsValid)
        {
            var current = cursor.Current;
            var cmp = current.Key.CompareTo(end);
            if (cmp > 0)
            {
                break;
            }

            results.Add(new DatabaseEntry(current.Key, current.Value));

            if (!cursor.MoveNext())
            {
                break;
            }
        }

        return results;
    }

    public async ValueTask UpdateIndexAsync(DatabaseKey compositeKey, DatabaseValue value, CancellationToken cancellationToken = default)
    {
        await _index.InsertAsync(compositeKey, value, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask RemoveFromIndexAsync(DatabaseKey compositeKey, CancellationToken cancellationToken = default)
    {
        await _index.DeleteAsync(compositeKey, cancellationToken).ConfigureAwait(false);
    }

    public static DatabaseKey BuildCompositeKey(params DatabaseKey[] keys)
    {
        var totalLength = 0;
        foreach (var key in keys)
        {
            totalLength += 4 + key.Length;
        }

        var bytes = new byte[totalLength];
        var offset = 0;

        foreach (var key in keys)
        {
            var lengthBytes = BitConverter.GetBytes(key.Length);
            Array.Copy(lengthBytes, 0, bytes, offset, 4);
            offset += 4;

            key.Bytes.Span.CopyTo(bytes.AsSpan(offset, key.Length));
            offset += key.Length;
        }

        return new DatabaseKey(bytes);
    }

    #endregion
}
