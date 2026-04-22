namespace Gnosis.XR.Tracking;

/// <summary>
/// XR 追踪状态标志
/// </summary>
[Flags]
public enum XrTrackingFlags
{
    /// <summary>
    /// 无
    /// </summary>
    None = 0,

    /// <summary>
    /// 位置有效
    /// </summary>
    PositionValid = 1 << 0,

    /// <summary>
    /// 旋转有效
    /// </summary>
    RotationValid = 1 << 1,

    /// <summary>
    /// 线速度有效
    /// </summary>
    LinearVelocityValid = 1 << 2,

    /// <summary>
    /// 角速度有效
    /// </summary>
    AngularVelocityValid = 1 << 3,

    /// <summary>
    /// 追踪正常
    /// </summary>
    TrackingOk = 1 << 4,

    /// <summary>
    /// 所有标志
    /// </summary>
    All = PositionValid | RotationValid | LinearVelocityValid | AngularVelocityValid | TrackingOk
}
