using Gnosis.XR.Tracking;

namespace Gnosis.XR.Input;

/// <summary>
/// XR 动作接口，表示一个可绑定的 XR 输入动作
/// </summary>
public interface IXrAction
{
    /// <summary>
    /// 动作名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 动作类型
    /// </summary>
    XrActionType ActionType { get; }

    /// <summary>
    /// 是否启用
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 当前动作状态
    /// </summary>
    XrActionState CurrentState { get; }

    /// <summary>
    /// 动作绑定列表
    /// </summary>
    IReadOnlyList<XrActionBinding> Bindings { get; }

    /// <summary>
    /// 动作状态变更事件
    /// </summary>
    event EventHandler<XrActionStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// 添加绑定
    /// </summary>
    /// <param name="binding">动作绑定</param>
    void AddBinding(XrActionBinding binding);

    /// <summary>
    /// 移除绑定
    /// </summary>
    /// <param name="path">绑定路径</param>
    void RemoveBinding(string path);

    /// <summary>
    /// 更新动作状态（每帧调用）
    /// </summary>
    void Update();
}

/// <summary>
/// XR 动作状态变更事件参数
/// </summary>
public class XrActionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 动作名称
    /// </summary>
    public string ActionName { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public XrActionState CurrentState { get; }

    /// <summary>
    /// 前一状态
    /// </summary>
    public XrActionState PreviousState { get; }

    /// <summary>
    /// 初始化动作状态变更事件参数
    /// </summary>
    public XrActionStateChangedEventArgs(string actionName, XrActionState currentState, XrActionState previousState)
    {
        ActionName = actionName;
        CurrentState = currentState;
        PreviousState = previousState;
    }
}
