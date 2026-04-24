using Gnosis.Core.Math;
using Gnosis.Navigation.NavMesh;

namespace Gnosis.Navigation.Tile;

/// <summary>
/// 导航网格分块实现，支持大世界流式加载
/// </summary>
public sealed class NavMeshTile : INavMeshTile
{
    #region 字段

    private readonly List<NavMeshPolygon> _polygons = new();
    private bool _isLoaded;

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
        _isLoaded = false;
    }

    /// <summary>
    /// 设置分块多边形数据
    /// </summary>
    /// <param name="polygons">多边形列表</param>
    public void SetPolygons(List<NavMeshPolygon> polygons)
    {
        _polygons.Clear();
        _polygons.AddRange(polygons);
    }

    /// <summary>
    /// 检测点是否在分块范围内
    /// </summary>
    /// <param name="point">检测点</param>
    /// <returns>是否在分块范围内</returns>
    public bool ContainsPoint(Vector3 point)
    {
        return point.X >= BoundsMin.X && point.X <= BoundsMax.X &&
               point.Y >= BoundsMin.Y && point.Y <= BoundsMax.Y &&
               point.Z >= BoundsMin.Z && point.Z <= BoundsMax.Z;
    }

    /// <summary>
    /// 在分块中查找包含指定点的多边形
    /// </summary>
    /// <param name="point">检测点</param>
    /// <returns>多边形，不存在则返回 null</returns>
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
    /// <param name="point">检测点</param>
    /// <returns>是否可行走</returns>
    public bool IsPointWalkable(Vector3 point)
    {
        if (!_isLoaded)
        {
            return false;
        }

        return FindPolygon(point) != null;
    }

    #endregion

    #region 私有方法

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
