using System.Runtime.CompilerServices;

namespace Gnosis.Core.Thread;

/// <summary>
/// 任务优先级
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// 任务句柄，用于追踪异步任务状态
/// </summary>
public sealed class TaskHandle
{
    #region 字段

    private volatile bool _isCompleted;

    #endregion

    #region 属性

    /// <summary>
    /// 任务是否已完成
    /// </summary>
    public bool IsCompleted => _isCompleted;

    /// <summary>
    /// 任务优先级
    /// </summary>
    public TaskPriority Priority { get; }

    #endregion

    #region 构造函数

    public TaskHandle(TaskPriority priority = TaskPriority.Normal)
    {
        Priority = priority;
        _isCompleted = false;
    }

    #endregion

    #region 内部方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MarkCompleted()
    {
        _isCompleted = true;
    }

    #endregion
}

/// <summary>
/// 任务调度器，管理任务队列和线程池
/// </summary>
public sealed class TaskScheduler : IScheduler
{
    #region 字段

    private readonly PriorityQueue<Action, TaskPriority> _taskQueue;
    private readonly List<System.Threading.Thread> _workers;
    private readonly object _lock = new();
    private readonly AutoResetEvent _signal;
    private volatile bool _isRunning;
    private volatile int _pendingCount;

    #endregion

    #region 属性

    /// <summary>
    /// 工作线程数量
    /// </summary>
    public int WorkerCount => _workers.Count;

    /// <summary>
    /// 待处理任务数量
    /// </summary>
    public int PendingCount => _pendingCount;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建任务调度器
    /// </summary>
    /// <param name="workerCount">工作线程数量，默认为处理器核心数</param>
    public TaskScheduler(int? workerCount = null)
    {
        var count = workerCount ?? Environment.ProcessorCount;
        _taskQueue = new PriorityQueue<Action, TaskPriority>();
        _workers = new List<System.Threading.Thread>(count);
        _signal = new AutoResetEvent(false);
        _isRunning = true;
        _pendingCount = 0;

        for (var i = 0; i < count; i++)
        {
            var thread = new System.Threading.Thread(WorkerLoop)
            {
                IsBackground = true,
                Name = $"Gnosis-Worker-{i}"
            };
            _workers.Add(thread);
            thread.Start();
        }
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 调度一个任务
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TaskHandle Schedule(Action action, TaskPriority priority = TaskPriority.Normal)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        var handle = new TaskHandle(priority);

        lock (_lock)
        {
            _taskQueue.Enqueue(() =>
            {
                try
                {
                    action();
                }
                finally
                {
                    handle.MarkCompleted();
                    System.Threading.Interlocked.Decrement(ref _pendingCount);
                }
            }, priority);

            _pendingCount++;
        }

        _signal.Set();
        return handle;
    }

    /// <summary>
    /// 关闭调度器，等待所有工作线程退出
    /// </summary>
    public void Shutdown()
    {
        _isRunning = false;
        _signal.Set();

        foreach (var worker in _workers)
        {
            worker.Join();
        }

        _workers.Clear();
    }

    #endregion

    #region IScheduler 实现

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(float deltaTime)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FixedUpdate(float fixedDeltaTime)
    {
    }

    #endregion

    #region 私有方法

    private void WorkerLoop()
    {
        while (_isRunning)
        {
            Action? task = null;

            lock (_lock)
            {
                if (_taskQueue.Count > 0)
                {
                    task = _taskQueue.Dequeue();
                }
            }

            if (task is not null)
            {
                task();
            }
            else
            {
                _signal.WaitOne(100);
            }
        }
    }

    #endregion
}
