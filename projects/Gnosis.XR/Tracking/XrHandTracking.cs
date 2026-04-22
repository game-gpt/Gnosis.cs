using Gnosis.Core.Math;

namespace Gnosis.XR.Tracking;

/// <summary>
/// 手部骨骼关节索引
/// </summary>
public enum XrHandJoint
{
    Palm = 0,
    Wrist = 1,
    ThumbMetacarpal = 2,
    ThumbProximal = 3,
    ThumbDistal = 4,
    ThumbTip = 5,
    IndexMetacarpal = 6,
    IndexProximal = 7,
    IndexIntermediate = 8,
    IndexDistal = 9,
    IndexTip = 10,
    MiddleMetacarpal = 11,
    MiddleProximal = 12,
    MiddleIntermediate = 13,
    MiddleDistal = 14,
    MiddleTip = 15,
    RingMetacarpal = 16,
    RingProximal = 17,
    RingIntermediate = 18,
    RingDistal = 19,
    RingTip = 20,
    LittleMetacarpal = 21,
    LittleProximal = 22,
    LittleIntermediate = 23,
    LittleDistal = 24,
    LittleTip = 25,
    Count = 26
}

/// <summary>
/// 手部关节姿态
/// </summary>
public readonly record struct XrHandJointPose
{
    /// <summary>
    /// 关节姿态
    /// </summary>
    public XrPose Pose { get; init; }

    /// <summary>
    /// 关节半径
    /// </summary>
    public float Radius { get; init; }

    /// <summary>
    /// 位置是否有效
    /// </summary>
    public bool IsPositionValid { get; init; }

    /// <summary>
    /// 旋转是否有效
    /// </summary>
    public bool IsRotationValid { get; init; }
}

/// <summary>
/// 手部追踪数据
/// </summary>
public sealed class XrHandTrackingData
{
    /// <summary>
    /// 手部关节姿态数组（长度为 XrHandJoint.Count = 26）
    /// </summary>
    public XrHandJointPose[] JointPoses { get; }

    /// <summary>
    /// 是否正在追踪
    /// </summary>
    public bool IsTracking { get; set; }

    /// <summary>
    /// 手部位置准确度（0.0 ~ 1.0）
    /// </summary>
    public float PositionAccuracy { get; set; }

    /// <summary>
    /// 手部朝向准确度（0.0 ~ 1.0）
    /// </summary>
    public float OrientationAccuracy { get; set; }

    /// <summary>
    /// 初始化手部追踪数据
    /// </summary>
    public XrHandTrackingData()
    {
        JointPoses = new XrHandJointPose[(int)XrHandJoint.Count];
        IsTracking = false;
        PositionAccuracy = 0;
        OrientationAccuracy = 0;
    }

    /// <summary>
    /// 获取指定关节的姿态
    /// </summary>
    /// <param name="joint">关节索引</param>
    /// <returns>关节姿态</returns>
    public XrHandJointPose GetJointPose(XrHandJoint joint)
    {
        return JointPoses[(int)joint];
    }

    /// <summary>
    /// 设置指定关节的姿态
    /// </summary>
    /// <param name="joint">关节索引</param>
    /// <param name="pose">关节姿态</param>
    public void SetJointPose(XrHandJoint joint, XrHandJointPose pose)
    {
        JointPoses[(int)joint] = pose;
    }
}
