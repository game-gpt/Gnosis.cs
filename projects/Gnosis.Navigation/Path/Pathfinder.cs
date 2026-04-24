using Gnosis.Core.Math;
using Gnosis.Navigation.NavMesh;
using Gnosis.Navigation.Query;

namespace Gnosis.Navigation.Path;

/// <summary>
/// A* 寻路器实现类，基于导航网格多边形执行 A* 寻路算法
/// </summary>
public sealed class Pathfinder : IPathfinder
{
    #region 内部类型

    /// <summary>
    /// A* 开放节点
    /// </summary>
    private struct AStarNode
    {
        public int PolygonId;
        public int ParentPolygonId;
        public float GCost;
        public float FCost;
    }

    #endregion

    #region 字段

    private INavMesh? _navMesh;
    private readonly Dictionary<int, float> _areaCosts = new();
    private readonly PathSmoother _smoother = new();

    #endregion

    #region IPathfinder 实现

    /// <summary>
    /// 从起点到终点寻路
    /// </summary>
    public IPath FindPath(Vector3 start, Vector3 end)
    {
        if (_navMesh is null || !_navMesh.IsBuilt)
        {
            return new Path(new List<Vector3> { start });
        }

        var startPolygon = _navMesh.FindPolygon(start);
        var endPolygon = _navMesh.FindPolygon(end);

        if (startPolygon is null || endPolygon is null)
        {
            return new Path(new List<Vector3> { start });
        }

        if (startPolygon.Value.Id == endPolygon.Value.Id)
        {
            return new Path(new List<Vector3> { start, end });
        }

        var polygonPath = FindPolygonPath(startPolygon.Value.Id, endPolygon.Value.Id);
        if (polygonPath is null)
        {
            return new Path(new List<Vector3> { start });
        }

        var waypoints = ExtractWaypoints(polygonPath, start, end);
        var smoothed = _smoother.Smooth(waypoints, _navMesh);

        return new Path(smoothed);
    }

    /// <summary>
    /// 从路径请求寻路
    /// </summary>
    public IPath FindPath(IPathRequest request)
    {
        return FindPath(request.Start, request.End);
    }

    /// <summary>
    /// 更新导航网格引用
    /// </summary>
    public void UpdateNavMesh(INavMesh navMesh)
    {
        _navMesh = navMesh;
    }

    /// <summary>
    /// 设置区域代价
    /// </summary>
    public void SetAreaCost(int area, float cost)
    {
        _areaCosts[area] = cost;
    }

    #endregion

    #region 私有方法

    private List<int>? FindPolygonPath(int startId, int endId)
    {
        var polygons = _navMesh!.Polygons;
        var polygonMap = new Dictionary<int, NavMeshPolygon>();
        foreach (var polygon in polygons)
        {
            polygonMap[polygon.Id] = polygon;
        }

        var openSet = new PriorityQueue<AStarNode, float>();
        var cameFrom = new Dictionary<int, int>();
        var gScore = new Dictionary<int, float>();
        var closedSet = new HashSet<int>();

        if (!polygonMap.TryGetValue(endId, out var endPolygon))
        {
            return null;
        }

        var endCenter = GetPolygonCenter(endPolygon);

        gScore[startId] = 0;
        openSet.Enqueue(new AStarNode
        {
            PolygonId = startId,
            ParentPolygonId = -1,
            GCost = 0,
            FCost = 0
        }, 0);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current.PolygonId == endId)
            {
                return ReconstructPath(cameFrom, current.PolygonId);
            }

            if (closedSet.Contains(current.PolygonId))
            {
                continue;
            }

            closedSet.Add(current.PolygonId);

            if (!polygonMap.TryGetValue(current.PolygonId, out var currentPolygon))
            {
                continue;
            }

