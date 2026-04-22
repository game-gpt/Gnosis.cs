namespace Gnosis.XR.Composition;

/// <summary>
/// XR 图层类型
/// </summary>
public enum XrLayerType
{
    /// <summary>
    /// 四边形图层（在 3D 空间中显示一个平面纹理）
    /// </summary>
    Quad,

    /// <summary>
    /// 柱面图层（在 3D 空间中显示一个柱面纹理）
    /// </summary>
    Cylinder,

    /// <summary>
    /// 投影图层（由头部追踪驱动的立体投影层）
    /// </summary>
    Projection
}
