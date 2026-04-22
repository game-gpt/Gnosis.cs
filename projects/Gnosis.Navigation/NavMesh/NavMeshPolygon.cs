namespace Gnosis.Navigation.NavMesh;

/// <summary>
/// 导航多边形，描述导航网格中的一个凸多边形区域
/// </summary>
public struct NavMeshPolygon
{
    /// <summary>
    /// 多边形 ID
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// 顶点列表（每 3 个 float 为一个 3D 坐标）
    /// </summary>
    public float[] Vertices { get; init; }

    /// <summary>
    /// 邻接多边形 ID 列表
    /// </summary>
    public int[] Neighbors { get; init; }

    /// <summary>
    /// 区域 ID
    /// </summary>
    public int AreaId { get; init; }

    /// <summary>
    /// 区域类型代价
    /// </summary>
    public float AreaCost { get; init; }
}
