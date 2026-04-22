using Gnosis.Core;
using Gnosis.Infrastructure;

namespace Gnosis.Database.SHM;

public readonly record struct ShmSegmentHeader(
    uint Magic,
    uint Version,
    int SegmentCount,
    int FreeListHead,
    long TotalSize,
    long UsedSize,
    Timestamp CreatedAt)
{
    public static readonly uint ExpectedMagic = 0x47454E53;

    public static readonly uint CurrentVersion = 1;

    public double UsageRatio => TotalSize > 0 ? (double)UsedSize / TotalSize : 0;
}
