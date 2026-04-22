using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Gnosis.Core.Memory;

/// <summary>
/// 堆内存分配器，使用 NativeMemory 进行对齐内存分配，线程安全
/// </summary>
public class HeapAllocator : IAllocator
{
    #region 字段

    private readonly Dictionary<nint, AllocationInfo> _allocations = new();
    private readonly object _lock = new();
    private bool _disposed;

    #endregion

    #region 属性

    /// <summary>
    /// 已分配的总内存大小（字节）
    /// </summary>
    public nuint TotalAllocated { get; private set; }

    /// <summary>
    /// 已释放的总内存大小（字节）
    /// </summary>
    public nuint TotalFreed { get; private set; }

    /// <summary>
    /// 当前活跃的分配数量
    /// </summary>
    public int ActiveAllocations
    {
        get
        {
            lock (_lock)
            {
                return _allocations.Count;
            }
        }
    }

    #endregion

    #region IAllocator 实现

    /// <summary>
    /// 分配指定大小和对齐方式的内存块
    /// </summary>
    /// <param name="size">要分配的内存大小（字节）</param>
    /// <param name="alignment">内存对齐字节数，默认为 16</param>
    /// <returns>分配的内存块指针</returns>
    public nint Allocate(nuint size, nuint alignment = 16)
    {
        return Allocate(size, alignment, string.Empty);
    }

    /// <summary>
    /// 分配指定大小、对齐方式和标签的内存块
    /// </summary>
    /// <param name="size">要分配的内存大小（字节）</param>
    /// <param name="alignment">内存对齐字节数</param>
    /// <param name="tag">分配标签，用于分类标识</param>
    /// <returns>分配的内存块指针</returns>
    public nint Allocate(nuint size, nuint alignment, string tag)
    {
        if (size == 0)
        {
            throw new ArgumentException("分配大小不能为零", nameof(size));
        }

        lock (_lock)
        {
            ThrowIfDisposed();

            nint ptr;
            unsafe
            {
                ptr = (nint)NativeMemory.AlignedAlloc(size, alignment);
            }

            if (ptr == 0)
            {
                throw new OutOfMemoryException($"无法分配 {size} 字节的内存");
            }

            unsafe
            {
                NativeMemory.Clear((void*)ptr, size);
            }

            var info = new AllocationInfo
            {
                Pointer = ptr,
                Size = size,
                Alignment = alignment,
                Tag = tag ?? string.Empty,
                Timestamp = DateTime.UtcNow.Ticks,
                StackTrace = Environment.StackTrace
            };

            _allocations[ptr] = info;
            TotalAllocated += size;

            return ptr;
        }
    }

    /// <summary>
    /// 释放之前分配的内存块
    /// </summary>
    /// <param name="ptr">要释放的内存块指针</param>
    public void Deallocate(nint ptr)
    {
        if (ptr == 0)
        {
            return;
        }

        lock (_lock)
        {
            ThrowIfDisposed();

            if (!_allocations.TryGetValue(ptr, out var info))
            {
                throw new InvalidOperationException($"尝试释放未分配的内存指针：0x{ptr:X}");
            }

            unsafe
            {
                NativeMemory.AlignedFree((void*)ptr);
            }

            _allocations.Remove(ptr);
            TotalFreed += info.Size;
        }
    }

    /// <summary>
    /// 重新分配内存块的大小，分配新块并复制数据后释放旧块
    /// </summary>
    /// <param name="ptr">原有内存块指针</param>
    /// <param name="newSize">新的内存大小（字节）</param>
    /// <param name="alignment">内存对齐字节数，默认为 16</param>
    /// <returns>重新分配后的内存块指针</returns>
    public nint Reallocate(nint ptr, nuint newSize, nuint alignment = 16)
    {
        if (ptr == 0)
        {
            return Allocate(newSize, alignment);
        }

        lock (_lock)
        {
            ThrowIfDisposed();

            if (!_allocations.TryGetValue(ptr, out var info))
            {
                throw new InvalidOperationException($"尝试重分配未分配的内存指针：0x{ptr:X}");
            }

            var newPtr = Allocate(newSize, alignment, info.Tag);

            unsafe
            {
                var copySize = (nuint)global::System.Math.Min((ulong)info.Size, (ulong)newSize);
                Buffer.MemoryCopy((void*)ptr, (void*)newPtr, newSize, copySize);
            }

            Deallocate(ptr);

            return newPtr;
        }
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放所有未释放的内存分配并释放资源
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }

            foreach (var kvp in _allocations)
            {
                unsafe
                {
                    NativeMemory.AlignedFree((void*)kvp.Key);
                }

                TotalFreed += kvp.Value.Size;
            }

            _allocations.Clear();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 析构函数，确保所有内存分配被释放
    /// </summary>
    ~HeapAllocator()
    {
        Dispose();
    }

    #endregion

    #region 私有方法

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HeapAllocator));
        }
    }

    #endregion
}
