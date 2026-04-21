using Gnosis.Database.Core;
using Gnosis.Database.WAL;

namespace Gnosis.Database;

public sealed class WalManager : IWriteAheadLog
{
    private SequenceNumber _currentSequence;

    public WalManager(WalOptions options)
    {
        Options = options;
        _currentSequence = SequenceNumber.Zero;
    }

    public WalOptions Options { get; }

    public ValueTask<SequenceNumber> AppendAsync(WalEntry entry, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask CheckpointAsync(WalCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<WalCheckpoint?> GetLatestCheckpointAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IReadOnlyList<WalEntry>> ReadFromAsync(SequenceNumber sequence, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask TruncateAsync(SequenceNumber sequence, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
