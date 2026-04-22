using System.Collections.Generic;
using Gnosis.Database.Core;

namespace Gnosis.Database.BTree;

public interface IBTreeNode
{
    PageId PageId { get; }

    bool IsLeaf { get; }

    int KeyCount { get; }

    IReadOnlyList<DatabaseKey> Keys { get; }

    IReadOnlyList<PageId> Children { get; }

    IReadOnlyList<DatabaseValue> Values { get; }

    PageId NextLeaf { get; }

    bool IsFull { get; }

    bool IsUnderflow { get; }
}
