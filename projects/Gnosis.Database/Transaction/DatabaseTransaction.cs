using Gnosis.Database.Core;

namespace Gnosis.Database.Transaction;

public sealed class DatabaseTransaction : ITransaction
{
    #region 字段

    private readonly TransactionManager _manager;
    private readonly Dictionary<DatabaseKey, DatabaseValue> _pendingWrites;
    private readonly HashSet<DatabaseKey> _deletedKeys;
    private bool _disposed;

    #endregion

    #region 构造函数

    internal DatabaseTransaction(
        TransactionManager manager,
        TransactionId id,
        IsolationLevel isolationLevel,
        Timestamp startTime,
        SequenceNumber snapshotSequence)
    {
        _manager = manager;
        _pendingWrites = [];
        _deletedKeys = [];
        Id = id;
        IsolationLevel = isolationLevel;
        StartTime = startTime;
        SnapshotSequence = snapshotSequence;
        IsReadOnly = false;
        IsCommitted = false;
        IsRolledBack = false;
    }

    #endregion

    #region 属性

    public TransactionId Id { get; }

    public IsolationLevel IsolationLevel { get; }

    public Timestamp StartTime { get; }

    public bool IsReadOnly { get; }

    public bool IsCommitted { get; private set; }

    public bool IsRolledBack { get; private set; }

    internal SequenceNumber SnapshotSequence { get; }

    internal IReadOnlyDictionary<DatabaseKey, DatabaseValue> PendingWrites => _pendingWrites;

    internal IReadOnlySet<DatabaseKey> DeletedKeys => _deletedKeys;

    #endregion

    #region 公开方法

    public ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        return _manager.GetAsync(this, key, cancellationToken);
    }

    public ValueTask PutAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        return _manager.PutAsync(this, key, value, cancellationToken);
    }

    public ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        return _manager.DeleteAsync(this, key, cancellationToken);
    }

    public ValueTask CommitAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        IsCommitted = true;
        return _manager.CommitAsync(this, cancellationToken);
    }

    public void Rollback()
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        _manager.Rollback(this);
    }

    #endregion

    #region 内部方法

    internal DatabaseValue? GetPendingWrite(DatabaseKey key)
    {
        return _pendingWrites.GetValueOrDefault(key);
    }

    internal void AddPendingWrite(DatabaseKey key, DatabaseValue value)
    {
        _pendingWrites[key] = value;
        _deletedKeys.Remove(key);
    }

    internal void AddPendingDelete(DatabaseKey key)
    {
        _deletedKeys.Add(key);
        _pendingWrites.Remove(key);
    }

    internal void MarkRolledBack()
    {
        IsRolledBack = true;
    }

    #endregion

    #region 私有方法

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void ThrowIfNotActive()
    {
        if (IsCommitted)
        {
            throw new InvalidOperationException("事务已提交，无法执行操作");
        }

        if (IsRolledBack)
        {
            throw new InvalidOperationException("事务已回滚，无法执行操作");
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (!IsCommitted && !IsRolledBack)
        {
            Rollback();
        }

        _pendingWrites.Clear();
        _deletedKeys.Clear();
        _disposed = true;
    }

    #endregion
}
