using Gnosis.XR.Tracking;

namespace Gnosis.XR.Anchor;

/// <summary>
/// 空间锚点创建信息
/// </summary>
public sealed record XrAnchorCreateInfo
{
    /// <summary>
    /// 锚点初始姿态
    /// </summary>
    public XrPose Pose { get; init; } = XrPose.Identity;

    /// <summary>
    /// 创建锚点时使用的追踪空间
    /// </summary>
    public XrTrackingSpace TrackingSpace { get; init; } = XrTrackingSpace.Local;

    /// <summary>
    /// 是否请求持久化
    /// </summary>
    public bool RequestPersistence { get; init; }

    /// <summary>
    /// 锚点名称（可选）
    /// </summary>
    public string Name { get; init; } = string.Empty;
}
