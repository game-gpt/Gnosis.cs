namespace Gnosis.XR.Tracking;

/// <summary>
/// XR 追踪空间类型
/// </summary>
public enum XrTrackingSpace
{
    /// <summary>
    /// 本地空间（相对于头显初始位置）
    /// </summary>
    Local,

    /// <summary>
    /// 视图空间（相对于当前视图）
    /// </summary>
    View,

    /// <summary>
    /// 阶段空间（相对于地面定义的游玩区域）
    /// </summary>
    Stage,

    /// <summary>
    /// 世界空间
    /// </summary>
    World
}
