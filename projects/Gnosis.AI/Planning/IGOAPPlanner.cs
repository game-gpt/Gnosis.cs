namespace Gnosis.AI.Planning;

/// <summary>
/// GOAP 规划器接口
/// </summary>
public interface IGOAPPlanner
{
    /// <summary>
    /// 可用动作集合
    /// </summary>
    IReadOnlyList<IGOAPAction> AvailableActions { get; }

    /// <summary>
    /// 添加可用动作
    /// </summary>
    void AddAction(IGOAPAction action);

    /// <summary>
    /// 移除可用动作
    /// </summary>
    void RemoveAction(string actionName);

    /// <summary>
    /// 规划从当前状态到目标状态的动作序列
    /// </summary>
    IReadOnlyList<IGOAPAction>? Plan(IWorldState currentState, IWorldState goalState);

    /// <summary>
    /// 重置规划器
    /// </summary>
    void Reset();
}
