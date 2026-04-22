namespace Gnosis.Navigation.NavMesh;

/// <summary>
/// 导航网格实现类
/// </summary>
public class NavMesh : INavMesh
{
    #region 字段

    private readonly List<NavMeshPolygon> _polygons = new();
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
    public bool IsPointWalkable(float[] point)
    {
        if (!_isBuilt || _polygons.Count == 0)
        {
            return false;
        }

        var polygon = FindPolygon(point);
        return polygon != null;
    }

    /// <summary>
    /// 获取最近可行走点
    /// </summary>
    public float[] GetClosestPoint(float[] point)
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
            var dx = projected[0] - point[0];
            var dz = projected[2] - point[2];
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
    public NavMeshPolygon? FindPolygon(float[] point)
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
        _polygons.AddRange(polygons);
    }

    /// <summary>
    /// 获取指定 ID 的多边形
    /// </summary>
    public NavMeshPolygon? GetPolygonById(int id)
    {
        foreach (var polygon in _polygons)
        {
            if (polygon.Id == id)
            {
                return polygon;
            }
        }

        return null;
    }

    #endregion

    #region 私有方法

    private static bool IsPointInPolygon(float[] point, NavMeshPolygon polygon)
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

            if (((zi > point[2]) != (zj > point[2])) &&
                (point[0] < (xj - xi) * (point[2] - zi) / (zj - zi) + xi))
            {
                inside = !inside;
            }

            j = i;
        }

        return inside;
    }

    private static float[] ProjectPointToPolygon(float[] point, NavMeshPolygon polygon)
    {
        var vertexCount = polygon.Vertices.Length / 3;

        var closestDist = float.MaxValue;
        var closestX = point[0];
        var closestY = point[1];
        var closestZ = point[2];

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
                t = Math.Clamp(((point[0] - ax) * dx + (point[2] - az) * dz) / lenSq, 0.0f, 1.0f);
            }

            var projX = ax + t * dx;
            var projZ = az + t * dz;
            var projY = polygon.Vertices[i * 3 + 1] + t * (polygon.Vertices[j * 3 + 1] - polygon.Vertices[i * 3 + 1]);

            var distX = point[0] - projX;
            var distZ = point[2] - projZ;
            var dist = distX * distX + distZ * distZ;

            if (dist < closestDist)
            {
                closestDist = dist;
                closestX = projX;
                closestY = projY;
                closestZ = projZ;
            }
        }

        return new float[] { closestX, closestY, closestZ };
    }

    #endregion
}
