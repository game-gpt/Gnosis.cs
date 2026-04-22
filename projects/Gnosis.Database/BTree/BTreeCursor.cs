using Gnosis.Database.Core;

namespace Gnosis.Database.BTree;

public sealed class BTreeCursor : IBTreeCursor
{
    private readonly BTreeIndex _tree;
    private BTreeNode? _currentNode;
    private int _currentIndex;

    public BTreeCursor(BTreeIndex tree)
    {
        _tree = tree;
        _currentNode = null;
        _currentIndex = -1;
    }

    public DatabaseEntry Current
    {
        get
        {
            if (!IsValid)
            {
                return DatabaseEntry.Empty;
            }

            return new DatabaseEntry(
                _currentNode!.KeysInternal[_currentIndex],
                _currentNode.ValuesInternal[_currentIndex]);
        }
    }

    public bool IsValid => _currentNode is not null
        && _currentIndex >= 0
        && _currentIndex < _currentNode.KeyCount;

    public bool MoveNext()
    {
        if (_currentNode is null)
        {
            return SeekToFirst();
        }

        _currentIndex++;
        if (_currentIndex < _currentNode.KeyCount)
        {
            return true;
        }

        _currentNode = _currentNode.NextLeafNode;
        if (_currentNode is null)
        {
            _currentIndex = -1;
            return false;
        }

        _currentIndex = 0;
        return _currentNode.KeyCount > 0;
    }

    public bool MovePrev()
    {
        if (_currentNode is null)
        {
            return SeekToLast();
        }

        _currentIndex--;
        if (_currentIndex >= 0)
        {
            return true;
        }

        var targetKey = _currentNode.KeysInternal[0];
        _currentNode = null;
        _currentIndex = -1;

        if (!SeekToFirst())
        {
            return false;
        }

        BTreeNode? prevNode = null;
        var prevIndex = -1;

        while (IsValid)
        {
            var cmp = Current.Key.CompareTo(targetKey);
            if (cmp >= 0)
            {
                break;
            }

            prevNode = _currentNode;
            prevIndex = _currentIndex;
            MoveNext();
        }

        if (prevNode is not null && prevIndex >= 0)
        {
            _currentNode = prevNode;
            _currentIndex = prevIndex;
            return true;
        }

        _currentNode = null;
        _currentIndex = -1;
        return false;
    }

    public bool Seek(DatabaseKey key)
    {
        var node = _tree.GetRootNode();
        if (node is null)
        {
            _currentNode = null;
            _currentIndex = -1;
            return false;
        }

        while (!node.IsLeaf)
        {
            var index = BTreeIndex.FindChildIndex(node, key);
            node = node.ChildrenNodes[index];
            if (node is null)
            {
                _currentNode = null;
                _currentIndex = -1;
                return false;
            }
        }

        _currentNode = node;
        _currentIndex = node.FindKeyIndex(key);

        if (_currentIndex < node.KeyCount)
        {
            return true;
        }

        _currentNode = node.NextLeafNode;
        _currentIndex = 0;
        return _currentNode is not null && _currentNode.KeyCount > 0;
    }

    public bool SeekToFirst()
    {
        var node = _tree.GetRootNode();
        if (node is null)
        {
            _currentNode = null;
            _currentIndex = -1;
            return false;
        }

        while (!node.IsLeaf)
        {
            node = node.ChildrenNodes[0];
            if (node is null)
            {
                _currentNode = null;
                _currentIndex = -1;
                return false;
            }
        }

        _currentNode = node;
        _currentIndex = 0;
        return node.KeyCount > 0;
    }

    public bool SeekToLast()
    {
        var node = _tree.GetRootNode();
        if (node is null)
        {
            _currentNode = null;
            _currentIndex = -1;
            return false;
        }

        while (!node.IsLeaf)
        {
            node = node.ChildrenNodes[node.KeyCount];
            if (node is null)
            {
                _currentNode = null;
                _currentIndex = -1;
                return false;
            }
        }

        _currentNode = node;
        _currentIndex = node.KeyCount - 1;
        return node.KeyCount > 0;
    }

    public void Dispose()
    {
        _currentNode = null;
    }
}
