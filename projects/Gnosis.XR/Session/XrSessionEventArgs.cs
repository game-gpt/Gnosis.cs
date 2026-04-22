namespace Gnosis.XR.Session;

/// <summary>
/// XR 会话事件参数
/// </summary>
public class XrSessionEventArgs : EventArgs
{
    #region 属性

    /// <summary>
    /// 当前会话状态
    /// </summary>
    public XrSessionState State { get; }

    /// <summary>
    /// 前一个会话状态
    /// </summary>
    public XrSessionState PreviousState { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 XR 会话事件参数
    /// </summary>
    public XrSessionEventArgs(XrSessionState state, XrSessionState previousState)
    {
        State = state;
        PreviousState = previousState;
    }

    #endregion
}
