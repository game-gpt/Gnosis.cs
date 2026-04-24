using Gnosis.Core.Math;
using Gnosis.Navigation.NavMesh;

namespace Gnosis.Navigation.Path;

/// <summary>
/// D* Lite 动态寻路器，支持环境变化时的增量式路径重规划。
/// 基于 Sven Koenig 和 Maxim Likhachev 的 D* Lite 算法实现。
/// 当导航网格中的通行性发生变化时，只需更新受影响的边代价，
/// 而不需要完全重新计算路径。
/// </summary>
public sealed class DStarLitePathfinder
{
    #region 内部类型

    private enum NodeState : byte
    {
        New = 0,
        Open = 1,
        Closed = 2
    }

    private struct DStarNode
    {
        public int PolygonId;
        public float G;
        public float Rhs;
        public float KeyPrimary;
        public float KeySecondary;
        public NodeState State;
    }

    private struct EdgeChange
    {
        public int FromId;
        public int ToId;
        public float OldCost;
        public float NewCost;
    }

    private sealed class DStarKeyComparer : IComparer<(float Primary, float Secondary)>
    {
        public int Compare((float Primary, float Secondary) x, (float Primary, float Secondary) y)
        {
            var cmp = x.Primary.CompareTo(y.Primary);
            if (cmp != 0)
            {
                return cmp;
            }

            return x.Secondary.CompareTo(y.Secondary);
        }
    }

    #endregion

    #region 字段

    private INavMesh? _navMesh;
    private readonly Dictionary<int, DStarNode> _nodes = new();
    private readonly PriorityQueue<int, (float, float)> _openQueue;
    private readonly HashSet<int> _openSet = new();
    private readonly List<EdgeChange> _edgeChanges = new();
    private float _km;
    private int _startId;
    private int _goalId;
    private Vector3 _startPosition;
    private Vector3 _goalPosition;
    private bool _initialized;

    #endregion

    #region 构造函数

    public DStarLitePathfinder()
    {
        _openQueue = new PriorityQueue<int, (float, float)>(new DStarKeyComparer());
    }

    #endregion

    #region 属性

    public bool IsInitialized => _initialized;

    public int PendingEdgeChanges => _edgeChanges.Count;

    #endregion

    #region 公开方法

    public void SetNavMesh(INavMesh navMesh)
    {
        _navMesh = navMesh;
        _initialized = false;
    }

    public IPath Initialize(Vector3 start, Vector3 goal)
    {
        if (_navMesh is null || !_navMesh.IsBuilt)
        {
            return new Path(new List<Vector3> { start });
        }

        var startPolygon = _navMesh.FindPolygon(start);
        var goalPolygon = _navMesh.FindPolygon(goal);

        if (startPolygon is null || goalPolygon is null)
        {
            return new Path(new List<Vector3> { start });
        }

        _startId = startPolygon.Value.Id;
        _goalId = goalPolygon.Value.Id;
        _startPosition = start;
        _goalPosition = goal;
        _km = 0;

        _nodes.Clear();
        _openQueue.Clear();
        _openSet.Clear();
        _edgeChanges.Clear();

        foreach (var polygon in _navMesh.Polygons)
        {
            _nodes[polygon.Id] = new DStarNode
            {
                PolygonId = polygon.Id,
                G = float.PositiveInfinity,
                Rhs = float.PositiveInfinity,
                State = NodeState.New
            };
        }

        var goalNode = _nodes[_goalId];
        goalNode.Rhs = 0;
        var goalKey = CalculateKey(_goalId);
        goalNode.KeyPrimary = goalKey.Primary;
        goalNode.KeySecondary = goalKey.Secondary;
        goalNode.State = NodeState.Open;
        _nodes[_goalId] = goalNode;

        _openQueue.Enqueue(_goalId, goalKey);
        _openSet.Add(_goalId);

        ComputeShortestPath();

        _initialized = true;

        return ExtractPath();
    }

    public IPath UpdateStart(Vector3 newStart)
    {
        if (!_initialized || _navMesh is null)
        {
            return Initialize(newStart, _goalPosition);
        }

        var newStartPolygon = _navMesh.FindPolygon(newStart);
        if (newStartPolygon is null)
        {
            return new Path(new List<Vector3> { newStart });
        }

        var newStartId = newStartPolygon.Value.Id;

        _km += Heuristic(_startId, newStartId);
        _startId = newStartId;
        _startPosition = newStart;

        ComputeShortestPath();

        return ExtractPath();
    }

