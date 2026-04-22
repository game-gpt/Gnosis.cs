using System.Runtime.CompilerServices;

namespace Gnosis.Core.Collection;

/// <summary>
/// 环形缓冲区，固定容量的先进先出队列
/// </summary>
public sealed class RingBuffer<T>
{
    #region 字段

    private readonly T[] _buffer;
    private int _head;
    private int _tail;
    private int _count;

    #endregion

    #region 属性

    /// <summary>
    /// 当前元素数量
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// 缓冲区容量
    /// </summary>
    public int Capacity => _buffer.Length;

    /// <summary>
    /// 缓冲区是否已满
    /// </summary>
    public bool IsFull => _count == _buffer.Length;

    /// <summary>
    /// 缓冲区是否为空
    /// </summary>
    public bool IsEmpty => _count == 0;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建指定容量的环形缓冲区
    /// </summary>
    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "容量必须大于 0");
        }

        _buffer = new T[capacity];
        _head = 0;
        _tail = 0;
        _count = 0;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 向缓冲区尾部写入一个元素，缓冲区满时覆盖最旧的元素
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T item)
    {
        _buffer[_tail] = item;
        _tail = (_tail + 1) % _buffer.Length;

        if (_count == _buffer.Length)
        {
            _head = (_head + 1) % _buffer.Length;
        }
        else
        {
            _count++;
        }
    }

    /// <summary>
    /// 尝试从缓冲区头部读取一个元素
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _buffer[_head];
        _buffer[_head] = default!;
        _head = (_head + 1) % _buffer.Length;
        _count--;
        return true;
    }

    /// <summary>
    /// 查看缓冲区头部元素但不移除
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeek(out T item)
    {
        if (_count == 0)
        {
            item = default!;
            return false;
        }

        item = _buffer[_head];
        return true;
    }

    /// <summary>
    /// 清空缓冲区
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (_count > 0)
        {
            Array.Clear(_buffer, 0, _buffer.Length);
        }

        _head = 0;
        _tail = 0;
        _count = 0;
    }

    /// <summary>
    /// 获取指定索引处的元素（从头部开始计数）
    /// </summary>
    public T this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "索引超出范围");
            }

            return _buffer[(_head + index) % _buffer.Length];
        }
    }

    #endregion
}
