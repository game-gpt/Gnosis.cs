using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gnosis.Core.Memory;

/// <summary>
/// 池分配器，预分配固定大小内存块，O(1) 分配和释放
/// </summary>
public sealed class PoolAllocator : IAllocator
{
    #region 字段

    private readonly unsafe byte* _buffer;
    private readonly nuint _chunkSize;
    private readonly nuint _chunkAlignment;
    private readonly int _chunkCount;
    private readonly int[] _freeList;
    private int _freeCount;
    private readonly bool[] _allocated;
    private bool _disposed;

    #endregion

    #region 属性

    /// <summary>
    /// 已分配的总内存大小
    /// </summary>
    public nuint TotalAllocated => (nuint)(_chunkCount - _freeCount) * _chunkSize;

    /// <summary>
    /// 已释放的总内存大小
    /// </summary>
    public nuint TotalFreed { get; private set; }

    /// <summary>
    /// 当前活跃的分配数量
    /// </summary>
    public int ActiveAllocations => _chunkCount - _freeCount;

    /// <summary>
    /// 可用块数量
    /// </summary>
    public int FreeCount => _freeCount;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建池分配器
    /// </summary>
    /// <param name="chunkSize">每个内存块的大小</param>
    /// <param name="chunkCount">内存块数量</param>
    /// <param name="alignment">内存对齐字节数</param>
    public PoolAllocator(nuint chunkSize, int chunkCount, nuint alignment = 16)
    {
        if (chunkSize == 0)
        {
            throw new ArgumentException("块大小不能为零", nameof(chunkSize));
        }

        if (chunkCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkCount), "块数量必须大于 0");
        }

        _chunkSize = chunkSize;
        _chunkAlignment = alignment;
        _chunkCount = chunkCount;
        _freeList = new int[chunkCount];
        _allocated = new bool[chunkCount];
        _freeCount = chunkCount;
        TotalFreed = 0;

        for (var i = 0; i < chunkCount; i++)
        {
            _freeList[i] = i;
            _allocated[i] = false;
        }

        unsafe
        {
            var totalSize = chunkSize * (nuint)chunkCount;
            _buffer = (byte*)NativeMemory.AlignedAlloc(totalSize, alignment);
            NativeMemory.Clear(_buffer, totalSize);
        }
    }

    #endregion

    #region IAllocator 实现

    /// <summary>
    /// 从池中分配一个内存块
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public nint Allocate(nuint size, nuint alignment = 16)
    {
        if (size == 0)
        {
            throw new ArgumentException("分配大小不能为零", nameof(size));
        }

        if (size > _chunkSize)
        {
            throw new ArgumentException($"请求大小 {size} 超过块大小 {_chunkSize}", nameof(size));
        }

        ThrowIfDisposed();

        if (_freeCount == 0)
        {
            throw new OutOfMemoryException("内存池已耗尽");
        }

        var chunkIndex = _freeList[--_freeCount];
        _allocated[chunkIndex] = true;

        unsafe
        {
            return (nint)(_buffer + (nuint)chunkIndex * (nuint)_chunkSize);
        }
    }

    /// <summary>
    /// 将内存块归还到池中
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deallocate(nint ptr)
    {
        if (ptr == 0)
        {
            return;
        }

        ThrowIfDisposed();

        unsafe
        {
            var offset = (byte*)ptr - _buffer;
            var chunkIndex = (int)((long)offset / (long)_chunkSize);

            if (chunkIndex < 0 || chunkIndex >= _chunkCount || (nuint)chunkIndex * (nuint)_chunkSize != (nuint)offset)
            {
                throw new InvalidOperationException("尝试释放不属于此池分配器的内存指针");
            }

            if (!_allocated[chunkIndex])
            {
                throw new InvalidOperationException("尝试释放未被分配的内存块");
            }

            _allocated[chunkIndex] = false;
            _freeList[_freeCount++] = chunkIndex;
            TotalFreed += _chunkSize;

            NativeMemory.Clear(_buffer + (nuint)chunkIndex * (nuint)_chunkSize, (nuint)_chunkSize);
        }
    }

    /// <summary>
    /// 池分配器不支持重新分配
    /// </summary>
    public nint Reallocate(nint ptr, nuint newSize, nuint alignment = 16)
    {
        throw new NotSupportedException("池分配器不支持重新分配");
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

    ~PoolAllocator()
    {
        Dispose();
    }

    #endregion

    #region 私有方法

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PoolAllocator));
        }
    }

    #endregion
}
