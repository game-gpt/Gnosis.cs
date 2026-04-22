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
    public float[] FindClosestPoint(float[] point, float maxDistance)
    {
        if (!_navMesh.IsBuilt)
        {
            return point;
        }

        var closest = _navMesh.GetClosestPoint(point);
        var dx = closest[0] - point[0];
        var dz = closest[2] - point[2];
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
    public bool Raycast(float[] start, float[] end, out float[] hitPoint, out float hitDistance)
    {
        hitPoint = end;
        hitDistance = 0.0f;

        if (!_navMesh.IsBuilt)
        {
            return false;
        }

        var dirX = end[0] - start[0];
        var dirZ = end[2] - start[2];
        var totalDist = MathF.Sqrt(dirX * dirX + dirZ * dirZ);

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
            var testX = start[0] + dirX * t;
            var testZ = start[2] + dirZ * t;
            var testY = start[1] + (end[1] - start[1]) * t;

            var testPoint = new float[] { testX, testY, testZ };

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
    public bool IsPointOnNavMesh(float[] point)
    {
        return _navMesh.IsPointWalkable(point);
    }

    /// <summary>
    /// 获取指定半径内的可行走点
    /// </summary>
    public IReadOnlyList<float[]> FindPointsInRadius(float[] center, float radius, int maxResults = 16)
    {
        var results = new List<float[]>();
        var radiusSq = radius * radius;

        foreach (var polygon in _navMesh.Polygons)
        {
            var vertexCount = polygon.Vertices.Length / 3;
            for (var i = 0; i < vertexCount; i++)
            {
                var px = polygon.Vertices[i * 3];
                var py = polygon.Vertices[i * 3 + 1];
                var pz = polygon.Vertices[i * 3 + 2];

                var dx = px - center[0];
                var dz = pz - center[2];

                if (dx * dx + dz * dz <= radiusSq)
                {
                    results.Add(new float[] { px, py, pz });
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
        var polygon = _navMesh.FindPolygon(new float[3]);
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
