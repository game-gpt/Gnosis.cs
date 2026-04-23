using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.XR.Tracking;

/// <summary>
/// XR 追踪快照，记录某一时刻的追踪数据
/// </summary>
public readonly record struct XrTrackingSnapshot
{
    /// <summary>
    /// 姿态
    /// </summary>
    public XrPose Pose { get; init; }

    /// <summary>
    /// 线速度
    /// </summary>
    public Vector3 LinearVelocity { get; init; }

    /// <summary>
    /// 角速度
    /// </summary>
    public Vector3 AngularVelocity { get; init; }

    /// <summary>
    /// 追踪状态标志
    /// </summary>
    public XrTrackingFlags Flags { get; init; }

    /// <summary>
    /// 时间戳（纳秒）
    /// </summary>
    public long Timestamp { get; init; }

    /// <summary>
    /// 位置是否有效
    /// </summary>
    public bool IsPositionValid => (Flags & XrTrackingFlags.PositionValid) != 0;

    /// <summary>
    /// 旋转是否有效
    /// </summary>
    public bool IsRotationValid => (Flags & XrTrackingFlags.RotationValid) != 0;

    /// <summary>
    /// 追踪是否正常
    /// </summary>
    public bool IsTrackingOk => (Flags & XrTrackingFlags.TrackingOk) != 0;
}
