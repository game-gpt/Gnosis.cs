using Gnosis.Database.Core;

namespace Gnosis.Database.PageManager;

public readonly record struct PageHeader(
    PageType Type,
    int DataLength,
    SequenceNumber LastModified,
    uint Checksum)
{
    public static readonly int Size = 17;
}
