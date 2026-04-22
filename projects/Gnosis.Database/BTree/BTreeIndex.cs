using Gnosis.Database.Core;

namespace Gnosis.Database.BTree;

public sealed class BTreeIndex : IBTree
{
    private BTreeNode? _root;
    private long _count;
    private int _height;

    public BTreeIndex(int order)
    {
        Order = order;
        _root = new BTreeNode(order, true);
        _count = 0;
        _height = 1;
    }

    public int Order { get; }

    public int Height => _height;

    public long Count => _count;

    public IBTreeCursor CreateCursor()
    {
        return new BTreeCursor(this);
    }

    internal BTreeNode? GetRootNode() => _root;

    internal static int FindChildIndex(BTreeNode node, DatabaseKey key)
    {
        var index = node.FindKeyIndex(key);
        if (index < node.KeyCount && node.KeysInternal[index].CompareTo(key) <= 0)
        {
            return index + 1;
        }

        return index;
    }

    public ValueTask<bool> InsertAsync(DatabaseKey key, DatabaseValue value, CancellationToken cancellationToken = default)
    {
        if (_root is null)
        {
            _root = new BTreeNode(Order, true);
            _height = 1;
        }

        var (split, middleKey, rightNode) = InsertRecursive(_root, key, value);

        if (split)
        {
            var newRoot = new BTreeNode(Order, false);
            newRoot.InsertKeyAt(0, middleKey, default);
            newRoot.ChildrenNodes[0] = _root;
            newRoot.ChildrenNodes[1] = rightNode;
            _root = newRoot;
            _height++;
        }

        return new ValueTask<bool>(true);
    }

    private (bool Split, DatabaseKey MiddleKey, BTreeNode RightNode) InsertRecursive(
        BTreeNode node, DatabaseKey key, DatabaseValue value)
    {
        node.DecompressInternalKeys();

        if (node.IsLeaf)
        {
            var index = node.FindKeyIndex(key);
            if (index < node.KeyCount && node.KeysInternal[index].CompareTo(key) == 0)
            {
                node.ValuesInternal[index] = value;
                return (false, default, null!);
            }

            node.InsertKeyAt(index, key, value);
            _count++;

            if (!node.IsFull)
            {
                return (false, default, null!);
            }

            var middleKey = node.GetMiddleKey();
            var rightNode = node.Split();
            return (true, middleKey, rightNode);
        }

        var childIndex = FindChildIndex(node, key);
        var child = node.ChildrenNodes[childIndex];
        if (child is null)
        {
            child = new BTreeNode(Order, true);
            node.ChildrenNodes[childIndex] = child;
        }

        var (childSplit, childMiddleKey, childRight) = InsertRecursive(child, key, value);

        if (!childSplit)
        {
            return (false, default, null!);
        }

        node.InsertKeyAt(childIndex, childMiddleKey, default);
        node.ChildrenNodes[childIndex + 1] = childRight;

        if (!node.IsFull)
        {
            node.CompressInternalKeys();
            return (false, default, null!);
        }

        var middleKey2 = node.GetMiddleKey();
        var rightNode2 = node.Split();
        return (true, middleKey2, rightNode2);
    }

    public ValueTask<DatabaseValue?> SearchAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        var node = _root;
        while (node is not null)
        {
            node.DecompressInternalKeys();

            var index = node.FindKeyIndex(key);

            if (node.IsLeaf)
            {
                if (index < node.KeyCount && node.KeysInternal[index].CompareTo(key) == 0)
                {
                    return new ValueTask<DatabaseValue?>(node.ValuesInternal[index]);
                }

                return new ValueTask<DatabaseValue?>(null as DatabaseValue?);
            }

            node = node.ChildrenNodes[FindChildIndex(node, key)];
        }

