using System.Runtime.CompilerServices;

namespace Gnosis.Core.Memory;

/// <summary>
/// 内存跟踪器，记录所有分配器的分配信息，用于泄漏检测
/// </summary>
public sealed class MemoryTracker
{
    #region 嵌套类型

    /// <summary>
    /// 分配记录
    /// </summary>
    public sealed class AllocationRecord
    {
        /// <summary>
        /// 分配指针
        /// </summary>
        public nint Pointer { get; init; }

        /// <summary>
        /// 分配大小
        /// </summary>
        public nuint Size { get; init; }

        /// <summary>
        /// 分配标签
        /// </summary>
        public string Tag { get; init; } = string.Empty;

        /// <summary>
        /// 分配时间戳
        /// </summary>
        public long Timestamp { get; init; }

        /// <summary>
        /// 分配时的调用栈
        /// </summary>
        public string StackTrace { get; init; } = string.Empty;
    }

    #endregion

    #region 字段

    private readonly Dictionary<nint, AllocationRecord> _allocations = new();
    private readonly object _lock = new();

    #endregion

    #region 属性

    /// <summary>
    /// 当前活跃的分配数量
    /// </summary>
    public int ActiveAllocationCount
    {
        get
        {
            lock (_lock)
            {
                return _allocations.Count;
            }
        }
    }

    /// <summary>
    /// 当前活跃的分配总大小
    /// </summary>
    public nuint ActiveAllocationSize
    {
        get
        {
            lock (_lock)
            {
                nuint total = 0;
                foreach (var record in _allocations.Values)
                {
                    total += record.Size;
                }

                return total;
            }
        }
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 记录一次分配
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TrackAllocation(nint pointer, nuint size, string tag = "")
    {
        var record = new AllocationRecord
        {
            Pointer = pointer,
            Size = size,
            Tag = tag,
            Timestamp = DateTime.UtcNow.Ticks,
            StackTrace = Environment.StackTrace
        };

        lock (_lock)
        {
            _allocations[pointer] = record;
        }
    }

    /// <summary>
    /// 记录一次释放
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void TrackDeallocation(nint pointer)
    {
        lock (_lock)
        {
            _allocations.Remove(pointer);
        }
    }

    /// <summary>
    /// 获取所有活跃的分配记录
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<AllocationRecord> GetActiveAllocations()
    {
        lock (_lock)
        {
            return new List<AllocationRecord>(_allocations.Values);
        }
    }

    /// <summary>
    /// 按标签获取分配统计
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Dictionary<string, (int Count, nuint TotalSize)> GetAllocationStatsByTag()
    {
        lock (_lock)
        {
            var stats = new Dictionary<string, (int Count, nuint TotalSize)>();

            foreach (var record in _allocations.Values)
            {
                var tag = string.IsNullOrEmpty(record.Tag) ? "(untagged)" : record.Tag;
                if (!stats.TryGetValue(tag, out var existing))
                {
                    existing = (0, 0);
                }

                stats[tag] = (existing.Count + 1, existing.TotalSize + record.Size);
            }

            return stats;
        }
    }

    /// <summary>
    /// 检测潜在的内存泄漏，返回分配时间超过指定阈值的记录
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<AllocationRecord> DetectLeaks(TimeSpan maxAge)
    {
        var threshold = DateTime.UtcNow.Ticks - maxAge.Ticks;

        lock (_lock)
        {
            var leaks = new List<AllocationRecord>();

            foreach (var record in _allocations.Values)
            {
                if (record.Timestamp < threshold)
                {
                    leaks.Add(record);
                }
            }

            return leaks;
        }
    }

    /// <summary>
    /// 清除所有跟踪记录
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        lock (_lock)
        {
            _allocations.Clear();
        }
    }

    #endregion
}
