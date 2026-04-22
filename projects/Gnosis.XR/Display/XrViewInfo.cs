using Gnosis.Core.Math;
using Gnosis.XR.Tracking;

namespace Gnosis.XR.Display;

/// <summary>
/// XR 单眼视图信息，包含视图矩阵、投影矩阵与视场角
/// </summary>
public readonly record struct XrViewInfo
{
    /// <summary>
    /// 视图矩阵（行主序 4x4）
    /// </summary>
    public float[] ViewMatrix { get; init; }

    /// <summary>
    /// 投影矩阵（行主序 4x4）
    /// </summary>
    public float[] ProjectionMatrix { get; init; }

    /// <summary>
    /// 眼睛姿态
    /// </summary>
    public XrPose Pose { get; init; }

    /// <summary>
    /// 水平视场角（弧度）
    /// </summary>
    public float FieldOfViewHorizontal { get; init; }

    /// <summary>
    /// 垂直视场角（弧度）
    /// </summary>
    public float FieldOfViewVertical { get; init; }

    /// <summary>
    /// 近裁剪面距离
    /// </summary>
    public float NearPlane { get; init; }

    /// <summary>
    /// 远裁剪面距离
    /// </summary>
    public float FarPlane { get; init; }

    /// <summary>
    /// 视图是否有效
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// 创建默认的无效视图信息
    /// </summary>
    public static XrViewInfo Empty => new()
    {
        ViewMatrix = new float[16],
        ProjectionMatrix = new float[16],
        Pose = XrPose.Identity,
        FieldOfViewHorizontal = 0,
        FieldOfViewVertical = 0,
        NearPlane = 0.01f,
        FarPlane = 1000.0f,
        IsValid = false
    };
}