    public void UpdateEdgeCost(int fromId, int toId, float newCost)
    {
        if (!_initialized)
        {
            return;
        }

        var oldCost = GetEdgeCost(fromId, toId);

        _edgeChanges.Add(new EdgeChange
        {
            FromId = fromId,
            ToId = toId,
            OldCost = oldCost,
            NewCost = newCost
        });

        UpdateRhs(fromId);

        if (_nodes[fromId].State == NodeState.Open)
        {
            RemoveFromOpen(fromId);
        }

        if (_nodes[fromId].G != _nodes[fromId].Rhs)
        {
            var node = _nodes[fromId];
            var key = CalculateKey(fromId);
            node.KeyPrimary = key.Primary;
            node.KeySecondary = key.Secondary;
            node.State = NodeState.Open;
            _nodes[fromId] = node;
            _openQueue.Enqueue(fromId, key);
            _openSet.Add(fromId);
        }
    }

    public IPath ApplyChanges()
    {
        if (!_initialized || _edgeChanges.Count == 0)
        {
            return ExtractPath();
        }

        _edgeChanges.Clear();
        ComputeShortestPath();

        return ExtractPath();
    }

    #endregion

    #region D* Lite 核心算法

    private (float Primary, float Secondary) CalculateKey(int nodeId)
    {
        var node = _nodes[nodeId];
        var minG = Math.Min(node.G, node.Rhs);
        var h = Heuristic(nodeId, _startId);

        return (minG + h + _km, minG);
    }

    private void UpdateRhs(int nodeId)
    {
        if (nodeId == _goalId)
        {
            return;
        }

        var minRhs = float.PositiveInfinity;

        if (_navMesh is not null)
        {
            var polygon = FindPolygonById(nodeId);
            if (polygon is not null)
            {
                foreach (var neighborId in polygon.Value.Neighbors)
                {
                    if (_nodes.TryGetValue(neighborId, out var neighborNode))
                    {
                        var cost = GetEdgeCost(nodeId, neighborId);
                        var candidate = neighborNode.G + cost;

                        if (candidate < minRhs)
                        {
                            minRhs = candidate;
                        }
                    }
                }
            }
        }

        var node = _nodes[nodeId];
        node.Rhs = minRhs;
        _nodes[nodeId] = node;
    }

