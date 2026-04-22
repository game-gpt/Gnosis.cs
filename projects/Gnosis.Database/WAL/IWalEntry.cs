using Gnosis.Database.Core;

namespace Gnosis.Database.WAL;

public interface IWalEntry
{
    SequenceNumber Sequence { get; }

    TransactionId TransactionId { get; }

    WalEntryType EntryType { get; }

    DatabaseKey Key { get; }

    DatabaseValue Value { get; }

    uint Checksum { get; }

    uint ComputeChecksum();
}
