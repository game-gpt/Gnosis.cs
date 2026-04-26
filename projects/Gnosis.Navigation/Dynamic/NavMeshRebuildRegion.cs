using Gnosis.Core.Math;

namespace Gnosis.Navigation.Dynamic;

/// <summary>
/// 导航网格重建区域，描述需要局部更新的空间范围
/// </summary>
public struct NavMeshRebuildRegion
{
    /// <summary>
    /// 区域中心
    /// </summary>
    public Vector3 Center { get; init; }

    /// <summary>
    /// 区域半尺寸
    /// </summary>
    public Vector3 HalfExtents { get; init; }

    /// <summary>
    /// 触发重建的障碍物名称
    /// </summary>
    public string SourceName { get; init; }

    /// <summary>
    /// 是否为新增障碍物（false 表示障碍物移除）
    /// </summary>
    public bool IsAdded { get; init; }

    /// <summary>
    /// 受影响的多边形 ID 列表
    /// </summary>
    public List<int> AffectedPolygonIds { get; init; }

    /// <summary>
    /// 检测点是否在重建区域内
    /// </summary>
    public readonly bool Contains(Vector3 point)
    {
        var min = Center - HalfExtents;
        var max = Center + HalfExtents;

        return point.X >= min.X && point.X <= max.X &&
               point.Y >= min.Y && point.Y <= max.Y &&
               point.Z >= min.Z && point.Z <= max.Z;
    }
}
