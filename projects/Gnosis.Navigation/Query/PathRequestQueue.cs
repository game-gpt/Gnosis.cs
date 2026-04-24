using Gnosis.Navigation.NavMesh;
using Gnosis.Navigation.Path;

namespace Gnosis.Navigation.Query;

/// <summary>
/// 路径请求队列，支持异步路径计算和优先级调度。
/// 避免大量寻路请求阻塞主线程，支持每帧限制处理数量。
/// </summary>
public sealed class PathRequestQueue
{
    #region 内部类型

    /// <summary>
    /// 内部路径请求条目
    /// </summary>
    private struct PathRequestEntry
    {
        public IPathRequest Request;
        public Pathfinder Pathfinder;
        public TaskCompletionSource<IPath> CompletionSource;
        public float Priority;
    }

    #endregion

    #region 字段

    private readonly PriorityQueue<PathRequestEntry, float> _queue = new();
    private readonly Dictionary<int, PathRequestEntry> _activeRequests = new();
    private readonly List<Task<IPath>> _pendingCompletions = new();
    private int _nextRequestId;
    private int _maxRequestsPerFrame;

    #endregion

    #region 属性

    /// <summary>
    /// 队列中等待处理的请求数量
    /// </summary>
    public int PendingCount => _queue.Count;

    /// <summary>
    /// 正在处理的请求数量
    /// </summary>
    public int ActiveCount => _activeRequests.Count;

    /// <summary>
    /// 每帧最大处理请求数
    /// </summary>
    public int MaxRequestsPerFrame
    {
        get => _maxRequestsPerFrame;
        set => _maxRequestsPerFrame = Math.Max(1, value);
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建路径请求队列
    /// </summary>
    /// <param name="maxRequestsPerFrame">每帧最大处理请求数</param>
    public PathRequestQueue(int maxRequestsPerFrame = 4)
    {
        _maxRequestsPerFrame = maxRequestsPerFrame;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 入队路径请求
    /// </summary>
    /// <param name="request">路径请求</param>
    /// <param name="pathfinder">寻路器</param>
    /// <param name="priority">优先级（越小越优先）</param>
    /// <returns>路径计算任务</returns>
    public Task<IPath> Enqueue(IPathRequest request, Pathfinder pathfinder, float priority = 0.0f)
    {
        var entry = new PathRequestEntry
        {
            Request = request,
            Pathfinder = pathfinder,
            CompletionSource = new TaskCompletionSource<IPath>(),
            Priority = priority
        };

        _queue.Enqueue(entry, priority);
        return entry.CompletionSource.Task;
    }

    /// <summary>
    /// 处理队列中的路径请求（每帧调用一次）
    /// </summary>
    /// <param name="maxMilliseconds">最大处理时间（毫秒），0 表示无限制</param>
    /// <returns>本帧处理的请求数量</returns>
    public int ProcessRequests(float maxMilliseconds = 0)
    {
        var processedCount = 0;
        var startTime = Environment.TickCount64;

        while (_queue.Count > 0 && processedCount < _maxRequestsPerFrame)
        {
            if (maxMilliseconds > 0)
            {
                var elapsed = Environment.TickCount64 - startTime;
                if (elapsed >= (long)maxMilliseconds)
                {
                    break;
                }
            }

            if (!_queue.TryDequeue(out var entry, out _))
            {
                break;
            }

            var requestId = _nextRequestId++;
            _activeRequests[requestId] = entry;

            try
            {
                var path = entry.Pathfinder.FindPath(entry.Request);
                entry.CompletionSource.SetResult(path);
            }
            catch (Exception ex)
            {
                entry.CompletionSource.SetException(ex);
            }

            _activeRequests.Remove(requestId);
            processedCount++;
        }

        return processedCount;
    }

    /// <summary>
    /// 取消所有等待中的请求
    /// </summary>
    public void CancelAll()
    {
        while (_queue.TryDequeue(out var entry, out _))
        {
            entry.CompletionSource.SetCanceled();
        }

        foreach (var kvp in _activeRequests)
        {
            kvp.Value.CompletionSource.SetCanceled();
        }

        _activeRequests.Clear();
    }

    /// <summary>
    /// 清空队列
    /// </summary>
    public void Clear()
    {
        CancelAll();
    }

    #endregion
}
