using Gnosis.Core.Math;
using Gnosis.Navigation.Dynamic;
using Gnosis.Navigation.NavMesh;

namespace Gnosis.Navigation.Tile;

/// <summary>
/// 导航网格分块实现，支持大世界流式加载和局部重建
/// </summary>
public sealed class NavMeshTile : INavMeshTile
{
    #region 字段

    private readonly List<NavMeshPolygon> _polygons = new();
    private readonly HashSet<int> _blockedPolygonIds = new();
    private bool _isLoaded;
    private bool _needsRebuild;

    #endregion

    #region INavMeshTile 实现

    /// <summary>
    /// 分块坐标
    /// </summary>
    public TileCoord Coord { get; }

    /// <summary>
    /// 是否已加载
    /// </summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>
    /// 分块中的多边形数量
    /// </summary>
    public int PolygonCount => _polygons.Count;

    /// <summary>
    /// 分块边界（最小点）
    /// </summary>
    public Vector3 BoundsMin { get; }

    /// <summary>
    /// 分块边界（最大点）
    /// </summary>
    public Vector3 BoundsMax { get; }

    #endregion

    #region 属性

    /// <summary>
    /// 分块中的多边形列表
    /// </summary>
    public IReadOnlyList<NavMeshPolygon> Polygons => _polygons;

    /// <summary>
    /// 分块大小
    /// </summary>
    public Vector3 Size => BoundsMax - BoundsMin;

    /// <summary>
    /// 分块中心
    /// </summary>
    public Vector3 Center => (BoundsMin + BoundsMax) * 0.5f;

    /// <summary>
    /// 是否需要重建
    /// </summary>
    public bool NeedsRebuild => _needsRebuild;

