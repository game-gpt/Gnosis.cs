namespace Gnosis.AI.State;

/// <summary>
/// 状态过渡实现
/// </summary>
public sealed class AITransition : ITransition
{
    #region 字段

    private readonly Func<bool> _condition;
    private readonly Action? _onTransitionAction;

    #endregion

    #region 属性

    /// <summary>
    /// 源状态名称
    /// </summary>
    public string FromState { get; }

    /// <summary>
    /// 目标状态名称
    /// </summary>
    public string ToState { get; }

    /// <summary>
    /// 过渡模式
    /// </summary>
    public StateTransitionMode Mode { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建状态过渡
    /// </summary>
    /// <param name="fromState">源状态名称</param>
    /// <param name="toState">目标状态名称</param>
    /// <param name="condition">过渡条件</param>
    /// <param name="mode">过渡模式</param>
    /// <param name="onTransition">过渡执行回调</param>
    public AITransition(
        string fromState,
        string toState,
        Func<bool> condition,
        StateTransitionMode mode = StateTransitionMode.Immediate,
        Action? onTransition = null)
    {
        FromState = fromState;
        ToState = toState;
        _condition = condition;
        Mode = mode;
        _onTransitionAction = onTransition;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 过渡条件
    /// </summary>
    /// <returns>是否满足过渡条件</returns>
    public bool CanTransition()
    {
        return _condition();
    }

    /// <summary>
    /// 过渡执行时调用
    /// </summary>
    public void OnTransition()
    {
        _onTransitionAction?.Invoke();
    }

    #endregion
}
