namespace Gnosis.Profiler.Memory;

public static class MemoryTracker
{
    #region 字段

    private static long _totalAllocatedBytes;
    private static long _peakAllocatedBytes;
    private static readonly Dictionary<string, long> _allocationsByTag = new();

    #endregion

    #region 属性

    public static long TotalAllocatedBytes => _totalAllocatedBytes;

    public static long PeakAllocatedBytes => _peakAllocatedBytes;

    #endregion

    #region 公开方法

    public static void RecordAllocation(long bytes, string tag = "")
    {
        _totalAllocatedBytes += bytes;

        if (_totalAllocatedBytes > _peakAllocatedBytes)
        {
            _peakAllocatedBytes = _totalAllocatedBytes;
        }

        if (!string.IsNullOrEmpty(tag))
        {
            lock (_allocationsByTag)
            {
                if (_allocationsByTag.ContainsKey(tag))
                {
                    _allocationsByTag[tag] += bytes;
                }
                else
                {
                    _allocationsByTag[tag] = bytes;
                }
            }
        }
    }

    public static void RecordDeallocation(long bytes, string tag = "")
    {
        _totalAllocatedBytes -= bytes;

        if (!string.IsNullOrEmpty(tag))
        {
            lock (_allocationsByTag)
            {
                if (_allocationsByTag.ContainsKey(tag))
                {
                    _allocationsByTag[tag] -= bytes;
                }
            }
        }
    }

    public static Dictionary<string, long> GetAllocationsByTag()
    {
        lock (_allocationsByTag)
        {
            return new Dictionary<string, long>(_allocationsByTag);
        }
    }

    public static void Reset()
    {
        _totalAllocatedBytes = 0;
        _peakAllocatedBytes = 0;

        lock (_allocationsByTag)
        {
            _allocationsByTag.Clear();
        }
    }

    #endregion
}