    /// <summary>
    /// 被阻塞的多边形 ID 集合
    /// </summary>
    public IReadOnlySet<int> BlockedPolygonIds => _blockedPolygonIds;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建导航网格分块
    /// </summary>
    /// <param name="coord">分块坐标</param>
    /// <param name="boundsMin">边界最小点</param>
    /// <param name="boundsMax">边界最大点</param>
    public NavMeshTile(TileCoord coord, Vector3 boundsMin, Vector3 boundsMax)
    {
        Coord = coord;
        BoundsMin = boundsMin;
        BoundsMax = boundsMax;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 加载分块数据
    /// </summary>
    public void Load()
    {
        _isLoaded = true;
    }

    /// <summary>
    /// 卸载分块数据
    /// </summary>
    public void Unload()
    {
        _polygons.Clear();
        _blockedPolygonIds.Clear();
        _isLoaded = false;
        _needsRebuild = false;
    }

    /// <summary>
    /// 设置分块多边形数据
    /// </summary>
    /// <param name="polygons">多边形列表</param>
    public void SetPolygons(List<NavMeshPolygon> polygons)
    {
        _polygons.Clear();
        _polygons.AddRange(polygons);
        _needsRebuild = false;
    }

    /// <summary>
    /// 检测点是否在分块范围内
    /// </summary>
    public bool ContainsPoint(Vector3 point)
    {
        return point.X >= BoundsMin.X && point.X <= BoundsMax.X &&
               point.Y >= BoundsMin.Y && point.Y <= BoundsMax.Y &&
               point.Z >= BoundsMin.Z && point.Z <= BoundsMax.Z;
    }

    /// <summary>
    /// 在分块中查找包含指定点的多边形
    /// </summary>
    public NavMeshPolygon? FindPolygon(Vector3 point)
    {
        foreach (var polygon in _polygons)
        {
            if (IsPointInPolygon(point, polygon))
            {
                return polygon;
            }
        }

        return null;
    }

    /// <summary>
    /// 检测点是否在分块中可行走
    /// </summary>
    public bool IsPointWalkable(Vector3 point)
    {
        if (!_isLoaded)
        {
            return false;
        }

        var polygon = FindPolygon(point);

        if (polygon is null)
        {
            return false;
        }

        return !_blockedPolygonIds.Contains(polygon.Value.Id);
    }

    /// <summary>
    /// 标记多边形为阻塞状态
    /// </summary>
    public bool SetPolygonBlocked(int polygonId, bool blocked)
    {
        if (blocked)
        {
            _blockedPolygonIds.Add(polygonId);
        }
        else
        {
            _blockedPolygonIds.Remove(polygonId);
        }

        _needsRebuild = true;
        return true;
    }

    /// <summary>
    /// 检测多边形是否被阻塞
    /// </summary>
    public bool IsPolygonBlocked(int polygonId)
    {
        return _blockedPolygonIds.Contains(polygonId);
    }

    /// <summary>
    /// 查找与指定区域相交的所有多边形
    /// </summary>
    public List<int> FindPolygonsInArea(Vector3 center, Vector3 halfExtents)
    {
        var result = new List<int>();
        var min = center - halfExtents;
        var max = center + halfExtents;

        foreach (var polygon in _polygons)
        {
            if (PolygonIntersectsArea(polygon, min, max))
            {
                result.Add(polygon.Id);
            }
        }

        return result;
    }

    /// <summary>
    /// 应用重建区域到分块
    /// </summary>
    public void ApplyRebuildRegion(NavMeshRebuildRegion region)
    {
        if (!ContainsPoint(region.Center) && !RegionOverlaps(region))
        {
            return;
        }

        foreach (var polygonId in region.AffectedPolygonIds)
        {
            SetPolygonBlocked(polygonId, region.IsAdded);
        }
    }

    /// <summary>
    /// 清除所有阻塞标记
    /// </summary>
    public void ClearAllBlocked()
    {
        _blockedPolygonIds.Clear();
        _needsRebuild = true;
    }

    /// <summary>
    /// 标记分块需要重建
    /// </summary>
    public void MarkNeedsRebuild()
    {
        _needsRebuild = true;
    }

    /// <summary>
    /// 清除重建标记
    /// </summary>
    public void ClearRebuildFlag()
    {
        _needsRebuild = false;
    }

    #endregion

    #region 私有方法

    private bool RegionOverlaps(NavMeshRebuildRegion region)
    {
        var regionMin = region.Center - region.HalfExtents;
        var regionMax = region.Center + region.HalfExtents;

        return regionMin.X <= BoundsMax.X && regionMax.X >= BoundsMin.X &&
               regionMin.Y <= BoundsMax.Y && regionMax.Y >= BoundsMin.Y &&
               regionMin.Z <= BoundsMax.Z && regionMax.Z >= BoundsMin.Z;
    }

    private static bool PolygonIntersectsArea(NavMeshPolygon polygon, Vector3 areaMin, Vector3 areaMax)
    {
        var vertexCount = polygon.Vertices.Length / 3;

        var polyMinX = float.MaxValue;
        var polyMinZ = float.MaxValue;
        var polyMaxX = float.MinValue;
        var polyMaxZ = float.MinValue;

        for (var i = 0; i < vertexCount; i++)
        {
            var x = polygon.Vertices[i * 3];
            var z = polygon.Vertices[i * 3 + 2];

            if (x < polyMinX) polyMinX = x;
            if (z < polyMinZ) polyMinZ = z;
            if (x > polyMaxX) polyMaxX = x;
            if (z > polyMaxZ) polyMaxZ = z;
        }

        return polyMinX <= areaMax.X && polyMaxX >= areaMin.X &&
               polyMinZ <= areaMax.Z && polyMaxZ >= areaMin.Z;
    }

    private static bool IsPointInPolygon(Vector3 point, NavMeshPolygon polygon)
    {
        var vertexCount = polygon.Vertices.Length / 3;
        var inside = false;

        var j = vertexCount - 1;
        for (var i = 0; i < vertexCount; i++)
        {
            var xi = polygon.Vertices[i * 3];
            var zi = polygon.Vertices[i * 3 + 2];
            var xj = polygon.Vertices[j * 3];
            var zj = polygon.Vertices[j * 3 + 2];

            if (((zi > point.Z) != (zj > point.Z)) &&
                (point.X < (xj - xi) * (point.Z - zi) / (zj - zi) + xi))
            {
                inside = !inside;
            }

            j = i;
        }

        return inside;
    }

    #endregion
}