            foreach (var neighborId in currentPolygon.Neighbors)
            {
                if (closedSet.Contains(neighborId))
                {
                    continue;
                }

                if (!polygonMap.TryGetValue(neighborId, out var neighborPolygon))
                {
                    continue;
                }

                var moveCost = GetMoveCost(currentPolygon, neighborPolygon);
                var tentativeG = current.GCost + moveCost;

                if (!gScore.TryGetValue(neighborId, out var existingG) || tentativeG < existingG)
                {
                    cameFrom[neighborId] = current.PolygonId;
                    gScore[neighborId] = tentativeG;

                    var neighborCenter = GetPolygonCenter(neighborPolygon);
                    var heuristic = DistanceXZ(neighborCenter, endCenter);

                    openSet.Enqueue(new AStarNode
                    {
                        PolygonId = neighborId,
                        ParentPolygonId = current.PolygonId,
                        GCost = tentativeG,
                        FCost = tentativeG + heuristic
                    }, tentativeG + heuristic);
                }
            }
        }

        return null;
    }

    private static List<int> ReconstructPath(Dictionary<int, int> cameFrom, int currentId)
    {
        var path = new List<int> { currentId };

        while (cameFrom.TryGetValue(currentId, out var parentId))
        {
            path.Add(parentId);
            currentId = parentId;
        }

        path.Reverse();
        return path;
    }

    private List<Vector3> ExtractWaypoints(List<int> polygonPath, Vector3 start, Vector3 end)
    {
        var waypoints = new List<Vector3> { start };

        for (var i = 1; i < polygonPath.Count; i++)
        {
            var prevId = polygonPath[i - 1];
            var currId = polygonPath[i];

            var sharedEdgeMid = FindSharedEdgeMidpoint(prevId, currId);
            if (sharedEdgeMid is not null)
            {
                waypoints.Add(sharedEdgeMid.Value);
            }
        }

        waypoints.Add(end);
        return waypoints;
    }

    private Vector3? FindSharedEdgeMidpoint(int polygonAId, int polygonBId)
    {
        if (_navMesh is null)
        {
            return null;
        }

        var polyA = FindPolygonById(polygonAId);
        var polyB = FindPolygonById(polygonBId);

        if (polyA is null || polyB is null)
        {
            return null;
        }

        var sharedVertices = FindSharedVertices(polyA.Value, polyB.Value);
        if (sharedVertices.Count < 2)
        {
            return null;
        }

        var v0 = sharedVertices[0];
        var v1 = sharedVertices[1];

        return (v0 + v1) * 0.5f;
    }

    private NavMeshPolygon? FindPolygonById(int polygonId)
    {
        foreach (var polygon in _navMesh!.Polygons)
        {
            if (polygon.Id == polygonId)
            {
                return polygon;
            }
        }

        return null;
    }

    private static List<Vector3> FindSharedVertices(NavMeshPolygon a, NavMeshPolygon b)
    {
        var shared = new List<Vector3>();
        var vertexCountA = a.Vertices.Length / 3;
        var vertexCountB = b.Vertices.Length / 3;
        var thresholdSq = 0.01f * 0.01f;

        for (var i = 0; i < vertexCountA; i++)
        {
            var ax = a.Vertices[i * 3];
            var ay = a.Vertices[i * 3 + 1];
            var az = a.Vertices[i * 3 + 2];

            for (var j = 0; j < vertexCountB; j++)
            {
                var bx = b.Vertices[j * 3];
                var by = b.Vertices[j * 3 + 1];
                var bz = b.Vertices[j * 3 + 2];

                var dx = ax - bx;
                var dy = ay - by;
                var dz = az - bz;

                if (dx * dx + dy * dy + dz * dz < thresholdSq)
                {
                    shared.Add(new Vector3(ax, ay, az));
                    break;
                }
            }
        }

        return shared;
    }

    private float GetMoveCost(NavMeshPolygon from, NavMeshPolygon to)
    {
        var areaCost = _areaCosts.TryGetValue(to.AreaId, out var cost) ? cost : to.AreaCost;
        var fromCenter = GetPolygonCenter(from);
        var toCenter = GetPolygonCenter(to);
        return DistanceXZ(fromCenter, toCenter) * areaCost;
    }

    private static Vector3 GetPolygonCenter(NavMeshPolygon polygon)
    {
        var vertexCount = polygon.Vertices.Length / 3;
        if (vertexCount == 0)
        {
            return Vector3.Zero;
        }

        var cx = 0.0f;
        var cy = 0.0f;
        var cz = 0.0f;

        for (var i = 0; i < vertexCount; i++)
        {
            cx += polygon.Vertices[i * 3];
            cy += polygon.Vertices[i * 3 + 1];
            cz += polygon.Vertices[i * 3 + 2];
        }

        return new Vector3(cx / vertexCount, cy / vertexCount, cz / vertexCount);
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    #endregion
}
