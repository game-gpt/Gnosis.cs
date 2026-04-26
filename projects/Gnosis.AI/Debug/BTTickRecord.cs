using Gnosis.AI.Behavior;

namespace Gnosis.AI.Debug;

/// <summary>
/// 行为树 Tick 执行记录，记录一次完整 Tick 的所有节点状态
/// </summary>
public sealed class BTTickRecord
{
    #region 字段

    private readonly List<BTNodeSnapshot> _nodeSnapshots = new();

    #endregion

    #region 属性

    /// <summary>
    /// 行为树名称
    /// </summary>
    public string TreeName { get; }

    /// <summary>
    /// Tick 帧号
    /// </summary>
    public long FrameNumber { get; }

    /// <summary>
    /// Tick 开始时间
    /// </summary>
    public long TimestampMs { get; }

    /// <summary>
    /// 行为树最终状态
    /// </summary>
    public BTNodeStatus TreeStatus { get; init; }

    /// <summary>
    /// 所有节点快照
    /// </summary>
    public IReadOnlyList<BTNodeSnapshot> NodeSnapshots => _nodeSnapshots;

    /// <summary>
    /// 节点快照数量
    /// </summary>
    public int SnapshotCount => _nodeSnapshots.Count;

    /// <summary>
    /// 总执行耗时（毫秒）
    /// </summary>
    public float TotalExecutionTimeMs { get; init; }

    #endregion

    #region 构造函数

    public BTTickRecord(string treeName, long frameNumber, long timestampMs)
    {
        TreeName = treeName;
        FrameNumber = frameNumber;
        TimestampMs = timestampMs;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 添加节点快照
    /// </summary>
    public void AddSnapshot(BTNodeSnapshot snapshot)
    {
        _nodeSnapshots.Add(snapshot);
    }

    /// <summary>
    /// 获取指定深度的所有节点快照
    /// </summary>
    public IReadOnlyList<BTNodeSnapshot> GetSnapshotsAtDepth(int depth)
    {
        var result = new List<BTNodeSnapshot>();

        foreach (var snapshot in _nodeSnapshots)
        {
            if (snapshot.Depth == depth)
            {
                result.Add(snapshot);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取当前执行路径上的所有节点快照
    /// </summary>
    public IReadOnlyList<BTNodeSnapshot> GetActivePath()
    {
        var result = new List<BTNodeSnapshot>();

        foreach (var snapshot in _nodeSnapshots)
        {
            if (snapshot.IsActive)
            {
                result.Add(snapshot);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取指定名称的节点快照
    /// </summary>
    public BTNodeSnapshot? FindSnapshot(string nodeName)
    {
        foreach (var snapshot in _nodeSnapshots)
        {
            if (snapshot.Name == nodeName)
            {
                return snapshot;
            }
        }

        return null;
    }

    #endregion
}
