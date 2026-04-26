using System.Diagnostics;
using Gnosis.AI.Behavior;
using Gnosis.AI.Blackboard;

namespace Gnosis.AI.Debug;

/// <summary>
/// 行为树调试器，记录行为树执行历史并提供查询 API。
/// 通过包装行为树的 Tick 调用来捕获每个节点的执行快照。
/// </summary>
public sealed class BehaviorTreeDebugger
{
    #region 字段

    private readonly IBehaviorTree _tree;
    private readonly LinkedList<BTTickRecord> _history = new();
    private readonly Dictionary<string, BTNodeSnapshot> _latestNodeStates = new();
    private readonly HashSet<string> _watchedNodes = new();
    private readonly List<BTNodeStatus> _statusHistory = new();
    private int _maxHistoryLength = 300;
    private long _frameNumber;
    private bool _isEnabled = true;
    private bool _recordAllNodes = true;

    #endregion

    #region 属性

    /// <summary>
    /// 关联的行为树
    /// </summary>
    public IBehaviorTree Tree => _tree;

    /// <summary>
    /// 是否启用调试记录
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    /// <summary>
    /// 是否记录所有节点（包括未执行的节点）
    /// </summary>
    public bool RecordAllNodes
    {
        get => _recordAllNodes;
        set => _recordAllNodes = value;
    }

    /// <summary>
    /// 最大历史记录长度
    /// </summary>
    public int MaxHistoryLength
    {
        get => _maxHistoryLength;
        set => _maxHistoryLength = Math.Max(1, value);
    }

    /// <summary>
    /// 历史记录数量
    /// </summary>
    public int HistoryCount => _history.Count;

    /// <summary>
    /// 最新一次 Tick 记录
    /// </summary>
    public BTTickRecord? LatestRecord => _history.Last?.Value;

    /// <summary>
    /// 行为树状态变化历史
    /// </summary>
    public IReadOnlyList<BTNodeStatus> StatusHistory => _statusHistory;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建行为树调试器
    /// </summary>
    /// <param name="tree">要调试的行为树</param>
    /// <param name="maxHistoryLength">最大历史记录长度</param>
    public BehaviorTreeDebugger(IBehaviorTree tree, int maxHistoryLength = 300)
    {
        _tree = tree;
        _maxHistoryLength = maxHistoryLength;
    }

    #endregion

    #region 公开方法 - 调试 Tick

    /// <summary>
    /// 执行带调试记录的行为树 Tick
    /// </summary>
    /// <returns>行为树执行状态</returns>
    public BTNodeStatus DebugTick()
    {
        if (!_isEnabled)
        {
            return _tree.Tick();
        }

        _frameNumber++;
        var stopwatch = Stopwatch.StartNew();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var record = new BTTickRecord(_tree.Name, _frameNumber, timestamp);

        var treeStatus = _tree.Tick();

        stopwatch.Stop();

        if (_tree.Root is not null)
        {
            CaptureNodeSnapshot(_tree.Root, 0, 0, record, true);
        }

        record = new BTTickRecord(_tree.Name, _frameNumber, timestamp)
        {
            TreeStatus = treeStatus,
            TotalExecutionTimeMs = (float)stopwatch.Elapsed.TotalMilliseconds
        };

        if (_tree.Root is not null)
        {
            CaptureNodeSnapshot(_tree.Root, 0, 0, record, true);
        }

        AddRecord(record);
        _statusHistory.Add(treeStatus);

        return treeStatus;
    }

    #endregion

    #region 公开方法 - 监视节点

    /// <summary>
    /// 添加监视节点
    /// </summary>
    /// <param name="nodeName">节点名称</param>
    public void WatchNode(string nodeName)
    {
        _watchedNodes.Add(nodeName);
    }

    /// <summary>
    /// 移除监视节点
    /// </summary>
    /// <param name="nodeName">节点名称</param>
    public void UnwatchNode(string nodeName)
    {
        _watchedNodes.Remove(nodeName);
    }

    /// <summary>
    /// 获取监视节点的最新状态
    /// </summary>
    public IReadOnlyDictionary<string, BTNodeSnapshot> GetWatchedNodeStates()
    {
        var result = new Dictionary<string, BTNodeSnapshot>();

        foreach (var nodeName in _watchedNodes)
        {
            if (_latestNodeStates.TryGetValue(nodeName, out var snapshot))
            {
                result[nodeName] = snapshot;
            }
        }

        return result;
    }

