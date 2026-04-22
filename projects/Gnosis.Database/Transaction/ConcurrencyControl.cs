using System.Threading;
using Gnosis.Database.Core;

namespace Gnosis.Database.Transaction;

public sealed class ReaderWriterLock
{
    #region 字段

    private readonly object _lock = new();
    private int _readerCount;
    private int _writerCount;
    private bool _writerActive;

    #endregion

    #region 公开方法

    public void EnterReadLock()
    {
        lock (_lock)
        {
            while (_writerActive || _writerCount > 0)
            {
                Monitor.Wait(_lock);
            }

            _readerCount++;
        }
    }

    public void ExitReadLock()
    {
        lock (_lock)
        {
            _readerCount--;
            if (_readerCount == 0)
            {
                Monitor.PulseAll(_lock);
            }
        }
    }

    public void EnterWriteLock()
    {
        lock (_lock)
        {
            _writerCount++;

            while (_readerCount > 0 || _writerActive)
            {
                Monitor.Wait(_lock);
            }

            _writerActive = true;
        }
    }

    public void ExitWriteLock()
    {
        lock (_lock)
        {
            _writerActive = false;
            _writerCount--;
            Monitor.PulseAll(_lock);
        }
    }

    #endregion
}

public sealed class OptimisticConcurrencyControl
{
    #region 字段

    private long _version;

    #endregion

    #region 属性

    public long CurrentVersion => Interlocked.Read(ref _version);

    #endregion

    #region 公开方法

    public long BeginRead()
    {
        return Interlocked.Read(ref _version);
    }

    public bool ValidateRead(long readVersion)
    {
        return Interlocked.Read(ref _version) == readVersion;
    }

    public bool TryCommit()
    {
        Interlocked.Increment(ref _version);
        return true;
    }

    public void Abort()
    {
    }

    #endregion
}

public sealed class TransactionLockManager
{
    #region 字段

    private readonly ReaderWriterLock _globalLock;
    private readonly OptimisticConcurrencyControl _optimisticControl;
    private readonly Dictionary<ulong, int> _transactionReadCounts;
    private readonly object _lock = new();

    #endregion

    #region 构造函数

    public TransactionLockManager()
    {
        _globalLock = new ReaderWriterLock();
        _optimisticControl = new OptimisticConcurrencyControl();
        _transactionReadCounts = new Dictionary<ulong, int>();
    }

    #endregion

    #region 公开方法

    public void AcquireReadLock(TransactionId transactionId)
    {
        _globalLock.EnterReadLock();

        lock (_lock)
        {
            _transactionReadCounts[transactionId.Value] = _transactionReadCounts.GetValueOrDefault(transactionId.Value) + 1;
        }
    }

    public void ReleaseReadLock(TransactionId transactionId)
    {
        lock (_lock)
        {
            if (_transactionReadCounts.TryGetValue(transactionId.Value, out var count))
            {
                if (count <= 1)
                {
                    _transactionReadCounts.Remove(transactionId.Value);
                }
                else
                {
                    _transactionReadCounts[transactionId.Value] = count - 1;
                }
            }
        }

        _globalLock.ExitReadLock();
    }

    public void AcquireWriteLock()
    {
        _globalLock.EnterWriteLock();
    }

    public void ReleaseWriteLock()
    {
        _globalLock.ExitWriteLock();
    }

    public long BeginOptimisticRead()
    {
        return _optimisticControl.BeginRead();
    }

    public bool ValidateOptimisticRead(long readVersion)
    {
        return _optimisticControl.ValidateRead(readVersion);
    }

    public bool TryOptimisticCommit()
    {
        return _optimisticControl.TryCommit();
    }

    #endregion
}
