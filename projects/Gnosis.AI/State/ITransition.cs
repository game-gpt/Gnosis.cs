namespace Gnosis.AI.State;

/// <summary>
/// 状态过渡接口
/// </summary>
public interface ITransition
{
    /// <summary>
    /// 源状态名称
    /// </summary>
    string FromState { get; }

    /// <summary>
    /// 目标状态名称
    /// </summary>
    string ToState { get; }

    /// <summary>
    /// 过渡模式
    /// </summary>
    StateTransitionMode Mode { get; }

    /// <summary>
    /// 过渡条件
    /// </summary>
    bool CanTransition();

    /// <summary>
    /// 过渡执行时调用
    /// </summary>
    void OnTransition();
}
