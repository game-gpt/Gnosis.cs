using GnosisDatabaseCore = Gnosis.Database.Core;
using SolidDatabaseCore = SolidDB.Core;

namespace Gnosis.Database.Engine;

public sealed class GenesisKvDatabase : GnosisDatabaseCore.IKvDatabase
{
    #region 字段

    private readonly SolidDB.SolidDatabase _solidDb;
    private readonly GnosisDatabaseCore.DatabaseOptions _options;
    private GnosisDatabaseCore.DatabaseStatistics _statistics;
    private bool _disposed;

    #endregion

    #region 构造函数

    public GenesisKvDatabase(GnosisDatabaseCore.DatabaseOptions options)
    {
        _options = options;
        _solidDb = new SolidDB.SolidDatabase(options.ToSolidOptions());
        _statistics = GnosisDatabaseCore.DatabaseStatistics.Zero;
    }

    #endregion

    #region 属性

    public GnosisDatabaseCore.DatabaseOptions Options => _options;

    public GnosisDatabaseCore.DatabaseStatistics Statistics =>
        GnosisDatabaseCore.DatabaseStatistics.FromSolidStatistics(_solidDb.Statistics);

    #endregion

    #region 公开方法

    public GnosisDatabaseCore.ITransaction BeginTransaction(
        GnosisDatabaseCore.IsolationLevel isolationLevel = GnosisDatabaseCore.IsolationLevel.Snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var solidIsolation = (SolidDatabaseCore.IsolationLevel)isolationLevel;
        var solidTx = _solidDb.BeginTransactionAsync(solidIsolation).GetAwaiter().GetResult();
        return new KvTransaction(solidTx);
    }

