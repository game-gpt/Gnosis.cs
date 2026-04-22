namespace Gnosis.XR.Session;

/// <summary>
/// XR 会话状态
/// </summary>
public enum XrSessionState
{
    /// <summary>
    /// 未知状态
    /// </summary>
    Unknown,

    /// <summary>
    /// 已创建但未启动
    /// </summary>
    Created,

    /// <summary>
    /// 等待 XR 运行时就绪
    /// </summary>
    Pending,

    /// <summary>
    /// 已就绪，可进入专注状态
    /// </summary>
    Ready,

    /// <summary>
    /// 专注状态（用户可见 XR 内容）
    /// </summary>
    Focused,

    /// <summary>
    /// 失去焦点（系统 UI 遮挡）
    /// </summary>
    VisibilityLoss,

    /// <summary>
    /// 请求退出
    /// </summary>
    Quitting,

    /// <summary>
    /// 运行时丢失
    /// </summary>
    RuntimeLoss,

    /// <summary>
    /// 已销毁
    /// </summary>
    Destroyed
}
