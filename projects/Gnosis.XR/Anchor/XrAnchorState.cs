namespace Gnosis.XR.Anchor;

/// <summary>
/// 空间锚点状态
/// </summary>
public enum XrAnchorState
{
    /// <summary>
    /// 未知状态
    /// </summary>
    Unknown,

    /// <summary>
    /// 已创建，等待追踪
    /// </summary>
    Pending,

    /// <summary>
    /// 正在追踪（位置/旋转有效）
    /// </summary>
    Tracking,

    /// <summary>
    /// 暂时丢失追踪
    /// </summary>
    Lost,

    /// <summary>
    /// 已销毁
    /// </summary>
    Destroyed
}
