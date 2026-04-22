namespace Gnosis.Graphic.RHI;

/// <summary>
/// 图元拓扑类型，定义顶点如何组成图元
/// </summary>
public enum PrimitiveTopology
{
    PointList = 0,
    LineList = 1,
    LineStrip = 2,
    TriangleList = 3,
    TriangleStrip = 4,
    TriangleFan = 5
}
