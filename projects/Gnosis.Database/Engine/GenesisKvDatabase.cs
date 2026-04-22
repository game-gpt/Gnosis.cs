using Gnosis.Database.Core;
using SolidDB.Core;

namespace Gnosis.Database.Engine;

public sealed class GenesisKvDatabase : IKvDatabase
{
    #region 字段

    private readonly SolidDB.SolidDatabase _solidDb;
    private readonly DatabaseOptions _options;
    private DatabaseStatistics _statistics;
    private bool _disposed;

    #endregion

    #region 构造函数

    public GenesisKvDatabase(DatabaseOptions options)
    {
        _options = options;
        _solidDb = new SolidDB.SolidDatabase(options.ToSolidOptions());
        _statistics = DatabaseStatistics.Zero;
    }

    #endregion

    #region 属性

    public DatabaseOptions Options => _options;

    public DatabaseStatistics Statistics => DatabaseStatistics.FromSolidStatistics(_solidDb.Statistics);

    #endregion

    #region 公开方法

    public ITransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var solidIsolation = (SolidDB.Core.IsolationLevel)isolationLevel;
        var solidTx = _solidDb.BeginTransactionAsync(solidIsolation).GetAwaiter().GetResult();
        return new KvTransaction(solidTx);
    }

    public ISnapshot CreateSnapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var solidSnapshot = _solidDb.CreateSnapshot();
        return new KvSnapshot(solidSnapshot);
    }

    public async ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var result = await _solidDb.GetAsync<SolidValue>(key, cancellationToken);
        if (result.IsEmpty)
        {
            return null;
        }

        return new DatabaseValue(result.Bytes);
    }

    public async ValueTask PutAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _solidDb.PutAsync(key, (SolidValue)value, cancellationToken);
        _statistics = _statistics with { TotalWrites = _statistics.TotalWrites + 1, TotalKeys = _statistics.TotalKeys + 1 };
    }

    public async ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var deleted = await _solidDb.DeleteAsync(key, cancellationToken);
        if (deleted)
        {
            _statistics = _statistics with
            {
                TotalWrites = _statistics.TotalWrites + 1,
                TotalKeys = Math.Max(0, _statistics.TotalKeys - 1)
            };
        }

        return deleted;
    }

    public async ValueTask<bool> ExistsAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return await _solidDb.ExistsAsync(key, cancellationToken);
    }

    public ICursor Seek(DatabaseKey key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var solidCursor = _solidDb.Seek(key);
        return new KvCursor(solidCursor);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _solidDb.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _solidDb.DisposeAsync();
        _disposed = true;
    }

    #endregion

    #region 内部适配器

    private sealed class KvTransaction : ITransaction
    {
        private readonly ISolidTransaction _solidTx;

        public KvTransaction(ISolidTransaction solidTx)
        {
            _solidTx = solidTx;
        }

        public TransactionId Id => new(_solidTx.Id.Value);

        public IsolationLevel IsolationLevel => (IsolationLevel)_solidTx.IsolationLevel;

        public Timestamp StartTime => new(new DateTimeOffset(_solidTx.StartTime));

        public bool IsReadOnly => _solidTx.IsReadOnly;

        public bool IsCommitted => _solidTx.IsCommitted;

        public bool IsRolledBack => _solidTx.IsRolledBack;

        public async ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default)
        {
            var result = await _solidTx.GetAsync<SolidValue>(key, cancellationToken);
            if (result.IsEmpty)
            {
                return null;
            }

            return new DatabaseValue(result.Bytes);
        }

        public async ValueTask PutAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default)
        {
            await _solidTx.PutAsync(key, (SolidValue)value, cancellationToken);
        }

        public async ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default)
        {
            return await _solidTx.DeleteAsync(key, cancellationToken);
        }

        public async ValueTask CommitAsync(CancellationToken cancellationToken = default)
        {
            await _solidTx.CommitAsync(cancellationToken);
        }

        public void Rollback()
        {
            _solidTx.RollbackAsync().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _solidTx.Dispose();
        }
    }

    private sealed class KvSnapshot : ISnapshot
    {
        private readonly ISolidSnapshot _solidSnapshot;

        public KvSnapshot(ISolidSnapshot solidSnapshot)
        {
            _solidSnapshot = solidSnapshot;
        }

        public SequenceNumber Sequence => new(_solidSnapshot.Sequence.Value);

        public async ValueTask<DatabaseValue?> GetAsync(DatabaseKey key, CancellationToken cancellationToken = default)
        {
            var result = await _solidSnapshot.GetAsync<SolidValue>(key, cancellationToken);
            if (result.IsEmpty)
            {
                return null;
            }

            return new DatabaseValue(result.Bytes);
        }

        public ICursor Seek(DatabaseKey key)
        {
            var solidCursor = _solidSnapshot.Seek(key);
            return new KvCursor(solidCursor);
        }

        public ISnapshot CreateChild()
        {
            var child = _solidSnapshot.CreateChild();
            return new KvSnapshot(child);
        }

        public void Dispose()
        {
            _solidSnapshot.Dispose();
        }
    }

    private sealed class KvCursor : ICursor
    {
        private readonly ISolidCursor _solidCursor;

        public KvCursor(ISolidCursor solidCursor)
        {
            _solidCursor = solidCursor;
        }

        public DatabaseEntry Current => DatabaseEntry.FromSolidEntry(_solidCursor.Current);

        public bool IsValid => _solidCursor.IsValid;

        public bool MoveNext() => _solidCursor.MoveNextAsync().GetAwaiter().GetResult();

        public bool MovePrev() => _solidCursor.MovePrevAsync().GetAwaiter().GetResult();

        public bool SeekToFirst() => _solidCursor.SeekToFirstAsync().GetAwaiter().GetResult();

        public bool SeekToLast() => _solidCursor.SeekToLastAsync().GetAwaiter().GetResult();

        public bool Seek(DatabaseKey key) => _solidCursor.SeekAsync(key).GetAwaiter().GetResult();

        public IReadOnlyList<DatabaseEntry> GetRange(DatabaseKey start, DatabaseKey end, int limit = 1000)
        {
            var solidEntries = _solidCursor.GetRangeAsync(start, end, limit).GetAwaiter().GetResult();
            return solidEntries.Select(DatabaseEntry.FromSolidEntry).ToList().AsReadOnly();
        }

        public void Dispose()
        {
            _solidCursor.Dispose();
        }
    }

    #endregion
}