    private void ComputeShortestPath()
    {
        var maxIterations = _nodes.Count * 3;
        var iterations = 0;

        while (_openQueue.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            _openQueue.TryPeek(out _, out var topKey);
            var startKey = CalculateKey(_startId);

            if (KeyLessOrEqual(topKey, startKey) == false &&
                _nodes[_startId].Rhs == _nodes[_startId].G)
            {
                break;
            }

            var currentId = _openQueue.Dequeue();

            if (!_openSet.Contains(currentId))
            {
                continue;
            }

            _openSet.Remove(currentId);

            var currentKey = CalculateKey(currentId);

            if (KeyLess(currentKey, topKey))
            {
                var node = _nodes[currentId];
                node.KeyPrimary = currentKey.Primary;
                node.KeySecondary = currentKey.Secondary;
                node.State = NodeState.Open;
                _nodes[currentId] = node;
                _openQueue.Enqueue(currentId, currentKey);
                _openSet.Add(currentId);
                continue;
            }

            var current = _nodes[currentId];

            if (current.G > current.Rhs)
            {
                var node = _nodes[currentId];
                node.G = node.Rhs;
                node.State = NodeState.Closed;
                _nodes[currentId] = node;

                if (_navMesh is not null)
                {
                    var polygon = FindPolygonById(currentId);
                    if (polygon is not null)
                    {
                        foreach (var neighborId in polygon.Value.Neighbors)
                        {
                            if (neighborId != _goalId)
                            {
                                UpdateRhs(neighborId);
                            }

                            var neighbor = _nodes[neighborId];
                            if (neighbor.State == NodeState.Open)
                            {
                                RemoveFromOpen(neighborId);
                            }

                            if (neighbor.G != neighbor.Rhs)
                            {
                                var n = _nodes[neighborId];
                                var nKey = CalculateKey(neighborId);
                                n.KeyPrimary = nKey.Primary;
                                n.KeySecondary = nKey.Secondary;
                                n.State = NodeState.Open;
                                _nodes[neighborId] = n;
                                _openQueue.Enqueue(neighborId, nKey);
                                _openSet.Add(neighborId);
                            }
                        }
                    }
                }
            }
            else
            {
                var node = _nodes[currentId];
                node.G = float.PositiveInfinity;
                _nodes[currentId] = node;

                UpdateRhs(currentId);

                if (_navMesh is not null)
                {
                    var polygon = FindPolygonById(currentId);
                    if (polygon is not null)
                    {
                        foreach (var neighborId in polygon.Value.Neighbors)
                        {
                            UpdateRhs(neighborId);

                            var neighbor = _nodes[neighborId];
                            if (neighbor.G != neighbor.Rhs)
                            {
                                if (neighbor.State == NodeState.Open)
                                {
                                    RemoveFromOpen(neighborId);
                                }

                                var n = _nodes[neighborId];
                                var nKey = CalculateKey(neighborId);
                                n.KeyPrimary = nKey.Primary;
                                n.KeySecondary = nKey.Secondary;
                                n.State = NodeState.Open;
                                _nodes[neighborId] = n;
                                _openQueue.Enqueue(neighborId, nKey);
                                _openSet.Add(neighborId);
                            }
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region 路径提取

    private IPath ExtractPath()
    {
        if (_navMesh is null)
        {
            return new Path(new List<Vector3> { _startPosition });
        }

        if (!_nodes.TryGetValue(_startId, out var startNode) ||
            float.IsInfinity(startNode.G))
        {
            return new Path(new List<Vector3> { _startPosition });
        }

        var waypoints = new List<Vector3> { _startPosition };
        var currentId = _startId;
        var visited = new HashSet<int>();
        var maxSteps = _nodes.Count;

        for (var step = 0; step < maxSteps; step++)
        {
            if (currentId == _goalId)
            {
                waypoints.Add(_goalPosition);
                break;
            }

            if (visited.Contains(currentId))
            {
                break;
            }

            visited.Add(currentId);

            var bestNeighbor = -1;
            var bestCost = float.PositiveInfinity;

            var polygon = FindPolygonById(currentId);
            if (polygon is null)
            {
                break;
            }

            foreach (var neighborId in polygon.Value.Neighbors)
            {
                if (!_nodes.TryGetValue(neighborId, out var neighborNode))
                {
                    continue;
                }

                var cost = GetEdgeCost(currentId, neighborId) + neighborNode.G;

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestNeighbor = neighborId;
                }
            }

            if (bestNeighbor < 0)
            {
                break;
            }

            var neighborPolygon = FindPolygonById(bestNeighbor);
            if (neighborPolygon is not null)
            {
                var center = GetPolygonCenter(neighborPolygon.Value);
                waypoints.Add(center);
            }

            currentId = bestNeighbor;
        }

        if (waypoints.Count > 0 && waypoints[^1] != _goalPosition)
        {
            waypoints.Add(_goalPosition);
        }

        return new Path(waypoints);
    }

    #endregion

    #region 辅助方法

    private NavMeshPolygon? FindPolygonById(int polygonId)
    {
        if (_navMesh is null)
        {
            return null;
        }

        foreach (var polygon in _navMesh.Polygons)
        {
            if (polygon.Id == polygonId)
            {
                return polygon;
            }
        }

        return null;
    }

    private float GetEdgeCost(int fromId, int toId)
    {
        var fromPolygon = FindPolygonById(fromId);
        var toPolygon = FindPolygonById(toId);

        if (fromPolygon is null || toPolygon is null)
        {
            return float.PositiveInfinity;
        }

        var fromCenter = GetPolygonCenter(fromPolygon.Value);
        var toCenter = GetPolygonCenter(toPolygon.Value);

        return DistanceXZ(fromCenter, toCenter) * toPolygon.Value.AreaCost;
    }

    private float Heuristic(int fromId, int toId)
    {
        var fromPolygon = FindPolygonById(fromId);
        var toPolygon = FindPolygonById(toId);

        if (fromPolygon is null || toPolygon is null)
        {
            return 0;
        }

        var fromCenter = GetPolygonCenter(fromPolygon.Value);
        var toCenter = GetPolygonCenter(toPolygon.Value);

        return DistanceXZ(fromCenter, toCenter);
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

    private void RemoveFromOpen(int nodeId)
    {
        _openSet.Remove(nodeId);
        var node = _nodes[nodeId];
        node.State = NodeState.Closed;
        _nodes[nodeId] = node;
    }

    private static bool KeyLess((float Primary, float Secondary) a, (float Primary, float Secondary) b)
    {
        if (a.Primary < b.Primary)
        {
            return true;
        }

        if (a.Primary > b.Primary)
        {
            return false;
        }

        return a.Secondary < b.Secondary;
    }

    private static bool KeyLessOrEqual((float Primary, float Secondary) a, (float Primary, float Secondary) b)
    {
        if (a.Primary < b.Primary)
        {
            return true;
        }

        if (a.Primary > b.Primary)
        {
            return false;
        }

        return a.Secondary <= b.Secondary;
    }

    #endregion
}
