using Gnosis.Core.Math;
using Gnosis.Navigation.NavMesh;

namespace Gnosis.Navigation.Query;

/// <summary>
/// 导航网格查询器实现类
/// </summary>
public class NavMeshQuery : INavMeshQuery
{
    #region 字段

    private readonly INavMesh _navMesh;

    #endregion

    #region 构造函数

    public NavMeshQuery(INavMesh navMesh)
    {
        _navMesh = navMesh;
    }

    #endregion

    #region INavMeshQuery 实现

    /// <summary>
    /// 查询最近可行走点
    /// </summary>
    public Vector3 FindClosestPoint(Vector3 point, float maxDistance)
    {
        if (!_navMesh.IsBuilt)
        {
            return point;
        }

        var closest = _navMesh.GetClosestPoint(point);
        var dx = closest.X - point.X;
        var dz = closest.Z - point.Z;
        var dist = MathF.Sqrt(dx * dx + dz * dz);

        if (dist > maxDistance)
        {
            return point;
        }

        return closest;
    }

    /// <summary>
    /// 射线投射检测
    /// </summary>
    public bool Raycast(Vector3 start, Vector3 end, out Vector3 hitPoint, out float hitDistance)
    {
        hitPoint = end;
        hitDistance = 0.0f;

        if (!_navMesh.IsBuilt)
        {
            return false;
        }

        var dir = end - start;
        var totalDist = MathF.Sqrt(dir.X * dir.X + dir.Z * dir.Z);

        if (totalDist < 1e-6f)
        {
            hitPoint = start;
            hitDistance = 0.0f;
            return _navMesh.IsPointWalkable(start);
        }

        var steps = Math.Max(1, (int)(totalDist / 0.5f));
        var stepDist = totalDist / steps;

        for (var i = 0; i <= steps; i++)
        {
            var t = (float)i / steps;
            var testPoint = Vector3.Lerp(start, end, t);

            if (!_navMesh.IsPointWalkable(testPoint))
            {
                hitPoint = testPoint;
                hitDistance = i * stepDist;
                return true;
            }
        }

        hitDistance = totalDist;
        return false;
    }

    /// <summary>
    /// 检测点是否在导航网格上
    /// </summary>
    public bool IsPointOnNavMesh(Vector3 point)
    {
        return _navMesh.IsPointWalkable(point);
    }

    /// <summary>
    /// 获取指定半径内的可行走点
    /// </summary>
    public IReadOnlyList<Vector3> FindPointsInRadius(Vector3 center, float radius, int maxResults = 16)
    {
        var results = new List<Vector3>();
        var radiusSq = radius * radius;

        foreach (var polygon in _navMesh.Polygons)
        {
            var vertexCount = polygon.Vertices.Length / 3;
            for (var i = 0; i < vertexCount; i++)
            {
                var px = polygon.Vertices[i * 3];
                var py = polygon.Vertices[i * 3 + 1];
                var pz = polygon.Vertices[i * 3 + 2];

                var dx = px - center.X;
                var dz = pz - center.Z;

                if (dx * dx + dz * dz <= radiusSq)
                {
                    results.Add(new Vector3(px, py, pz));
                    if (results.Count >= maxResults)
                    {
                        return results;
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// 获取多边形面积
    /// </summary>
    public float GetPolygonArea(int polygonId)
    {
        var polygon = _navMesh.FindPolygon(Vector3.Zero);
        if (polygon == null)
        {
            return 0.0f;
        }

        var vertexCount = polygon.Value.Vertices.Length / 3;
        var area = 0.0f;

        for (var i = 0; i < vertexCount; i++)
        {
            var j = (i + 1) % vertexCount;
            var xi = polygon.Value.Vertices[i * 3];
            var zi = polygon.Value.Vertices[i * 3 + 2];
            var xj = polygon.Value.Vertices[j * 3];
            var zj = polygon.Value.Vertices[j * 3 + 2];

            area += xi * zj - xj * zi;
        }

        return MathF.Abs(area) * 0.5f;
    }

    #endregion
}
