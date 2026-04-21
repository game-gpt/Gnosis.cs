using Gnosis.Database.Core;

namespace Gnosis.Database.PageManager;

public readonly record struct Page(
    PageId Id,
    PageType Type,
    int DataLength,
    byte[] Data,
    SequenceNumber LastModified)
{
    public static Page Empty(PageId id) => new(id, PageType.Free, 0, [], SequenceNumber.Zero);
}
