using Gnosis.Database.Core;

namespace Gnosis.Database.WAL;

public interface IWriteAheadLog : IDisposable, IAsyncDisposable
{
    ValueTask<SequenceNumber> AppendAsync(WalEntry entry, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<WalEntry>> ReadFromAsync(SequenceNumber sequence, CancellationToken cancellationToken = default);

    ValueTask TruncateAsync(SequenceNumber sequence, CancellationToken cancellationToken = default);

    ValueTask CheckpointAsync(WalCheckpoint checkpoint, CancellationToken cancellationToken = default);

    ValueTask<WalCheckpoint?> GetLatestCheckpointAsync(CancellationToken cancellationToken = default);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);

    WalOptions Options { get; }
}
