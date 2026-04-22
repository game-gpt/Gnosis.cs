using System.Collections.Generic;
using Gnosis.Database.Core;

namespace Gnosis.Database.BTree;

public class BTreeNode : IBTreeNode
{
    private static long _pageIdCounter;

    private readonly DatabaseKey[] _keys;
    private readonly DatabaseValue[] _values;
    private readonly BTreeNode?[] _children;
    private int _keyCount;
    private BTreeNode? _nextLeaf;

    private readonly int _order;
    private CompressedKeys? _compressedKeys;

    public BTreeNode(int order, bool isLeaf)
    {
        _order = order;
        _keys = new DatabaseKey[order];
        _values = new DatabaseValue[order];
        _children = new BTreeNode[order + 1];
        _keyCount = 0;
        IsLeaf = isLeaf;
        PageId = new PageId(Interlocked.Increment(ref _pageIdCounter));
    }

    public PageId PageId { get; }

    public bool IsLeaf { get; }

    public int KeyCount => _keyCount;

    public IReadOnlyList<DatabaseKey> Keys => new ReadOnlyListWrapper<DatabaseKey>(_keys, _keyCount);

    public IReadOnlyList<PageId> Children => IsLeaf
        ? new ReadOnlyListWrapper<PageId>([], 0)
        : new NodePageIdList(this);

    public IReadOnlyList<DatabaseValue> Values => IsLeaf
        ? new ReadOnlyListWrapper<DatabaseValue>(_values, _keyCount)
        : new ReadOnlyListWrapper<DatabaseValue>([], 0);

    public PageId NextLeaf => _nextLeaf?.PageId ?? PageId.Invalid;

    public bool IsFull => _keyCount >= _order - 1;

    public bool IsUnderflow => _keyCount < (_order - 1) / 2;

    internal BTreeNode? NextLeafNode => _nextLeaf;

    internal BTreeNode?[] ChildrenNodes => _children;

    internal DatabaseKey[] KeysInternal => _keys;

    internal DatabaseValue[] ValuesInternal => _values;

    internal int MaxKeys => _order - 1;

    internal int MinKeys => (_order - 1) / 2;

    internal void SetNextLeaf(BTreeNode? node)
    {
        _nextLeaf = node;
    }

