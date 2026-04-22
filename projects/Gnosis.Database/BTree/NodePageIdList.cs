using System.Collections;
using Gnosis.Database.Core;

namespace Gnosis.Database.BTree;

internal sealed class NodePageIdList : IReadOnlyList<PageId>
{
    private readonly BTreeNode?[] _children;
    private readonly int _count;

    public NodePageIdList(BTreeNode node)
    {
        _children = node.ChildrenNodes;
        _count = node.KeyCount + 1;
    }

    public PageId this[int index] => index >= 0 && index < _count && _children[index] is not null
        ? _children[index]!.PageId
        : PageId.Invalid;

    public int Count => _count;

    public IEnumerator<PageId> GetEnumerator()
    {
        for (var i = 0; i < _count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
