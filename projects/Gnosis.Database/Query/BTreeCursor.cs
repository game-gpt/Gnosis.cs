using Gnosis.Database.BTree;
using Gnosis.Database.Core;

namespace Gnosis.Database.Query;

public sealed class BTreeCursor : ICursor
{
    #region 字段

    private readonly IBTreeCursor _inner;
    private bool _disposed;

    #endregion

    #region 构造函数

    public BTreeCursor(IBTreeCursor inner)
    {
        _inner = inner;
    }

    #endregion

    #region 属性

    public DatabaseEntry Current => _inner.Current;

    public bool IsValid => _inner.IsValid;

    #endregion

    #region 公开方法

    public bool MoveNext()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _inner.MoveNext();
    }

    public bool MovePrev()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _inner.MovePrev();
    }

    public bool SeekToFirst()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _inner.SeekToFirst();
    }

    public bool SeekToLast()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _inner.SeekToLast();
    }

    public bool Seek(DatabaseKey key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _inner.Seek(key);
    }

    public IReadOnlyList<DatabaseEntry> GetRange(DatabaseKey start, DatabaseKey end, int limit = 1000)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var results = new List<DatabaseEntry>();

        if (!Seek(start))
        {
            return results;
        }

        while (IsValid && results.Count < limit)
        {
            var cmp = Current.Key.CompareTo(end);
            if (cmp > 0)
            {
                break;
            }

            results.Add(Current);

            if (!MoveNext())
            {
                break;
            }
        }

        return results;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _inner.Dispose();
        _disposed = true;
    }

    #endregion
}
