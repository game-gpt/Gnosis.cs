namespace Gnosis.AI.State;

/// <summary>
/// 层次状态机接口
/// </summary>
public interface IStateMachine
{
    /// <summary>
    /// 状态机名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    IState? CurrentState { get; }

    /// <summary>
    /// 所有状态
    /// </summary>
    IReadOnlyList<IState> States { get; }

    /// <summary>
    /// 所有过渡
    /// </summary>
    IReadOnlyList<ITransition> Transitions { get; }

    /// <summary>
    /// 添加状态
    /// </summary>
    void AddState(IState state);

    /// <summary>
    /// 移除状态
    /// </summary>
    void RemoveState(string stateName);

    /// <summary>
    /// 添加过渡
    /// </summary>
    void AddTransition(ITransition transition);

    /// <summary>
    /// 切换到指定状态
    /// </summary>
    bool SwitchTo(string stateName);

    /// <summary>
    /// 更新状态机
    /// </summary>
    void Update(float delta);

    /// <summary>
    /// 重置状态机
    /// </summary>
    void Reset();
}
