using System;

namespace Gnosis.Core.Memory;

/// <summary>
/// 内存分配器接口，提供内存的分配、释放和重分配操作
/// </summary>
public interface IAllocator : IDisposable
{
    /// <summary>
    /// 分配指定大小和对齐方式的内存块
    /// </summary>
    /// <param name="size">要分配的内存大小（字节）</param>
    /// <param name="alignment">内存对齐字节数，默认为 16</param>
    /// <returns>分配的内存块指针</returns>
    nint Allocate(nuint size, nuint alignment = 16);

    /// <summary>
    /// 释放之前分配的内存块
    /// </summary>
    /// <param name="ptr">要释放的内存块指针</param>
    void Deallocate(nint ptr);

    /// <summary>
    /// 重新分配内存块的大小
    /// </summary>
    /// <param name="ptr">原有内存块指针</param>
    /// <param name="newSize">新的内存大小（字节）</param>
    /// <param name="alignment">内存对齐字节数，默认为 16</param>
    /// <returns>重新分配后的内存块指针</returns>
    nint Reallocate(nint ptr, nuint newSize, nuint alignment = 16);

    /// <summary>
    /// 已分配的总内存大小（字节）
    /// </summary>
    nuint TotalAllocated { get; }

    /// <summary>
    /// 已释放的总内存大小（字节）
    /// </summary>
    nuint TotalFreed { get; }

    /// <summary>
    /// 当前活跃的分配数量
    /// </summary>
    int ActiveAllocations { get; }
}
