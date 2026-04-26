using Gnosis.Core.Math;

namespace Gnosis.Navigation.NavMesh;

/// <summary>
/// 导航网格实现类
/// </summary>
public class NavMesh : INavMesh
{
    #region 字段

    private readonly List<NavMeshPolygon> _polygons = new();
    private readonly HashSet<int> _blockedPolygonIds = new();
    private readonly Dictionary<int, NavMeshPolygon> _polygonById = new();
    private NavMeshBuildSettings _settings;
    private bool _isBuilt;

    #endregion

    #region INavMesh 实现

    /// <summary>
    /// 导航网格名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 是否已构建
    /// </summary>
    public bool IsBuilt => _isBuilt;

    /// <summary>
    /// 构建设置
    /// </summary>
    public NavMeshBuildSettings Settings => _settings;

    /// <summary>
    /// 导航网格中的多边形列表
    /// </summary>
    public IReadOnlyList<NavMeshPolygon> Polygons => _polygons;

    #endregion

    #region 构造函数

    public NavMesh(string name)
    {
        Name = name;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 构建导航网格
    /// </summary>
    public void Build(NavMeshBuildSettings settings)
    {
        _settings = settings;
        _isBuilt = true;
    }

    /// <summary>
    /// 重新构建导航网格
    /// </summary>
    public void Rebuild()
    {
        _polygons.Clear();
        _isBuilt = false;
        Build(_settings);
    }

    /// <summary>
    /// 清空导航网格
    /// </summary>
    public void Clear()
    {
        _polygons.Clear();
        _isBuilt = false;
    }

    /// <summary>
    /// 检测点是否可行走
    /// </summary>
    public bool IsPointWalkable(Vector3 point)
    {
        if (!_isBuilt || _polygons.Count == 0)
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
    /// 获取最近可行走点
    /// </summary>
    public Vector3 GetClosestPoint(Vector3 point)
    {
        if (!_isBuilt || _polygons.Count == 0)
        {
            return point;
        }

        var closestDist = float.MaxValue;
        var closestPoint = point;

        foreach (var polygon in _polygons)
        {
            var projected = ProjectPointToPolygon(point, polygon);
            var dx = projected.X - point.X;
            var dz = projected.Z - point.Z;
            var dist = dx * dx + dz * dz;

            if (dist < closestDist)
            {
                closestDist = dist;
                closestPoint = projected;
            }
        }

        return closestPoint;
    }

    /// <summary>
    /// 根据位置查找所在多边形
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
    /// 设置多边形列表
    /// </summary>
    public void SetPolygons(List<NavMeshPolygon> polygons)
    {
        _polygons.Clear();
        _polygonById.Clear();
        _polygons.AddRange(polygons);

        foreach (var polygon in polygons)
        {
            _polygonById[polygon.Id] = polygon;
        }
    }

    /// <summary>
    /// 获取指定 ID 的多边形
    /// </summary>
    public NavMeshPolygon? GetPolygonById(int id)
    {
        return _polygonById.GetValueOrDefault(id);
    }

    /// <summary>
    /// 标记多边形为阻塞状态
    /// </summary>
    public bool SetPolygonBlocked(int polygonId, bool blocked)
    {
        if (!_polygonById.ContainsKey(polygonId))
        {
            return false;
        }

        if (blocked)
        {
            _blockedPolygonIds.Add(polygonId);
        }
        else
        {
            _blockedPolygonIds.Remove(polygonId);
        }

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
    /// 获取所有被阻塞的多边形 ID
    /// </summary>
    public IReadOnlySet<int> BlockedPolygonIds => _blockedPolygonIds;

    /// <summary>
    /// 清除所有阻塞标记
    /// </summary>
    public void ClearAllBlocked()
    {
        _blockedPolygonIds.Clear();
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

    #endregion

    #region 私有方法

    private static bool PolygonIntersectsArea(NavMeshPolygon polygon, Vector3 areaMin, Vector3 areaMax)
    {
        var vertexCount = polygon.Vertices.Length / 3;

        var polyMinX = float.MaxValue;
        var polyMinY = float.MaxValue;
        var polyMinZ = float.MaxValue;
        var polyMaxX = float.MinValue;
        var polyMaxY = float.MinValue;
        var polyMaxZ = float.MinValue;

        for (var i = 0; i < vertexCount; i++)
        {
            var x = polygon.Vertices[i * 3];
            var y = polygon.Vertices[i * 3 + 1];
            var z = polygon.Vertices[i * 3 + 2];

            if (x < polyMinX) polyMinX = x;
            if (y < polyMinY) polyMinY = y;
            if (z < polyMinZ) polyMinZ = z;
            if (x > polyMaxX) polyMaxX = x;
            if (y > polyMaxY) polyMaxY = y;
            if (z > polyMaxZ) polyMaxZ = z;
        }

        return polyMinX <= areaMax.X && polyMaxX >= areaMin.X &&
               polyMinY <= areaMax.Y && polyMaxY >= areaMin.Y &&
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

    private static Vector3 ProjectPointToPolygon(Vector3 point, NavMeshPolygon polygon)
    {
        var vertexCount = polygon.Vertices.Length / 3;

        var closestDist = float.MaxValue;
        var closestX = point.X;
        var closestY = point.Y;
        var closestZ = point.Z;

        for (var i = 0; i < vertexCount; i++)
        {
            var j = (i + 1) % vertexCount;

            var ax = polygon.Vertices[i * 3];
            var az = polygon.Vertices[i * 3 + 2];
            var bx = polygon.Vertices[j * 3];
            var bz = polygon.Vertices[j * 3 + 2];

            var dx = bx - ax;
            var dz = bz - az;
            var lenSq = dx * dx + dz * dz;

            float t;
            if (lenSq < 1e-10f)
            {
                t = 0.0f;
            }
            else
            {
                t = Math.Clamp(((point.X - ax) * dx + (point.Z - az) * dz) / lenSq, 0.0f, 1.0f);
            }

            var projX = ax + t * dx;
            var projZ = az + t * dz;
            var projY = polygon.Vertices[i * 3 + 1] + t * (polygon.Vertices[j * 3 + 1] - polygon.Vertices[i * 3 + 1]);

            var distX = point.X - projX;
            var distZ = point.Z - projZ;
            var dist = distX * distX + distZ * distZ;

            if (dist < closestDist)
            {
                closestDist = dist;
                closestX = projX;
                closestY = projY;
                closestZ = projZ;
            }
        }

        return new Vector3(closestX, closestY, closestZ);
    }

    #endregion
}