    internal int FindKeyIndex(DatabaseKey key)
    {
        if (_compressedKeys.HasValue && !IsLeaf)
        {
            return FindKeyIndexCompressed(key);
        }

        var low = 0;
        var high = _keyCount - 1;

        while (low <= high)
        {
            var mid = low + (high - low) / 2;
            var cmp = _keys[mid].CompareTo(key);
            if (cmp == 0)
            {
                return mid;
            }

            if (cmp < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }

    private int FindKeyIndexCompressed(DatabaseKey key)
    {
        var compressed = _compressedKeys!.Value;
        var low = 0;
        var high = _keyCount - 1;

        while (low <= high)
        {
            var mid = low + (high - low) / 2;
            var reconstructed = PrefixCompressor.ReconstructKey(compressed, mid);
            var cmp = reconstructed.CompareTo(key);
            if (cmp == 0)
            {
                return mid;
            }

            if (cmp < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }

    internal void CompressInternalKeys()
    {
        // 暂时禁用前缀压缩，直到稳定性问题解决
        // if (IsLeaf || _keyCount < 2)
        // {
        //     return;
        // }
        //
        // var activeKeys = new DatabaseKey[_keyCount];
        // for (var i = 0; i < _keyCount; i++)
        // {
        //     activeKeys[i] = _keys[i];
        // }
        //
        // _compressedKeys = PrefixCompressor.CompressKeys(activeKeys);
    }

    internal void DecompressInternalKeys()
    {
        if (!_compressedKeys.HasValue || IsLeaf)
        {
            return;
        }

        var decompressed = PrefixCompressor.DecompressKeys(_compressedKeys.Value);
        for (var i = 0; i < decompressed.Length && i < _keys.Length; i++)
        {
            _keys[i] = decompressed[i];
        }

        _compressedKeys = null;
    }

    internal void InsertKeyAt(int index, DatabaseKey key, DatabaseValue value)
    {
        DecompressInternalKeys();

        for (var i = _keyCount; i > index; i--)
        {
            _keys[i] = _keys[i - 1];
            if (IsLeaf)
            {
                _values[i] = _values[i - 1];
            }
        }

        _keys[index] = key;
        if (IsLeaf)
        {
            _values[index] = value;
        }

        if (!IsLeaf)
        {
            for (var i = _keyCount + 1; i > index + 1; i--)
            {
                _children[i] = _children[i - 1];
            }
        }

        _keyCount++;
    }

    internal void InsertChildAt(int index, BTreeNode? child)
    {
        for (var i = _children.Length - 1; i > index; i--)
        {
            _children[i] = _children[i - 1];
        }

        _children[index] = child;
    }

    internal void RemoveKeyAt(int index)
    {
        DecompressInternalKeys();

        for (var i = index; i < _keyCount - 1; i++)
        {
            _keys[i] = _keys[i + 1];
            if (IsLeaf)
            {
                _values[i] = _values[i + 1];
            }
        }

        _keys[_keyCount - 1] = default;
        if (IsLeaf)
        {
            _values[_keyCount - 1] = default;
        }

        _keyCount--;
    }

    internal void RemoveChildAt(int index)
    {
        for (var i = index; i < _children.Length - 1; i++)
        {
            _children[i] = _children[i + 1];
        }

        _children[_children.Length - 1] = null;
    }

    internal BTreeNode Split()
    {
        var mid = _keyCount / 2;
        var right = new BTreeNode(_order, IsLeaf);

        if (IsLeaf)
        {
            for (var i = mid; i < _keyCount; i++)
            {
                right._keys[i - mid] = _keys[i];
                right._values[i - mid] = _values[i];
            }

            right._keyCount = _keyCount - mid;
            _keyCount = mid;

            right._nextLeaf = _nextLeaf;
            _nextLeaf = right;
        }
        else
        {
            for (var i = mid + 1; i < _keyCount; i++)
            {
                right._keys[i - mid - 1] = _keys[i];
                right._children[i - mid - 1] = _children[i];
            }

            right._children[_keyCount - mid - 1] = _children[_keyCount];
            right._keyCount = _keyCount - mid - 1;
            _keyCount = mid;
        }

        return right;
    }

    internal DatabaseKey GetMiddleKey()
    {
        return _keys[_keyCount / 2];
    }

    internal void MergeWithRight(BTreeNode right, DatabaseKey parentKey)
    {
        if (!IsLeaf)
        {
            _keys[_keyCount] = parentKey;
            _keyCount++;
        }

        for (var i = 0; i < right._keyCount; i++)
        {
            _keys[_keyCount] = right._keys[i];
            if (IsLeaf)
            {
                _values[_keyCount] = right._values[i];
            }

            if (!IsLeaf)
            {
                _children[_keyCount] = right._children[i];
            }

            _keyCount++;
        }

        if (!IsLeaf)
        {
            _children[_keyCount] = right._children[right._keyCount];
        }

        if (IsLeaf)
        {
            _nextLeaf = right._nextLeaf;
        }
    }

    internal void BorrowFromLeft(BTreeNode left, ref DatabaseKey parentKey)
    {
        if (IsLeaf)
        {
            InsertKeyAt(0, left._keys[left._keyCount - 1], left._values[left._keyCount - 1]);
            left.RemoveKeyAt(left._keyCount - 1);
            parentKey = _keys[0];
        }
        else
        {
            _children[_keyCount + 1] = _children[_keyCount];
            for (var i = _keyCount; i > 0; i--)
            {
                _keys[i] = _keys[i - 1];
                _children[i] = _children[i - 1];
            }

            _keys[0] = parentKey;
            _children[0] = left._children[left._keyCount];
            _keyCount++;

            parentKey = left._keys[left._keyCount - 1];
            left._keys[left._keyCount - 1] = default;
            left._children[left._keyCount] = null;
            left._keyCount--;
        }
    }

    internal void BorrowFromRight(BTreeNode right, ref DatabaseKey parentKey)
    {
        if (IsLeaf)
        {
            InsertKeyAt(_keyCount, right._keys[0], right._values[0]);
            right.RemoveKeyAt(0);
            parentKey = right._keys[0];
        }
        else
        {
            _keys[_keyCount] = parentKey;
            _children[_keyCount + 1] = right._children[0];
            _keyCount++;

            parentKey = right._keys[0];

            for (var i = 0; i < right._keyCount - 1; i++)
            {
                right._keys[i] = right._keys[i + 1];
                right._children[i] = right._children[i + 1];
            }

            right._children[right._keyCount - 1] = right._children[right._keyCount];
            right._keys[right._keyCount - 1] = default;
            right._children[right._keyCount] = null;
            right._keyCount--;
        }
    }
}
