using Gnosis.Database.Core;

namespace Gnosis.Database.PageManager;

public interface IFreePageList
{
    int Count { get; }

    PageId Allocate();

    void Free(PageId pageId);

    bool IsEmpty { get; }
}
