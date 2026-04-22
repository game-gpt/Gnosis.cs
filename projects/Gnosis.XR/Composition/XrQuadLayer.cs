using Gnosis.XR.Tracking;

namespace Gnosis.XR.Composition;

/// <summary>
/// Quad 图层信息，在 3D 空间中显示一个平面纹理
/// </summary>
public sealed class XrQuadLayer : XrLayerInfo
{
    /// <summary>
    /// 图层中心姿态（位置 + 旋转）
    /// </summary>
    public XrPose Pose { get; set; }

    /// <summary>
    /// 图层宽度（米）
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// 图层高度（米）
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// 初始化 Quad 图层
    /// </summary>
    public XrQuadLayer() : base(XrLayerType.Quad)
    {
        Pose = XrPose.Identity;
        Width = 1.0f;
        Height = 1.0f;
    }
}
