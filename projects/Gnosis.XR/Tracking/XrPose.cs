using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.XR.Tracking;

/// <summary>
/// XR 空间姿态（位置 + 旋转）
/// </summary>
public readonly record struct XrPose
{
    /// <summary>
    /// 位置
    /// </summary>
    public Vector3 Position { get; init; }

    /// <summary>
    /// 旋转四元数
    /// </summary>
    public Quaternion Rotation { get; init; }

    /// <summary>
    /// 单位姿态（原点、无旋转）
    /// </summary>
    public static XrPose Identity => new()
    {
        Position = new Vector3(0, 0, 0),
        Rotation = Quaternion.CreateFromEulerAngles(0, 0, 0)
    };

    /// <summary>
    /// 获取前方向量
    /// </summary>
    public Vector3 Forward => Rotation.Rotate(new Vector3(0, 0, -1));

    /// <summary>
    /// 获取上方向量
    /// </summary>
    public Vector3 Up => Rotation.Rotate(new Vector3(0, 1, 0));

    /// <summary>
    /// 获取右方向量
    /// </summary>
    public Vector3 Right => Rotation.Rotate(new Vector3(1, 0, 0));

    /// <summary>
    /// 组合两个姿态
    /// </summary>
    public XrPose Combine(XrPose other)
    {
        return new XrPose
        {
            Position = Position + other.Rotation.Rotate(other.Position),
            Rotation = (other.Rotation * Rotation).Normalize()
        };
    }
}