        return new ValueTask<DatabaseValue?>(null as DatabaseValue?);
    }

    public ValueTask<bool> DeleteAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        if (_root is null || _root.KeyCount == 0)
        {
            return new ValueTask<bool>(false);
        }

        var deleted = DeleteRecursive(_root, key);

        if (!deleted)
        {
            return new ValueTask<bool>(false);
        }

        _count--;

        if (!_root.IsLeaf && _root.KeyCount == 0)
        {
            _root = _root.ChildrenNodes[0];
            _height--;
        }

        return new ValueTask<bool>(true);
    }

    private bool DeleteRecursive(BTreeNode node, DatabaseKey key)
    {
        node.DecompressInternalKeys();

        if (node.IsLeaf)
        {
            var index = node.FindKeyIndex(key);
            if (index >= node.KeyCount || node.KeysInternal[index].CompareTo(key) != 0)
            {
                return false;
            }

            node.RemoveKeyAt(index);
            return true;
        }

        var childIndex = FindChildIndex(node, key);
        var child = node.ChildrenNodes[childIndex];
        if (child is null)
        {
            return false;
        }

        var deleted = DeleteRecursive(child, key);
        if (!deleted)
        {
            return false;
        }

        FixUnderflow(node, childIndex);
        return true;
    }

    private void FixUnderflow(BTreeNode parent, int childIndex)
    {
        var child = parent.ChildrenNodes[childIndex];
        if (child is null || !child.IsUnderflow)
        {
            return;
        }

        BTreeNode? leftSibling = childIndex > 0 ? parent.ChildrenNodes[childIndex - 1] : null;
        BTreeNode? rightSibling = childIndex < parent.KeyCount ? parent.ChildrenNodes[childIndex + 1] : null;

        if (leftSibling is not null && leftSibling.KeyCount > leftSibling.MinKeys)
        {
            var parentKey = parent.KeysInternal[childIndex - 1];
            child.BorrowFromLeft(leftSibling, ref parentKey);
            parent.KeysInternal[childIndex - 1] = parentKey;
        }
        else if (rightSibling is not null && rightSibling.KeyCount > rightSibling.MinKeys)
        {
            var parentKey = parent.KeysInternal[childIndex];
            child.BorrowFromRight(rightSibling, ref parentKey);
            parent.KeysInternal[childIndex] = parentKey;
        }
        else if (leftSibling is not null)
        {
            var parentKey = parent.KeysInternal[childIndex - 1];
            leftSibling.MergeWithRight(child, parentKey);
            parent.RemoveKeyAt(childIndex - 1);
            parent.RemoveChildAt(childIndex);
        }
        else if (rightSibling is not null)
        {
            var parentKey = parent.KeysInternal[childIndex];
            child.MergeWithRight(rightSibling, parentKey);
            parent.RemoveKeyAt(childIndex);
            parent.RemoveChildAt(childIndex + 1);
        }
    }

    public ValueTask<(bool Success, BTreeSplitResult Result)> TrySplitAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        if (_root is null || _root.KeyCount == 0)
        {
            return new ValueTask<(bool Success, BTreeSplitResult Result)>((false, default));
        }

        var node = FindLeafNode(key);
        if (node is null || !node.IsFull)
        {
            return new ValueTask<(bool Success, BTreeSplitResult Result)>((false, default));
        }

        var middleKey = node.GetMiddleKey();
        var leftPageId = node.PageId;
        var rightNode = node.Split();
        var result = new BTreeSplitResult(middleKey, leftPageId, rightNode.PageId);
        return new ValueTask<(bool Success, BTreeSplitResult Result)>((true, result));
    }

    public ValueTask<(bool Success, BTreeMergeResult Result)> TryMergeAsync(DatabaseKey key, CancellationToken cancellationToken = default)
    {
        if (_root is null || _root.KeyCount == 0)
        {
            return new ValueTask<(bool Success, BTreeMergeResult Result)>((false, default));
        }

        var node = FindLeafNode(key);
        if (node is null)
        {
            return new ValueTask<(bool Success, BTreeMergeResult Result)>((false, default));
        }

        var mergedPageId = node.PageId;
        var removedKey = node.KeysInternal.Length > 0 ? node.KeysInternal[0] : key;
        var result = new BTreeMergeResult(mergedPageId, removedKey);
        return new ValueTask<(bool Success, BTreeMergeResult Result)>((true, result));
    }

    private BTreeNode? FindLeafNode(DatabaseKey key)
    {
        var node = _root;
        while (node is not null && !node.IsLeaf)
        {
            node.DecompressInternalKeys();
            node = node.ChildrenNodes[FindChildIndex(node, key)];
        }

        return node;
    }

    public void Dispose()
    {
        _root = null;
        _count = 0;
        _height = 0;
    }
}
