using Gnosis.XR.Tracking;

namespace Gnosis.XR.Composition;

/// <summary>
/// 柱面图层信息，在 3D 空间中显示一个柱面纹理
/// </summary>
public sealed class XrCylinderLayer : XrLayerInfo
{
    /// <summary>
    /// 图层中心姿态（位置 + 旋转）
    /// </summary>
    public XrPose Pose { get; set; }

    /// <summary>
    /// 柱面半径（米）
    /// </summary>
    public float Radius { get; set; }

    /// <summary>
    /// 柱面中央角度（弧度，沿水平方向展开的角度）
    /// </summary>
    public float CentralAngle { get; set; }

    /// <summary>
    /// 柱面纵横比
    /// </summary>
    public float AspectRatio { get; set; }

    /// <summary>
    /// 初始化柱面图层
    /// </summary>
    public XrCylinderLayer() : base(XrLayerType.Cylinder)
    {
        Pose = XrPose.Identity;
        Radius = 1.0f;
        CentralAngle = MathF.PI * 0.5f;
        AspectRatio = 1.0f;
    }
}