    public GnosisDatabaseCore.ISnapshot CreateSnapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var solidSnapshot = _solidDb.CreateSnapshot();
        return new KvSnapshot(solidSnapshot);
    }

    public async ValueTask<GnosisDatabaseCore.DatabaseValue?> GetAsync(
        GnosisDatabaseCore.DatabaseKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var result = await _solidDb.GetAsync<SolidDatabaseCore.SolidValue>(key, cancellationToken);
        if (result.IsEmpty)
        {
            return null;
        }

        return new GnosisDatabaseCore.DatabaseValue(result.Bytes);
    }

    public async ValueTask PutAsync(GnosisDatabaseCore.DatabaseKey key,
        GnosisDatabaseCore.DatabaseValue value, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _solidDb.PutAsync(key, (SolidDatabaseCore.SolidValue)value, cancellationToken);
        _statistics.TotalWrites++;
        _statistics.TotalKeys++;
    }

    public async ValueTask<bool> DeleteAsync(GnosisDatabaseCore.DatabaseKey key,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var deleted = await _solidDb.DeleteAsync(key, cancellationToken);
        if (deleted)
        {
            _statistics.TotalWrites++;
            _statistics.TotalKeys = Math.Max(0, _statistics.TotalKeys - 1);
        }

        return deleted;
    }

    public async ValueTask<bool> ExistsAsync(GnosisDatabaseCore.DatabaseKey key,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return await _solidDb.ExistsAsync(key, cancellationToken);
    }

    public GnosisDatabaseCore.ICursor Seek(GnosisDatabaseCore.DatabaseKey key)
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

    private sealed class KvTransaction : GnosisDatabaseCore.ITransaction
    {
        private readonly SolidDatabaseCore.ISolidTransaction _solidTx;

        public KvTransaction(SolidDatabaseCore.ISolidTransaction solidTx)
        {
            _solidTx = solidTx;
        }

        public GnosisDatabaseCore.TransactionId Id =>
            new GnosisDatabaseCore.TransactionId(_solidTx.Id.Value);

        public GnosisDatabaseCore.IsolationLevel IsolationLevel =>
            (GnosisDatabaseCore.IsolationLevel)_solidTx.IsolationLevel;

        public GnosisDatabaseCore.Timestamp StartTime =>
            new GnosisDatabaseCore.Timestamp(new DateTimeOffset(_solidTx.StartTime));

        public bool IsReadOnly => _solidTx.IsReadOnly;

        public bool IsCommitted => _solidTx.IsCommitted;

        public bool IsRolledBack => _solidTx.IsRolledBack;

        public async ValueTask<GnosisDatabaseCore.DatabaseValue?> GetAsync(
            GnosisDatabaseCore.DatabaseKey key, CancellationToken cancellationToken = default)
        {
            var result = await _solidTx.GetAsync<SolidDatabaseCore.SolidValue>(key, cancellationToken);
            if (result.IsEmpty)
            {
                return null;
            }

            return new GnosisDatabaseCore.DatabaseValue(result.Bytes);
        }

        public async ValueTask PutAsync(GnosisDatabaseCore.DatabaseKey key,
            GnosisDatabaseCore.DatabaseValue value, CancellationToken cancellationToken = default)
        {
            await _solidTx.PutAsync(key, (SolidDatabaseCore.SolidValue)value, cancellationToken);
        }

        public async ValueTask<bool> DeleteAsync(GnosisDatabaseCore.DatabaseKey key,
            CancellationToken cancellationToken = default)
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

    private sealed class KvSnapshot : GnosisDatabaseCore.ISnapshot
    {
        private readonly SolidDatabaseCore.ISolidSnapshot _solidSnapshot;

        public KvSnapshot(SolidDatabaseCore.ISolidSnapshot solidSnapshot)
        {
            _solidSnapshot = solidSnapshot;
        }

        public GnosisDatabaseCore.SequenceNumber Sequence =>
            new GnosisDatabaseCore.SequenceNumber(_solidSnapshot.Sequence.Value);

        public async ValueTask<GnosisDatabaseCore.DatabaseValue?> GetAsync(
            GnosisDatabaseCore.DatabaseKey key, CancellationToken cancellationToken = default)
        {
            var result = await _solidSnapshot.GetAsync<SolidDatabaseCore.SolidValue>(key, cancellationToken);
            if (result.IsEmpty)
            {
                return null;
            }

            return new GnosisDatabaseCore.DatabaseValue(result.Bytes);
        }

        public GnosisDatabaseCore.ICursor Seek(GnosisDatabaseCore.DatabaseKey key)
        {
            var solidCursor = _solidSnapshot.Seek(key);
            return new KvCursor(solidCursor);
        }

        public GnosisDatabaseCore.ISnapshot CreateChild()
        {
            var child = _solidSnapshot.CreateChild();
            return new KvSnapshot(child);
        }

        public void Dispose()
        {
            _solidSnapshot.Dispose();
        }
    }

    private sealed class KvCursor : GnosisDatabaseCore.ICursor
    {
        private readonly SolidDatabaseCore.ISolidCursor _solidCursor;

        public KvCursor(SolidDatabaseCore.ISolidCursor solidCursor)
        {
            _solidCursor = solidCursor;
        }

        public GnosisDatabaseCore.DatabaseEntry Current =>
            GnosisDatabaseCore.DatabaseEntry.FromSolidEntry(_solidCursor.Current);

        public bool IsValid => _solidCursor.IsValid;

        public bool MoveNext() => _solidCursor.MoveNextAsync().GetAwaiter().GetResult();

        public bool MovePrev() => _solidCursor.MovePrevAsync().GetAwaiter().GetResult();

        public bool SeekToFirst() => _solidCursor.SeekToFirstAsync().GetAwaiter().GetResult();

        public bool SeekToLast() => _solidCursor.SeekToLastAsync().GetAwaiter().GetResult();

        public bool Seek(GnosisDatabaseCore.DatabaseKey key) =>
            _solidCursor.SeekAsync(key).GetAwaiter().GetResult();

        public IReadOnlyList<GnosisDatabaseCore.DatabaseEntry> GetRange(
            GnosisDatabaseCore.DatabaseKey start, GnosisDatabaseCore.DatabaseKey end, int limit = 1000)
        {
            var solidEntries = _solidCursor.GetRangeAsync(start, end, limit).GetAwaiter().GetResult();
            return solidEntries.Select(GnosisDatabaseCore.DatabaseEntry.FromSolidEntry).ToList().AsReadOnly();
        }

        public void Dispose()
        {
            _solidCursor.Dispose();
        }
    }

    #endregion
}
