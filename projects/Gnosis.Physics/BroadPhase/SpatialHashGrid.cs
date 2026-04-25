using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.BroadPhase;

public sealed class SpatialHashGrid
{
    #region 常量

    private const int InitialCellCapacity = 4;

    #endregion

    #region 字段

    private readonly float _cellSize;
    private readonly float _invCellSize;
    private readonly Dictionary<long, List<SpatialProxy>> _cells = new();
    private readonly List<SpatialProxy> _proxies = new();
    private readonly Dictionary<IRigidBody, int> _bodyToProxyIndex = new();

    #endregion

    #region 属性

    public int ProxyCount => _proxies.Count;

    public int CellCount => _cells.Count;

    #endregion

    #region 构造函数

    public SpatialHashGrid(float cellSize = 2.0f)
    {
        if (cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), "单元格大小必须大于 0");
        }

        _cellSize = cellSize;
        _invCellSize = 1f / cellSize;
    }

    #endregion

    #region 公开方法 - 代理管理

    public void Insert(IRigidBody body)
    {
        if (_bodyToProxyIndex.ContainsKey(body))
        {
            Update(body);
            return;
        }

        var aabb = ComputeAabb(body);
        var proxy = new SpatialProxy(body, aabb);
        _proxies.Add(proxy);
        _bodyToProxyIndex[body] = _proxies.Count - 1;

        InsertProxyIntoCells(proxy);
    }

    public void Remove(IRigidBody body)
    {
        if (!_bodyToProxyIndex.TryGetValue(body, out var index))
        {
            return;
        }

        var proxy = _proxies[index];
        RemoveProxyFromCells(proxy);

        var lastIndex = _proxies.Count - 1;
        if (index != lastIndex)
        {
            _proxies[index] = _proxies[lastIndex];
            _bodyToProxyIndex[_proxies[index].Body] = index;
        }

        _proxies.RemoveAt(lastIndex);
        _bodyToProxyIndex.Remove(body);
    }

    public void Update(IRigidBody body)
    {
        if (!_bodyToProxyIndex.TryGetValue(body, out var index))
        {
            return;
        }

        var proxy = _proxies[index];
        var newAabb = ComputeAabb(body);

        if (proxy.Aabb == newAabb)
        {
            return;
        }

        RemoveProxyFromCells(proxy);

        proxy.Aabb = newAabb;
        _proxies[index] = proxy;

        InsertProxyIntoCells(proxy);
    }

    public void UpdateAll()
    {
        for (var i = 0; i < _proxies.Count; i++)
        {
            var proxy = _proxies[i];
            var newAabb = ComputeAabb(proxy.Body);

            if (proxy.Aabb != newAabb)
            {
                RemoveProxyFromCells(proxy);
                proxy.Aabb = newAabb;
                _proxies[i] = proxy;
                InsertProxyIntoCells(proxy);
            }
        }
    }

    public void Clear()
    {
        _cells.Clear();
        _proxies.Clear();
        _bodyToProxyIndex.Clear();
    }

    #endregion

    #region 公开方法 - 查询

    public void QueryPairs(List<(IRigidBody, IRigidBody)> pairs)
    {
        var visited = new HashSet<(int, int)>();

        foreach (var kvp in _cells)
        {
            var cellProxies = kvp.Value;

            for (var i = 0; i < cellProxies.Count; i++)
            {
                for (var j = i + 1; j < cellProxies.Count; j++)
                {
                    var proxyA = cellProxies[i];
                    var proxyB = cellProxies[j];

                    var indexA = _bodyToProxyIndex.TryGetValue(proxyA.Body, out var ia) ? ia : -1;
                    var indexB = _bodyToProxyIndex.TryGetValue(proxyB.Body, out var ib) ? ib : -1;

                    if (indexA < 0 || indexB < 0)
                    {
                        continue;
                    }

                    var key = indexA < indexB ? (indexA, indexB) : (indexB, indexA);

                    if (visited.Contains(key))
                    {
                        continue;
                    }

                    visited.Add(key);

                    if (proxyA.Aabb.Intersects(proxyB.Aabb))
                    {
                        pairs.Add((proxyA.Body, proxyB.Body));
                    }
                }
            }
        }
    }

    public void QueryAabb(BoundingBox aabb, List<IRigidBody> results)
    {
        var minCellX = CellKey(aabb.Min.X);
        var minCellY = CellKey(aabb.Min.Y);
        var minCellZ = CellKey(aabb.Min.Z);
        var maxCellX = CellKey(aabb.Max.X);
        var maxCellY = CellKey(aabb.Max.Y);
        var maxCellZ = CellKey(aabb.Max.Z);

        var found = new HashSet<IRigidBody>();

        for (var x = minCellX; x <= maxCellX; x++)
        {
            for (var y = minCellY; y <= maxCellY; y++)
            {
                for (var z = minCellZ; z <= maxCellZ; z++)
                {
                    var cellKey = CombineCellKey(x, y, z);

                    if (!_cells.TryGetValue(cellKey, out var cellProxies))
                    {
                        continue;
                    }

                    foreach (var proxy in cellProxies)
                    {
                        if (proxy.Aabb.Intersects(aabb) && found.Add(proxy.Body))
                        {
                            results.Add(proxy.Body);
                        }
                    }
                }
            }
        }
    }

    public void QueryPoint(Vector3 point, List<IRigidBody> results)
    {
        var cellX = CellKey(point.X);
        var cellY = CellKey(point.Y);
        var cellZ = CellKey(point.Z);
        var cellKey = CombineCellKey(cellX, cellY, cellZ);

        if (!_cells.TryGetValue(cellKey, out var cellProxies))
        {
            return;
        }

        foreach (var proxy in cellProxies)
        {
            if (proxy.Aabb.Contains(point))
            {
                results.Add(proxy.Body);
            }
        }
    }

    public void QueryRay(Vector3 origin, Vector3 direction, float maxDistance, List<IRigidBody> results)
    {
        var found = new HashSet<IRigidBody>();
        var ray = new Ray(origin, direction);

        var step = _cellSize * 0.5f;
        var steps = (int)(maxDistance / step) + 1;

        var visitedCells = new HashSet<long>();

        for (var i = 0; i <= steps; i++)
        {
            var t = i * step;
            if (t > maxDistance)
            {
                break;
            }

            var point = origin + direction * t;
            var cellX = CellKey(point.X);
            var cellY = CellKey(point.Y);
            var cellZ = CellKey(point.Z);
            var cellKey = CombineCellKey(cellX, cellY, cellZ);

            if (!visitedCells.Add(cellKey))
            {
                continue;
            }

            if (!_cells.TryGetValue(cellKey, out var cellProxies))
            {
                continue;
            }

            foreach (var proxy in cellProxies)
            {
                if (found.Add(proxy.Body))
                {
                    var hit = ray.Intersects(proxy.Aabb);
                    if (hit.HasValue && hit.Value <= maxDistance)
                    {
                        results.Add(proxy.Body);
                    }
                }
            }
        }
    }

    #endregion

    #region 私有方法 - 单元格操作

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CellKey(float coordinate)
    {
        return (int)MathF.Floor(coordinate * _invCellSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long CombineCellKey(int x, int y, int z)
    {
        return ((long)(x + 32768) << 32) | ((uint)(y + 32768) << 16) | (uint)(z + 32768);
    }

    private void InsertProxyIntoCells(SpatialProxy proxy)
    {
        var aabb = proxy.Aabb;
        var minCellX = CellKey(aabb.Min.X);
        var minCellY = CellKey(aabb.Min.Y);
        var minCellZ = CellKey(aabb.Min.Z);
        var maxCellX = CellKey(aabb.Max.X);
        var maxCellY = CellKey(aabb.Max.Y);
        var maxCellZ = CellKey(aabb.Max.Z);

        for (var x = minCellX; x <= maxCellX; x++)
        {
            for (var y = minCellY; y <= maxCellY; y++)
            {
                for (var z = minCellZ; z <= maxCellZ; z++)
                {
                    var cellKey = CombineCellKey(x, y, z);

                    if (!_cells.TryGetValue(cellKey, out var cell))
                    {
                        cell = new List<SpatialProxy>(InitialCellCapacity);
                        _cells[cellKey] = cell;
                    }

                    cell.Add(proxy);
                }
            }
        }
    }

    private void RemoveProxyFromCells(SpatialProxy proxy)
    {
        var aabb = proxy.Aabb;
        var minCellX = CellKey(aabb.Min.X);
        var minCellY = CellKey(aabb.Min.Y);
        var minCellZ = CellKey(aabb.Min.Z);
        var maxCellX = CellKey(aabb.Max.X);
        var maxCellY = CellKey(aabb.Max.Y);
        var maxCellZ = CellKey(aabb.Max.Z);

        for (var x = minCellX; x <= maxCellX; x++)
        {
            for (var y = minCellY; y <= maxCellY; y++)
            {
                for (var z = minCellZ; z <= maxCellZ; z++)
                {
                    var cellKey = CombineCellKey(x, y, z);

                    if (_cells.TryGetValue(cellKey, out var cell))
                    {
                        cell.Remove(proxy);

                        if (cell.Count == 0)
                        {
                            _cells.Remove(cellKey);
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region 私有方法 - AABB 计算

    private static BoundingBox ComputeAabb(IRigidBody body)
    {
        var position = body.Position;
        var colliders = body.Colliders;

        if (colliders == null || colliders.Count == 0)
        {
            return new BoundingBox(position, position);
        }

        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        foreach (var collider in colliders)
        {
            var colliderAabb = ComputeColliderAabb(collider, position);
            min = Vector3.Min(min, colliderAabb.Min);
            max = Vector3.Max(max, colliderAabb.Max);
        }

        return new BoundingBox(min, max);
    }

    private static BoundingBox ComputeColliderAabb(ICollider collider, Vector3 bodyPosition)
    {
        var center = bodyPosition + collider.Center;

        return collider switch
        {
            ISphereCollider sphere => new BoundingBox(
                center - new Vector3(sphere.Radius),
                center + new Vector3(sphere.Radius)),
            IBoxCollider box => new BoundingBox(
                center - new Vector3(box.HalfExtentsX, box.HalfExtentsY, box.HalfExtentsZ),
                center + new Vector3(box.HalfExtentsX, box.HalfExtentsY, box.HalfExtentsZ)),
            ICapsuleCollider capsule => ComputeCapsuleAabb(capsule, center),
            _ => new BoundingBox(center - Vector3.One * 0.5f, center + Vector3.One * 0.5f)
        };
    }

    private static BoundingBox ComputeCapsuleAabb(ICapsuleCollider capsule, Vector3 center)
    {
        var radius = capsule.Radius;
        var halfHeight = capsule.Height * 0.5f;

        float offsetX, offsetY, offsetZ;

        switch (capsule.Direction)
        {
            case 0:
                offsetX = halfHeight;
                offsetY = radius;
                offsetZ = radius;
                break;
            case 2:
                offsetX = radius;
                offsetY = radius;
                offsetZ = halfHeight;
                break;
            default:
                offsetX = radius;
                offsetY = halfHeight;
                offsetZ = radius;
                break;
        }

        return new BoundingBox(
            center - new Vector3(offsetX, offsetY, offsetZ),
            center + new Vector3(offsetX, offsetY, offsetZ));
    }

    #endregion
}
