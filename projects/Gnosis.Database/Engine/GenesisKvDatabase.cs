using Gnosis.Database.BTree;
using Gnosis.Database.Cache;
using Gnosis.Database.Core;
using Gnosis.Database.Query;
using Gnosis.Database.Transaction;
using Gnosis.Database.WAL;

namespace Gnosis.Database.Engine;

public sealed class GenesisKvDatabase : IKvDatabase
{
    #region 字段

    private readonly BTreeIndex _btree;
    private readonly WalManager _wal;
    private readonly TransactionManager _transactionManager;
    private readonly RowCache _rowCache;
    private readonly DatabaseOptions _options;
    private DatabaseStatistics _statistics;
    private bool _disposed;

    #endregion

    #region 构造函数

    public GenesisKvDatabase(DatabaseOptions options)
    {
        _options = options;
        _btree = new BTreeIndex(options.BTreeOrder);
        _wal = new WalManager(options.Wal);
        _transactionManager = new TransactionManager(_btree, _wal);
        _rowCache = new RowCache();
        _statistics = DatabaseStatistics.Zero;
    }

    #endregion

    #region 属性

    public DatabaseOptions Options => _options;

    public DatabaseStatistics Statistics => _statistics;

    #endregion

    #region 公开方法

    public ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _transactionManager.BeginTransaction(isolationLevel);
    }

    public ISnapshot CreateSnapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _transactionManager.CreateSnapshot();
    }

    public async ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var cached = _rowCache.Get(key);
        if (cached is not null)
        {
            _statistics = _statistics with { TotalReads = _statistics.TotalReads + 1, CacheHits = _statistics.CacheHits + 1 };
            return cached;
        }

        _statistics = _statistics with { TotalReads = _statistics.TotalReads + 1, CacheMisses = _statistics.CacheMisses + 1 };

        var result = await _btree.SearchAsync(key, cancellationToken).ConfigureAwait(false);

        if (result is not null)
        {
            _rowCache.Put(key, result.Value);
        }

        return result;
    }

    public async ValueTask PutAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var txn = _transactionManager.BeginTransaction();
        await txn.PutAsync(key, value, cancellationToken).ConfigureAwait(false);
        await txn.CommitAsync(cancellationToken).ConfigureAwait(false);

        _rowCache.Put(key, value);
        _statistics = _statistics with
        {
            TotalWrites = _statistics.TotalWrites + 1,
            TotalKeys = _statistics.TotalKeys + 1
        };
    }

    public async ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var existing = await _btree.SearchAsync(key, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return false;
        }

        using var txn = _transactionManager.BeginTransaction();
        var deleted = await txn.DeleteAsync(key, cancellationToken).ConfigureAwait(false);
        await txn.CommitAsync(cancellationToken).ConfigureAwait(false);

        _rowCache.Invalidate(key);
        _statistics = _statistics with
        {
            TotalWrites = _statistics.TotalWrites + 1,
            TotalKeys = Math.Max(0, _statistics.TotalKeys - 1)
        };

        return deleted;
    }

    public async ValueTask<bool> ExistsAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var cached = _rowCache.Get(key);
        if (cached is not null)
        {
            return true;
        }

        var result = await _btree.SearchAsync(key, cancellationToken).ConfigureAwait(false);
        return result is not null;
    }

    public ICursor Seek(DatabaseKey key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var btreeCursor = _btree.CreateCursor();
        btreeCursor.Seek(key);
        return new Query.BTreeCursor(btreeCursor);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _rowCache.Dispose();
        _wal.Dispose();
        _btree.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _rowCache.Dispose();
        await _wal.DisposeAsync().ConfigureAwait(false);
        _btree.Dispose();
        _disposed = true;
    }

    #endregion
}
