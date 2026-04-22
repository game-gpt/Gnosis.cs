namespace Gnosis.AI.State;

/// <summary>
/// AI 状态实现，支持子状态机形成层次结构
/// </summary>
public sealed class AIState : IState
{
    #region 字段

    private readonly Action? _onEnterAction;
    private readonly Action<float>? _onUpdateAction;
    private readonly Action? _onExitAction;

    #endregion

    #region 属性

    /// <summary>
    /// 状态名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 子状态机
    /// </summary>
    public IStateMachine? SubStateMachine { get; set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建 AI 状态
    /// </summary>
    /// <param name="name">状态名称</param>
    /// <param name="onEnter">进入状态回调</param>
    /// <param name="onUpdate">更新状态回调</param>
    /// <param name="onExit">退出状态回调</param>
    public AIState(string name, Action? onEnter = null, Action<float>? onUpdate = null, Action? onExit = null)
    {
        Name = name;
        _onEnterAction = onEnter;
        _onUpdateAction = onUpdate;
        _onExitAction = onExit;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 进入状态时调用
    /// </summary>
    public void OnEnter()
    {
        _onEnterAction?.Invoke();

        if (SubStateMachine is not null)
        {
            SubStateMachine.Reset();
        }
    }

    /// <summary>
    /// 更新状态时调用
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void OnUpdate(float delta)
    {
        _onUpdateAction?.Invoke(delta);

        if (SubStateMachine is not null)
        {
            SubStateMachine.Update(delta);
        }
    }

    /// <summary>
    /// 退出状态时调用
    /// </summary>
    public void OnExit()
    {
        if (SubStateMachine is not null)
        {
            SubStateMachine.Reset();
        }

        _onExitAction?.Invoke();
    }

    #endregion
}
