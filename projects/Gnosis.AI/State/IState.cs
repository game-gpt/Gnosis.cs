namespace Gnosis.AI.State;

/// <summary>
/// 状态接口
/// </summary>
public interface IState
{
    /// <summary>
    /// 状态名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 子状态机
    /// </summary>
    IStateMachine? SubStateMachine { get; }

    /// <summary>
    /// 进入状态时调用
    /// </summary>
    void OnEnter();

    /// <summary>
    /// 更新状态时调用
    /// </summary>
    void OnUpdate(float delta);

    /// <summary>
    /// 退出状态时调用
    /// </summary>
    void OnExit();
}
