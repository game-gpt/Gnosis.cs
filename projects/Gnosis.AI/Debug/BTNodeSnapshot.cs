using Gnosis.AI.Behavior;

namespace Gnosis.AI.Debug;

/// <summary>
/// 行为树节点执行快照，记录节点在某次 Tick 中的状态
/// </summary>
public struct BTNodeSnapshot
{
    /// <summary>
    /// 节点名称
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 节点类型
    /// </summary>
    public BTNodeType NodeType { get; init; }

    /// <summary>
    /// 执行结果状态
    /// </summary>
    public BTNodeStatus Status { get; init; }

    /// <summary>
    /// 节点在树中的深度（根节点为 0）
    /// </summary>
    public int Depth { get; init; }

    /// <summary>
    /// 在父节点子列表中的索引
    /// </summary>
    public int ChildIndex { get; init; }

    /// <summary>
    /// 子节点快照数量
    /// </summary>
    public int ChildCount { get; init; }

    /// <summary>
    /// 是否为当前执行路径上的节点
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Tick 时间戳（帧号）
    /// </summary>
    public long FrameNumber { get; init; }

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    public float ExecutionTimeMs { get; init; }
}
