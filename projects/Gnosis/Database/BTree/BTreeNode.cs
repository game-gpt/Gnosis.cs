using Gnosis.Database.Core;

namespace Gnosis.Database.BTree;

public readonly record struct BTreeNode(
    PageId PageId,
    bool IsLeaf,
    int KeyCount,
    DatabaseKey[] Keys,
    PageId[] Children,
    DatabaseValue[] Values,
    PageId NextLeaf)
{
    public bool IsFull => KeyCount >= Keys.Length;

    public bool IsUnderflow => KeyCount < Keys.Length / 2;
}
