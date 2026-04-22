using Gnosis.Database.BTree;
using Gnosis.Database.Core;

namespace Gnosis.Database.Transaction;

public sealed class DatabaseSnapshot : ISnapshot
{
    #region 字段

    private readonly BTreeIndex _btree;
    private bool _disposed;

    #endregion

    #region 构造函数

    internal DatabaseSnapshot(BTreeIndex btree, SequenceNumber sequence)
    {
        _btree = btree;
        Sequence = sequence;
    }

    #endregion

    #region 属性

    public SequenceNumber Sequence { get; }

    #endregion

    #region 公开方法

    public ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _btree.SearchAsync(key, cancellationToken);
    }

    public ICursor Seek(DatabaseKey key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var btreeCursor = _btree.CreateCursor();
        btreeCursor.Seek(key);
        return new SnapshotCursor(btreeCursor);
    }

    public ISnapshot CreateChild()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new DatabaseSnapshot(_btree, Sequence);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _disposed = true;
    }

    #endregion

    #region 嵌套类型

    private sealed class SnapshotCursor(IBTreeCursor inner) : ICursor
    {
        public DatabaseEntry Current => inner.Current;

        public bool IsValid => inner.IsValid;

        public bool MoveNext()
        {
            return inner.MoveNext();
        }

        public bool MovePrev()
        {
            return inner.MovePrev();
        }

        public bool SeekToFirst()
        {
            return inner.SeekToFirst();
        }

        public bool SeekToLast()
        {
            return inner.SeekToLast();
        }

        public bool Seek(DatabaseKey key)
        {
            return inner.Seek(key);
        }

        public IReadOnlyList<DatabaseEntry> GetRange(DatabaseKey start, DatabaseKey end, int limit = 1000)
        {
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

        public void Dispose()
        {
            inner.Dispose();
        }
    }

    #endregion
}
