using Gnosis.Database.Core;

namespace Gnosis.Database.WAL;

public readonly record struct WalEntry(
    SequenceNumber Sequence,
    TransactionId TransactionId,
    WalEntryType EntryType,
    DatabaseKey Key,
    DatabaseValue Value,
    uint Checksum)
{
    public uint ComputeChecksum()
    {
        uint hash = 17;
        hash = hash * 31 + (uint)Sequence.Value;
        hash = hash * 31 + (uint)TransactionId.Value;
        hash = hash * 31 + (uint)EntryType;

        var keySpan = Key.Bytes.Span;
        for (var i = 0; i < keySpan.Length; i++)
        {
            hash = hash * 31 + keySpan[i];
        }

        var valueSpan = Value.Bytes.Span;
        for (var i = 0; i < valueSpan.Length; i++)
        {
            hash = hash * 31 + valueSpan[i];
        }

        return hash;
    }
}
