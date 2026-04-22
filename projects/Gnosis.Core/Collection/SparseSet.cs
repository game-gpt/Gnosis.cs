using System.Runtime.CompilerServices;

namespace Gnosis.Core.Collection;

/// <summary>
/// 稀疏集，O(1) 插入、删除、查找，缓存友好的紧凑存储
/// </summary>
public sealed class SparseSet<T> where T : unmanaged
{
    #region 字段

    private int[] _sparse;
    private int[] _dense;
    private T[] _values;
    private int _count;

    #endregion

    #region 属性

    /// <summary>
    /// 当前元素数量
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// 最大键值加一
    /// </summary>
    public int Capacity => _sparse.Length;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建指定容量的稀疏集
    /// </summary>
    public SparseSet(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "容量必须大于 0");
        }

        _sparse = new int[capacity];
        _dense = new int[capacity];
        _values = new T[capacity];
        _count = 0;

        Array.Fill(_sparse, -1);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 检查指定键是否存在
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int key)
    {
        if ((uint)key >= (uint)_sparse.Length)
        {
            return false;
        }

        var denseIndex = _sparse[key];
        return denseIndex >= 0 && denseIndex < _count && _dense[denseIndex] == key;
    }

    /// <summary>
    /// 插入或更新指定键的值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int key, T value)
    {
        if ((uint)key >= (uint)_sparse.Length)
        {
            Resize(key + 1);
        }

        var denseIndex = _sparse[key];
        if (denseIndex >= 0 && denseIndex < _count && _dense[denseIndex] == key)
        {
            _values[denseIndex] = value;
            return;
        }

        _sparse[key] = _count;
        _dense[_count] = key;
        _values[_count] = value;
        _count++;
    }

    /// <summary>
    /// 获取指定键的值，不存在返回 false
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(int key, out T value)
    {
        if ((uint)key >= (uint)_sparse.Length)
        {
            value = default;
            return false;
        }

        var denseIndex = _sparse[key];
        if (denseIndex >= 0 && denseIndex < _count && _dense[denseIndex] == key)
        {
            value = _values[denseIndex];
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// 获取或设置指定键的值
    /// </summary>
    public T this[int key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (!TryGetValue(key, out var value))
            {
                throw new KeyNotFoundException($"稀疏集中不存在键：{key}");
            }

            return value;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Set(key, value);
    }

    /// <summary>
    /// 移除指定键
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(int key)
    {
        if ((uint)key >= (uint)_sparse.Length)
        {
            return false;
        }

        var denseIndex = _sparse[key];
        if (denseIndex < 0 || denseIndex >= _count || _dense[denseIndex] != key)
        {
            return false;
        }

        var lastKey = _dense[_count - 1];
        _sparse[lastKey] = denseIndex;
        _dense[denseIndex] = lastKey;
        _values[denseIndex] = _values[_count - 1];

        _sparse[key] = -1;
        _count--;

        return true;
    }

    /// <summary>
    /// 清空所有元素
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        for (var i = 0; i < _count; i++)
        {
            _sparse[_dense[i]] = -1;
        }

        _count = 0;
    }

    /// <summary>
    /// 获取紧凑存储的值跨度，适合高效遍历
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetValues()
    {
        return _values.AsSpan(0, _count);
    }

    /// <summary>
    /// 获取紧凑存储的键跨度
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<int> GetKeys()
    {
        return _dense.AsSpan(0, _count);
    }

    #endregion

    #region 私有方法

    private void Resize(int newCapacity)
    {
        var oldSparse = _sparse;
        _sparse = new int[newCapacity];
        Array.Fill(_sparse, -1);

        Array.Copy(oldSparse, _sparse, oldSparse.Length);

        var newDense = new int[newCapacity];
        Array.Copy(_dense, newDense, _count);
        _dense = newDense;

        var newValues = new T[newCapacity];
        Array.Copy(_values, newValues, _count);
        _values = newValues;
    }

    #endregion
}
