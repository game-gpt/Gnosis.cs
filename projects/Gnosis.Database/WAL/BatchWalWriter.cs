using Gnosis.Database.Core;

namespace Gnosis.Database.WAL;

public sealed class BatchWalWriter : IDisposable
{
    #region 字段

    private readonly IWriteAheadLog _wal;
    private readonly List<WalEntry> _batch;
    private readonly int _maxBatchSize;
    private readonly object _lock = new();
    private bool _disposed;

    #endregion

    #region 构造函数

    public BatchWalWriter(IWriteAheadLog wal, int maxBatchSize = 1024)
    {
        _wal = wal;
        _maxBatchSize = maxBatchSize;
        _batch = new List<WalEntry>(maxBatchSize);
    }

    #endregion

    #region 属性

    public int PendingCount
    {
        get
        {
            lock (_lock)
            {
                return _batch.Count;
            }
        }
    }

    #endregion

    #region 公开方法

    public ValueTask<SequenceNumber> AddAsync(WalEntry entry, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_lock)
        {
            _batch.Add(entry);

            if (_batch.Count >= _maxBatchSize)
            {
                return FlushAsync(cancellationToken);
            }
        }

        return new ValueTask<SequenceNumber>(entry.Sequence);
    }

    public async ValueTask<SequenceNumber> FlushAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        List<WalEntry> toFlush;
        lock (_lock)
        {
            if (_batch.Count == 0)
            {
                return SequenceNumber.Zero;
            }

            toFlush = new List<WalEntry>(_batch);
            _batch.Clear();
        }

        SequenceNumber lastSequence = SequenceNumber.Zero;
        foreach (var entry in toFlush)
        {
            lastSequence = await _wal.AppendAsync(entry, cancellationToken).ConfigureAwait(false);
        }

        await _wal.FlushAsync(cancellationToken).ConfigureAwait(false);

        return lastSequence;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_batch.Count > 0)
        {
            FlushAsync().AsTask().GetAwaiter().GetResult();
        }

        _disposed = true;
    }

    #endregion
}
