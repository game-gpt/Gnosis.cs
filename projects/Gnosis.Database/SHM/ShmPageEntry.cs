using Gnosis.Database.Core;

namespace Gnosis.Database.SHM;

public readonly record struct ShmPageEntry(
    PageId PageId,
    int Offset,
    int Size,
    SequenceNumber Sequence,
    Timestamp LastAccess,
    int HitCount)
{
    public ShmPageEntry Touch() => this with
    {
        LastAccess = Timestamp.Now,
        HitCount = HitCount + 1
    };
}
