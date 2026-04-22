using Gnosis.XR.Tracking;

namespace Gnosis.XR.Input;

/// <summary>
/// XR 动作绑定路径，描述动作与物理输入的映射关系
/// </summary>
public sealed record XrActionBinding
{
    /// <summary>
    /// 绑定路径（如 "xr/controller/left/trigger"、"xr/controller/right/grip"）
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// 绑定对应的追踪器类型
    /// </summary>
    public XrTrackerType TrackerType { get; init; }

    /// <summary>
    /// 绑定是否激活
    /// </summary>
    public bool IsActive { get; init; }
}
