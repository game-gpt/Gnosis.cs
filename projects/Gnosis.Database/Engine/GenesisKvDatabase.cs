using GnosisDatabaseCore = Gnosis.Database.Core;
using LightDatabaseCore = LightDB.Core;

namespace Gnosis.Database.Engine;

public sealed class GenesisKvDatabase : GnosisDatabaseCore.IKvDatabase
{
    #region 字段

    private readonly LightDB.LightDatabase _lightDb;
    private readonly GnosisDatabaseCore.DatabaseOptions _options;
    private GnosisDatabaseCore.DatabaseStatistics _statistics;
    private bool _disposed;

    #endregion

    #region 构造函数

    public GenesisKvDatabase(GnosisDatabaseCore.DatabaseOptions options)
    {
        _options = options;
        _lightDb = new LightDB.LightDatabase(options.ToLightOptions());
        _statistics = GnosisDatabaseCore.DatabaseStatistics.Zero;
    }

    #endregion

    #region 属性

    public GnosisDatabaseCore.DatabaseOptions Options => _options;

    public GnosisDatabaseCore.DatabaseStatistics Statistics =>
        GnosisDatabaseCore.DatabaseStatistics.FromLightStatistics(_lightDb.Statistics);

    #endregion

    #region 公开方法

    public async ValueTask<GnosisDatabaseCore.ITransaction> BeginTransactionAsync(
        GnosisDatabaseCore.IsolationLevel isolationLevel = GnosisDatabaseCore.IsolationLevel.Snapshot)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var lightIsolation = (LightDatabaseCore.IsolationLevel)isolationLevel;
        var lightTx = await _lightDb.BeginTransactionAsync(lightIsolation);
        return new KvTransaction(lightTx);
    }

    public GnosisDatabaseCore.ITransaction BeginTransaction(
        GnosisDatabaseCore.IsolationLevel isolationLevel = GnosisDatabaseCore.IsolationLevel.Snapshot)
    {
        return BeginTransactionAsync(isolationLevel).GetAwaiter().GetResult();
    }

    public GnosisDatabaseCore.ISnapshot CreateSnapshot()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var lightSnapshot = _lightDb.CreateSnapshot();
        return new KvSnapshot(lightSnapshot);
    }

    public async ValueTask<GnosisDatabaseCore.DatabaseValue?> GetAsync(
        GnosisDatabaseCore.DatabaseKey key, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var result = await _lightDb.GetAsync<LightDatabaseCore.LightValue>(key, cancellationToken);
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

        await _lightDb.PutAsync(key, (LightDatabaseCore.LightValue)value, cancellationToken);
        _statistics.TotalWrites++;
        _statistics.TotalKeys++;
    }

    public async ValueTask<bool> DeleteAsync(GnosisDatabaseCore.DatabaseKey key,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var deleted = await _lightDb.DeleteAsync(key, cancellationToken);
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

        return await _lightDb.ExistsAsync(key, cancellationToken);
    }

    public GnosisDatabaseCore.ICursor Seek(GnosisDatabaseCore.DatabaseKey key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var lightCursor = _lightDb.Seek(key);
        return new KvCursor(lightCursor);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _lightDb.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _lightDb.DisposeAsync();
        _disposed = true;
    }

    #endregion

    #region 内部适配器

    private sealed class KvTransaction : GnosisDatabaseCore.ITransaction
    {
        private readonly LightDatabaseCore.ILightTransaction _lightTx;

        public KvTransaction(LightDatabaseCore.ILightTransaction lightTx)
        {
            _lightTx = lightTx;
        }

        public GnosisDatabaseCore.TransactionId Id =>
            new GnosisDatabaseCore.TransactionId(_lightTx.Id.Value);

        public GnosisDatabaseCore.IsolationLevel IsolationLevel =>
            (GnosisDatabaseCore.IsolationLevel)_lightTx.IsolationLevel;

        public GnosisDatabaseCore.Timestamp StartTime =>
            new GnosisDatabaseCore.Timestamp(new DateTimeOffset(_lightTx.StartTime));

        public bool IsReadOnly => _lightTx.IsReadOnly;

        public bool IsCommitted => _lightTx.IsCommitted;

        public bool IsRolledBack => _lightTx.IsRolledBack;

        public async ValueTask<GnosisDatabaseCore.DatabaseValue?> GetAsync(
            GnosisDatabaseCore.DatabaseKey key, CancellationToken cancellationToken = default)
        {
            var result = await _lightTx.GetAsync<LightDatabaseCore.LightValue>(key, cancellationToken);
            if (result.IsEmpty)
            {
                return null;
            }

            return new GnosisDatabaseCore.DatabaseValue(result.Bytes);
        }

        public async ValueTask PutAsync(GnosisDatabaseCore.DatabaseKey key,
            GnosisDatabaseCore.DatabaseValue value, CancellationToken cancellationToken = default)
        {
            await _lightTx.PutAsync(key, (LightDatabaseCore.LightValue)value, cancellationToken);
        }

        public async ValueTask<bool> DeleteAsync(GnosisDatabaseCore.DatabaseKey key,
            CancellationToken cancellationToken = default)
        {
            return await _lightTx.DeleteAsync(key, cancellationToken);
        }

        public async ValueTask CommitAsync(CancellationToken cancellationToken = default)
        {
            await _lightTx.CommitAsync(cancellationToken);
        }

        public async ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _lightTx.RollbackAsync(cancellationToken);
        }

        public void Rollback()
        {
            _lightTx.RollbackAsync().GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _lightTx.Dispose();
        }
    }

    private sealed class KvSnapshot : GnosisDatabaseCore.ISnapshot
    {
        private readonly LightDatabaseCore.ILightSnapshot _lightSnapshot;

        public KvSnapshot(LightDatabaseCore.ILightSnapshot lightSnapshot)
        {
            _lightSnapshot = lightSnapshot;
        }

        public GnosisDatabaseCore.SequenceNumber Sequence =>
            new GnosisDatabaseCore.SequenceNumber(_lightSnapshot.Sequence.Value);

        public async ValueTask<GnosisDatabaseCore.DatabaseValue?> GetAsync(
            GnosisDatabaseCore.DatabaseKey key, CancellationToken cancellationToken = default)
        {
            var result = await _lightSnapshot.GetAsync<LightDatabaseCore.LightValue>(key, cancellationToken);
            if (result.IsEmpty)
            {
                return null;
            }

            return new GnosisDatabaseCore.DatabaseValue(result.Bytes);
        }

        public GnosisDatabaseCore.ICursor Seek(GnosisDatabaseCore.DatabaseKey key)
        {
            var lightCursor = _lightSnapshot.Seek(key);
            return new KvCursor(lightCursor);
        }

        public GnosisDatabaseCore.ISnapshot CreateChild()
        {
            var child = _lightSnapshot.CreateChild();
            return new KvSnapshot(child);
        }

        public void Dispose()
        {
            _lightSnapshot.Dispose();
        }
    }

    private sealed class KvCursor : GnosisDatabaseCore.ICursor
    {
        private readonly LightDatabaseCore.ILightCursor _lightCursor;

        public KvCursor(LightDatabaseCore.ILightCursor lightCursor)
        {
            _lightCursor = lightCursor;
        }

        public GnosisDatabaseCore.DatabaseEntry Current =>
            GnosisDatabaseCore.DatabaseEntry.FromLightEntry(_lightCursor.Current);

        public bool IsValid => _lightCursor.IsValid;

        public bool MoveNext() => _lightCursor.MoveNextAsync().GetAwaiter().GetResult();

        public bool MovePrev() => _lightCursor.MovePrevAsync().GetAwaiter().GetResult();

        public bool SeekToFirst() => _lightCursor.SeekToFirstAsync().GetAwaiter().GetResult();

        public bool SeekToLast() => _lightCursor.SeekToLastAsync().GetAwaiter().GetResult();

        public bool Seek(GnosisDatabaseCore.DatabaseKey key) =>
            _lightCursor.SeekAsync(key).GetAwaiter().GetResult();

        public IReadOnlyList<GnosisDatabaseCore.DatabaseEntry> GetRange(
            GnosisDatabaseCore.DatabaseKey start, GnosisDatabaseCore.DatabaseKey end, int limit = 1000)
        {
            var lightEntries = _lightCursor.GetRangeAsync(start, end, limit).GetAwaiter().GetResult();
            return lightEntries.Select(GnosisDatabaseCore.DatabaseEntry.FromLightEntry).ToList().AsReadOnly();
        }

        public void Dispose()
        {
            _lightCursor.Dispose();
        }
    }

    #endregion
}
