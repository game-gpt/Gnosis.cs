using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gnosis.Core.Memory;

/// <summary>
/// 栈分配器，LIFO 顺序分配和释放，支持嵌套作用域
/// </summary>
public sealed class StackAllocator : IAllocator
{
    #region 字段

    private readonly unsafe byte* _buffer;
    private readonly nuint _capacity;
    private nuint _offset;
    private bool _disposed;

    #endregion

    #region 属性

    /// <summary>
    /// 已分配的总内存大小
    /// </summary>
    public nuint TotalAllocated => _offset;

    /// <summary>
    /// 已释放的总内存大小
    /// </summary>
    public nuint TotalFreed { get; private set; }

    /// <summary>
    /// 当前活跃的分配数量
    /// </summary>
    public int ActiveAllocations { get; private set; }

    /// <summary>
    /// 当前栈顶偏移量，可用于保存/恢复
    /// </summary>
    public nuint CurrentOffset => _offset;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建栈分配器
    /// </summary>
    /// <param name="capacity">缓冲区大小（字节）</param>
    public StackAllocator(nuint capacity)
    {
        if (capacity == 0)
        {
            throw new ArgumentException("容量不能为零", nameof(capacity));
        }

        _capacity = capacity;
        _offset = 0;
        TotalFreed = 0;
        ActiveAllocations = 0;

        unsafe
        {
            _buffer = (byte*)NativeMemory.AlignedAlloc(capacity, 16);
            NativeMemory.Clear(_buffer, capacity);
        }
    }

    #endregion

    #region IAllocator 实现

    /// <summary>
    /// 从栈顶分配指定大小的内存块
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nint Allocate(nuint size, nuint alignment = 16)
    {
        if (size == 0)
        {
            throw new ArgumentException("分配大小不能为零", nameof(size));
        }

        ThrowIfDisposed();

        unsafe
        {
            var currentPtr = (nuint)_buffer + _offset;
            var alignedPtr = (currentPtr + alignment - 1) & ~(alignment - 1);
            var newOffset = alignedPtr + size - (nuint)_buffer;

            if (newOffset > _capacity)
            {
                throw new OutOfMemoryException($"栈分配器空间不足：请求 {size} 字节，剩余 {_capacity - _offset} 字节");
            }

            _offset = newOffset;
            ActiveAllocations++;
            return (nint)alignedPtr;
        }
    }

    /// <summary>
    /// 释放指定指针的内存（栈分配器仅支持回滚到指定偏移量）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deallocate(nint ptr)
    {
        ThrowIfDisposed();

        unsafe
        {
            var bufferStart = (nuint)_buffer;
            var ptrValue = (nuint)ptr;

            if (ptrValue < bufferStart || ptrValue >= bufferStart + _capacity)
            {
                throw new InvalidOperationException("尝试释放不属于此栈分配器的内存指针");
            }

            if (ActiveAllocations > 0)
            {
                ActiveAllocations--;
            }
        }
    }

    /// <summary>
    /// 栈分配器不支持重新分配
    /// </summary>
    public nint Reallocate(nint ptr, nuint newSize, nuint alignment = 16)
    {
        throw new NotSupportedException("栈分配器不支持重新分配");
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 保存当前栈顶偏移量
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nuint SaveMarker()
    {
        return _offset;
    }

    /// <summary>
    /// 回滚到指定偏移量
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RollbackTo(nuint marker)
    {
        ThrowIfDisposed();

        if (marker > _offset)
        {
            throw new ArgumentException("回滚标记不能大于当前偏移量", nameof(marker));
        }

        var freed = _offset - marker;
        TotalFreed += freed;
        _offset = marker;
        ActiveAllocations = 0;

        unsafe
        {
            NativeMemory.Clear(_buffer + marker, freed);
        }
    }

    /// <summary>
    /// 重置栈分配器，释放所有分配
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        ThrowIfDisposed();

        TotalFreed += _offset;
        _offset = 0;
        ActiveAllocations = 0;

        unsafe
        {
            NativeMemory.Clear(_buffer, _capacity);
        }
    }

    #endregion

    #region IDisposable 实现

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        unsafe
        {
            NativeMemory.AlignedFree(_buffer);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~StackAllocator()
    {
        Dispose();
    }

    #endregion

    #region 私有方法

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(StackAllocator));
        }
    }

    #endregion
}
