using System.Diagnostics;

namespace Gnosis.Runtime.Sandbox;

public sealed class ResourceQuota
{
    #region Properties

    public long? MaxMemoryBytes { get; set; }
    public int? MaxObjectCount { get; set; }
    public int? MaxInstructionCount { get; set; }
    public int? MaxStackDepth { get; set; }
    public int? MaxCoroutineCount { get; set; }
    public TimeSpan MaxExecutionTime { get; set; }
    public int? MaxStringSize { get; set; }
    public int? MaxArrayLength { get; set; }

    #endregion

    #region Counters

    private long _currentMemoryUsage;
    private int _currentObjectCount;
    private int _currentInstructionCount;
    private int _currentCoroutineCount;
    private readonly Stopwatch _executionTimer;
    private bool _isRunning;

    public long CurrentMemoryUsage => _currentMemoryUsage;
    public int CurrentObjectCount => _currentObjectCount;
    public int CurrentInstructionCount => _currentInstructionCount;
    public int CurrentCoroutineCount => _currentCoroutineCount;
    public TimeSpan ElapsedExecutionTime => _isRunning ? _executionTimer.Elapsed : TimeSpan.Zero;

    #endregion

    #region Constructor

    public ResourceQuota()
    {
        _executionTimer = new Stopwatch();
        _currentMemoryUsage = 0;
        _currentObjectCount = 0;
        _currentInstructionCount = 0;
        _currentCoroutineCount = 0;
        _isRunning = false;
        MaxExecutionTime = Timeout.InfiniteTimeSpan;
    }

    #endregion

    #region Factory Methods

    public static ResourceQuota Unlimited()
    {
        return new ResourceQuota();
    }

    public static ResourceQuota Default()
    {
        return new ResourceQuota
        {
            MaxMemoryBytes = 64 * 1024 * 1024,
            MaxObjectCount = 10000,
            MaxInstructionCount = 10_000_000,
            MaxStackDepth = 256,
            MaxCoroutineCount = 100,
            MaxExecutionTime = TimeSpan.FromSeconds(30),
            MaxStringSize = 65536,
            MaxArrayLength = 1024
        };
    }

    public static ResourceQuota Strict()
    {
        return new ResourceQuota
        {
            MaxMemoryBytes = 16 * 1024 * 1024,
            MaxObjectCount = 1000,
            MaxInstructionCount = 1_000_000,
            MaxStackDepth = 64,
            MaxCoroutineCount = 10,
            MaxExecutionTime = TimeSpan.FromSeconds(5),
            MaxStringSize = 4096,
            MaxArrayLength = 128
        };
    }

    #endregion

    #region Quota Check Methods

    public bool CanAllocateMemory(long size)
    {
        if (!MaxMemoryBytes.HasValue || size <= 0)
        {
            return true;
        }

        return _currentMemoryUsage + size <= MaxMemoryBytes.Value;
    }

    public bool CanCreateObject()
    {
        if (!MaxObjectCount.HasValue)
        {
            return true;
        }

        return _currentObjectCount < MaxObjectCount.Value;
    }

    public bool CanExecuteInstruction()
    {
        if (!MaxInstructionCount.HasValue)
        {
            return true;
        }

        return _currentInstructionCount < MaxInstructionCount.Value;
    }

    public bool CanPushFrame(int currentDepth)
    {
        if (!MaxStackDepth.HasValue)
        {
            return true;
        }

        return currentDepth < MaxStackDepth.Value;
    }

    public bool CanCreateCoroutine()
    {
        if (!MaxCoroutineCount.HasValue)
        {
            return true;
        }

        return _currentCoroutineCount < MaxCoroutineCount.Value;
    }

    public bool IsWithinTimeLimit()
    {
        if (MaxExecutionTime == Timeout.InfiniteTimeSpan || !_isRunning)
        {
            return true;
        }

        return _executionTimer.Elapsed < MaxExecutionTime;
    }

    public bool IsValidStringLength(int length)
    {
        if (!MaxStringSize.HasValue)
        {
            return true;
        }

        return length <= MaxStringSize.Value;
    }

    public bool IsValidArrayLength(int length)
    {
        if (!MaxArrayLength.HasValue)
        {
            return true;
        }

        return length <= MaxArrayLength.Value;
    }

    #endregion

    #region Counter Update Methods

    public void RecordAllocation(long size)
    {
        _currentMemoryUsage += Math.Max(0, size);
    }

    public void RecordDeallocation(long size)
    {
        _currentMemoryUsage = Math.Max(0, _currentMemoryUsage - Math.Max(0, size));
    }

    public void RecordObjectCreated()
    {
        _currentObjectCount++;
    }

    public void RecordObjectDestroyed()
    {
        _currentObjectCount = Math.Max(0, _currentObjectCount - 1);
    }

    public void RecordInstructionExecuted()
    {
        _currentInstructionCount++;
    }

    public void RecordCoroutineCreated()
    {
        _currentCoroutineCount++;
    }

    public void RecordCoroutineDestroyed()
    {
        _currentCoroutineCount = Math.Max(0, _currentCoroutineCount - 1);
    }

    public void StartTiming()
    {
        _isRunning = true;
        _executionTimer.Restart();
    }

    public void StopTiming()
    {
        _isRunning = false;
        _executionTimer.Stop();
    }

    #endregion

    #region Reset

    public void Reset()
    {
        _currentMemoryUsage = 0;
        _currentObjectCount = 0;
        _currentInstructionCount = 0;
        _currentCoroutineCount = 0;
        _executionTimer.Reset();
        _isRunning = false;
    }

    #endregion
}
