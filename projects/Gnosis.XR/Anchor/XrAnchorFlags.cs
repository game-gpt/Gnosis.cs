namespace Gnosis.XR.Anchor;

/// <summary>
/// 空间锚点标志
/// </summary>
[Flags]
public enum XrAnchorFlags
{
    /// <summary>
    /// 无
    /// </summary>
    None = 0,

    /// <summary>
    /// 锚点位置有效
    /// </summary>
    PositionValid = 1 << 0,

    /// <summary>
    /// 锚点旋转有效
    /// </summary>
    RotationValid = 1 << 1,

    /// <summary>
    /// 锚点可被追踪
    /// </summary>
    Trackable = 1 << 2,

    /// <summary>
    /// 锚点支持持久化
    /// </summary>
    Persistable = 1 << 3,

    /// <summary>
    /// 所有标志
    /// </summary>
    All = PositionValid | RotationValid | Trackable | Persistable
}
