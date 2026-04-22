namespace Gnosis.AI.Planning;

/// <summary>
/// GOAP 动作接口
/// </summary>
public interface IGOAPAction
{
    /// <summary>
    /// 动作名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 动作代价
    /// </summary>
    float Cost { get; }

    /// <summary>
    /// 前置条件
    /// </summary>
    IWorldState Preconditions { get; }

    /// <summary>
    /// 执行效果
    /// </summary>
    IWorldState Effects { get; }

    /// <summary>
    /// 是否可在当前世界状态下执行
    /// </summary>
    bool IsExecutable(IWorldState worldState);

    /// <summary>
    /// 执行动作，返回是否成功
    /// </summary>
    bool Execute(float delta);
}
