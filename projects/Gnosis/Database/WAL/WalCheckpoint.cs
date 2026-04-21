using Gnosis.Database.Core;

namespace Gnosis.Database.WAL;

public readonly record struct WalCheckpoint(
    SequenceNumber Sequence,
    Timestamp Timestamp,
    long DataFileOffset,
    int DirtyPageCount)
{
    public static readonly WalCheckpoint Zero = new(
        SequenceNumber.Zero,
        Timestamp.Now,
        0,
        0);
}
