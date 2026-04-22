namespace Gnosis.Core.Memory;

/// <summary>
/// 内存分配信息记录，用于跟踪单次内存分配的元数据
/// </summary>
public readonly record struct AllocationInfo
{
    /// <summary>
    /// 分配的内存块指针
    /// </summary>
    public nint Pointer { get; init; }

    /// <summary>
    /// 分配的内存大小（字节）
    /// </summary>
    public nuint Size { get; init; }

    /// <summary>
    /// 内存对齐字节数
    /// </summary>
    public nuint Alignment { get; init; }

    /// <summary>
    /// 分配标签，用于分类标识
    /// </summary>
    public string Tag { get; init; }

    /// <summary>
    /// 分配时的时间戳（ ticks ）
    /// </summary>
    public long Timestamp { get; init; }

    /// <summary>
    /// 分配时的堆栈跟踪信息
    /// </summary>
    public string StackTrace { get; init; }
}
