using Gnosis.Database.BTree;
using Gnosis.Database.Core;
using Gnosis.Database.WAL;

namespace Gnosis.Database.Transaction;

public sealed class TransactionManager
{
    #region 字段

    private readonly BTreeIndex _btree;
    private readonly IWriteAheadLog _wal;
    private readonly List<DatabaseTransaction> _activeTransactions;
    private readonly object _lock = new();
    private SequenceNumber _currentSequence;

    #endregion

    #region 构造函数

    public TransactionManager(BTreeIndex btree, IWriteAheadLog wal)
    {
        _btree = btree;
        _wal = wal;
        _activeTransactions = [];
        _currentSequence = SequenceNumber.Zero;
    }

    #endregion

    #region 属性

    public int ActiveTransactionCount
    {
        get
        {
            lock (_lock)
            {
                return _activeTransactions.Count;
            }
        }
    }

    public SequenceNumber CurrentSequence => _currentSequence;

    #endregion

    #region 公开方法

    public DatabaseTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot)
    {
        lock (_lock)
        {
            var txn = new DatabaseTransaction(
                this,
                TransactionId.New(),
                isolationLevel,
                Timestamp.Now,
                _currentSequence);

            _activeTransactions.Add(txn);
            return txn;
        }
    }

    public DatabaseSnapshot CreateSnapshot()
    {
        lock (_lock)
        {
            return new DatabaseSnapshot(_btree, _currentSequence);
        }
    }

    internal async ValueTask CommitAsync(DatabaseTransaction transaction, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _activeTransactions.Remove(transaction);
        }

        var commitEntry = new WalEntry(
            _currentSequence,
            transaction.Id,
            WalEntryType.Commit,
            DatabaseKey.Empty,
            DatabaseValue.Empty,
            0);

        await _wal.AppendAsync(commitEntry, cancellationToken).ConfigureAwait(false);
        _currentSequence = _currentSequence.Next;

        await ApplyPendingWritesAsync(transaction, cancellationToken).ConfigureAwait(false);
    }

    internal void Rollback(DatabaseTransaction transaction)
    {
        lock (_lock)
        {
            _activeTransactions.Remove(transaction);
        }

        transaction.MarkRolledBack();
    }

    internal async ValueTask<DatabaseValue?> GetAsync(DatabaseTransaction transaction, DatabaseKey key, CancellationToken cancellationToken = default)
    {
        var pendingValue = transaction.GetPendingWrite(key);
        if (pendingValue is not null)
        {
            if (pendingValue.Value.IsEmpty)
            {
                return null;
            }

            return pendingValue;
        }

        if (transaction.DeletedKeys.Contains(key))
        {
            return null;
        }

        var result = await _btree.SearchAsync(key, cancellationToken).ConfigureAwait(false);
        return result;
    }

    internal ValueTask PutAsync(DatabaseTransaction transaction, DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default)
    {
        transaction.AddPendingWrite(key, value);
        return ValueTask.CompletedTask;
    }

    internal ValueTask<bool> DeleteAsync(DatabaseTransaction transaction, DatabaseKey key, CancellationToken cancellationToken = default)
    {
        transaction.AddPendingDelete(key);
        return ValueTask.FromResult(true);
    }

    #endregion

    #region 私有方法

    private async ValueTask ApplyPendingWritesAsync(DatabaseTransaction transaction, CancellationToken cancellationToken = default)
    {
        foreach (var (key, value) in transaction.PendingWrites)
        {
            if (transaction.DeletedKeys.Contains(key))
            {
                continue;
            }

            var walEntry = new WalEntry(
                _currentSequence,
                transaction.Id,
                WalEntryType.Put,
                key,
                value,
                0);

            await _wal.AppendAsync(walEntry, cancellationToken).ConfigureAwait(false);
            _currentSequence = _currentSequence.Next;

            await _btree.InsertAsync(key, value, cancellationToken).ConfigureAwait(false);
        }

        foreach (var key in transaction.DeletedKeys)
        {
            var walEntry = new WalEntry(
                _currentSequence,
                transaction.Id,
                WalEntryType.Delete,
                key,
                DatabaseValue.Empty,
                0);

            await _wal.AppendAsync(walEntry, cancellationToken).ConfigureAwait(false);
            _currentSequence = _currentSequence.Next;

            await _btree.DeleteAsync(key, cancellationToken).ConfigureAwait(false);
        }
    }

    #endregion
}
