using Gnosis.Database.Core;

namespace Gnosis.Database.PageManager;

public readonly record struct FreePageList(IReadOnlyList<PageId> FreePages)
{
    public int Count => FreePages.Count;

    public static readonly FreePageList Empty = new([]);
}
