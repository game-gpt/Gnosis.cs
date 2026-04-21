using Gnosis.Database.Core;

namespace Gnosis.Database.WAL;

public interface IWalReplayer
{
    ValueTask ReplayAsync(IWriteAheadLog wal, SequenceNumber fromSequence, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<WalEntry>> GetCommittedEntriesAsync(SequenceNumber fromSequence, CancellationToken cancellationToken = default);
}