    #endregion

    #region 公开方法 - 历史查询

    /// <summary>
    /// 获取指定帧号的 Tick 记录
    /// </summary>
    public BTTickRecord? GetRecord(long frameNumber)
    {
        foreach (var record in _history)
        {
            if (record.FrameNumber == frameNumber)
            {
                return record;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取最近 N 条记录
    /// </summary>
    public IReadOnlyList<BTTickRecord> GetRecentRecords(int count)
    {
        var result = new List<BTTickRecord>();
        var current = _history.Last;

        while (current is not null && result.Count < count)
        {
            result.Add(current.Value);
            current = current.Previous;
        }

        result.Reverse();
        return result;
    }

    /// <summary>
    /// 获取指定节点的最新快照
    /// </summary>
    public BTNodeSnapshot? GetLatestNodeSnapshot(string nodeName)
    {
        return _latestNodeStates.GetValueOrDefault(nodeName);
    }

    /// <summary>
    /// 获取所有节点的最新快照
    /// </summary>
    public IReadOnlyDictionary<string, BTNodeSnapshot> GetAllLatestSnapshots()
    {
        return _latestNodeStates;
    }

    /// <summary>
    /// 获取当前执行路径（最新记录中的活跃节点路径）
    /// </summary>
    public IReadOnlyList<BTNodeSnapshot> GetCurrentActivePath()
    {
        var latest = LatestRecord;
        return latest?.GetActivePath() ?? new List<BTNodeSnapshot>();
    }

    /// <summary>
    /// 获取节点状态变化统计
    /// </summary>
    public Dictionary<string, int> GetNodeStatusCounts()
    {
        var counts = new Dictionary<string, int>();

        foreach (var kvp in _latestNodeStates)
        {
            var statusKey = kvp.Value.Status.ToString();
            var key = $"{kvp.Key}:{statusKey}";

            if (!counts.ContainsKey(key))
            {
                counts[key] = 0;
            }

            counts[key]++;
        }

        return counts;
    }

    /// <summary>
    /// 清空历史记录
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
        _latestNodeStates.Clear();
        _statusHistory.Clear();
    }

    #endregion

    #region 私有方法

    private void CaptureNodeSnapshot(IBTNode node, int depth, int childIndex, BTTickRecord record, bool isActive)
    {
        var snapshot = new BTNodeSnapshot
        {
            Name = node.Name,
            NodeType = node.NodeType,
            Status = node.Status,
            Depth = depth,
            ChildIndex = childIndex,
            ChildCount = GetChildCount(node),
            IsActive = isActive,
            FrameNumber = _frameNumber
        };

        record.AddSnapshot(snapshot);
        _latestNodeStates[node.Name] = snapshot;

        if (!_recordAllNodes && !isActive)
        {
            return;
        }

        if (node is CompositeNode composite)
        {
            for (var i = 0; i < composite.Children.Count; i++)
            {
                var childIsActive = isActive && IsChildActive(composite, i);
                CaptureNodeSnapshot(composite.Children[i], depth + 1, i, record, childIsActive);
            }
        }
        else if (node is DecoratorNode decorator && decorator.Child is not null)
        {
            CaptureNodeSnapshot(decorator.Child, depth + 1, 0, record, isActive);
        }
    }

    private static int GetChildCount(IBTNode node)
    {
        if (node is CompositeNode composite)
        {
            return composite.Children.Count;
        }

        if (node is DecoratorNode decorator)
        {
            return decorator.Child is not null ? 1 : 0;
        }

        return 0;
    }

    private static bool IsChildActive(CompositeNode composite, int childIndex)
    {
        if (composite is BTNodes.Sequence sequence)
        {
            return childIndex == 0;
        }

        if (composite is BTNodes.Selector selector)
        {
            return childIndex == 0;
        }

        return true;
    }

    private void AddRecord(BTTickRecord record)
    {
        _history.AddLast(record);

        while (_history.Count > _maxHistoryLength)
        {
            _history.RemoveFirst();
        }
    }

    #endregion
}
