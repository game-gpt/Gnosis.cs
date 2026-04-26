using Gnosis.Core.Math;
using Gnosis.Navigation.NavMesh;
using Gnosis.Navigation.Path;

namespace Gnosis.Navigation.Dynamic;

/// <summary>
/// 动态导航网格更新器，将动态障碍物变化映射到 NavMesh 多边形阻塞标记和寻路器边代价更新。
/// 核心流程：障碍物脏检测 → 计算受影响多边形 → 标记阻塞/解除 → 更新 D* Lite 边代价
/// </summary>
public sealed class DynamicNavMeshUpdater : INavMeshRebuildHandler
{
    #region 字段

    private readonly DynamicObstacleManager _obstacleManager;
    private readonly List<NavMeshRebuildRegion> _pendingRegions = new();
    private readonly Dictionary<string, HashSet<int>> _obstacleBlockedPolygons = new();
    private INavMesh? _navMesh;
    private DStarLitePathfinder? _pathfinder;
    private bool _isRebuilding;

    #endregion

    #region 属性

    /// <summary>
    /// 关联的障碍物管理器
    /// </summary>
    public DynamicObstacleManager ObstacleManager => _obstacleManager;

    /// <summary>
    /// 是否正在重建
    /// </summary>
    public bool IsRebuilding => _isRebuilding;

    /// <summary>
    /// 待处理重建区域数量
    /// </summary>
    public int PendingRegionCount => _pendingRegions.Count;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建动态导航网格更新器
    /// </summary>
    /// <param name="obstacleManager">障碍物管理器</param>
    /// <param name="navMesh">导航网格</param>
    /// <param name="pathfinder">D* Lite 寻路器（可选）</param>
    public DynamicNavMeshUpdater(
        DynamicObstacleManager obstacleManager,
        INavMesh? navMesh = null,
        DStarLitePathfinder? pathfinder = null)
    {
        _obstacleManager = obstacleManager;
        _navMesh = navMesh;
        _pathfinder = pathfinder;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 设置关联的导航网格
    /// </summary>
    public void SetNavMesh(INavMesh navMesh)
    {
        _navMesh = navMesh;
    }

    /// <summary>
    /// 设置关联的 D* Lite 寻路器
    /// </summary>
    public void SetPathfinder(DStarLitePathfinder pathfinder)
    {
        _pathfinder = pathfinder;
    }

    /// <summary>
    /// 更新动态导航网格，检测障碍物变化并触发局部更新
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void Update(float delta)
    {
        if (_navMesh is null || !_navMesh.IsBuilt)
        {
            return;
        }

        if (!_obstacleManager.HasPendingUpdates)
        {
            return;
        }

        CollectDirtyObstacles();
        ProcessPendingRegions();
        _obstacleManager.Update(delta);
    }

    #endregion

    #region INavMeshRebuildHandler 实现

    /// <summary>
    /// 处理重建区域
    /// </summary>
    public void HandleRebuild(NavMeshRebuildRegion region)
    {
        HandleRebuildBatch(new List<NavMeshRebuildRegion> { region });
    }

    /// <summary>
    /// 批量处理重建区域
    /// </summary>
    public void HandleRebuildBatch(IReadOnlyList<NavMeshRebuildRegion> regions)
    {
        if (_navMesh is null)
        {
            return;
        }

        _isRebuilding = true;

        foreach (var region in regions)
        {
            ApplyRegionToNavMesh(region);
        }

        NotifyPathfinderChanges();

        _isRebuilding = false;
    }

    #endregion

    #region 私有方法 - 障碍物收集

    private void CollectDirtyObstacles()
    {
        foreach (var obstacle in _obstacleManager.Obstacles)
        {
            if (!obstacle.IsDirty)
            {
                continue;
            }

            var halfExtents = obstacle.HalfExtents;
            var affectedIds = _navMesh is not null
                ? _navMesh.FindPolygonsInArea(obstacle.Position, halfExtents)
                : new List<int>();

            var region = new NavMeshRebuildRegion
            {
                Center = obstacle.Position,
                HalfExtents = halfExtents,
                SourceName = obstacle.Name,
                IsAdded = obstacle.IsActive,
                AffectedPolygonIds = affectedIds
            };

            _pendingRegions.Add(region);
        }
    }

    #endregion

    #region 私有方法 - 区域处理

    private void ProcessPendingRegions()
    {
        if (_pendingRegions.Count == 0)
        {
            return;
        }

        HandleRebuildBatch(_pendingRegions);
        _pendingRegions.Clear();
    }

    private void ApplyRegionToNavMesh(NavMeshRebuildRegion region)
    {
        if (_navMesh is null)
        {
            return;
        }

        if (region.IsAdded)
        {
            BlockPolygonsForObstacle(region.SourceName, region.AffectedPolygonIds);
        }
        else
        {
            UnblockPolygonsForObstacle(region.SourceName);
        }
    }

    private void BlockPolygonsForObstacle(string obstacleName, List<int> polygonIds)
    {
        var previouslyBlocked = new HashSet<int>();

        if (_obstacleBlockedPolygons.TryGetValue(obstacleName, out var existing))
        {
            previouslyBlocked.UnionWith(existing);
        }

        var newBlocked = new HashSet<int>(polygonIds);

        if (!_obstacleBlockedPolygons.ContainsKey(obstacleName))
        {
            _obstacleBlockedPolygons[obstacleName] = newBlocked;
        }
        else
        {
            _obstacleBlockedPolygons[obstacleName] = newBlocked;
        }

        foreach (var polygonId in polygonIds)
        {
            _navMesh!.SetPolygonBlocked(polygonId, true);
        }

        foreach (var oldId in previouslyBlocked)
        {
            if (!newBlocked.Contains(oldId))
            {
                if (!IsBlockedByOtherObstacle(oldId, obstacleName))
                {
                    _navMesh!.SetPolygonBlocked(oldId, false);
                }
            }
        }
    }

    private void UnblockPolygonsForObstacle(string obstacleName)
    {
        if (!_obstacleBlockedPolygons.TryGetValue(obstacleName, out var blockedIds))
        {
            return;
        }

        foreach (var polygonId in blockedIds)
        {
            if (!IsBlockedByOtherObstacle(polygonId, obstacleName))
            {
                _navMesh!.SetPolygonBlocked(polygonId, false);
            }
        }

        _obstacleBlockedPolygons.Remove(obstacleName);
    }

    private bool IsBlockedByOtherObstacle(int polygonId, string excludeObstacle)
    {
        foreach (var kvp in _obstacleBlockedPolygons)
        {
            if (kvp.Key == excludeObstacle)
            {
                continue;
            }

            if (kvp.Value.Contains(polygonId))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region 私有方法 - 寻路器通知

    private void NotifyPathfinderChanges()
    {
        if (_pathfinder is null || !_pathfinder.IsInitialized || _navMesh is null)
        {
            return;
        }

        foreach (var polygonId in _navMesh.BlockedPolygonIds)
        {
            var polygon = _navMesh.GetPolygonById(polygonId);
            if (polygon is null)
            {
                continue;
            }

            foreach (var neighborId in polygon.Value.Neighbors)
            {
                _pathfinder.UpdateEdgeCost(polygonId, neighborId, float.PositiveInfinity);
                _pathfinder.UpdateEdgeCost(neighborId, polygonId, float.PositiveInfinity);
            }
        }

        foreach (var polygonId in _navMesh.BlockedPolygonIds)
        {
            var polygon = _navMesh.GetPolygonById(polygonId);
            if (polygon is null)
            {
                continue;
            }

            foreach (var neighborId in polygon.Value.Neighbors)
            {
                if (!_navMesh.IsPolygonBlocked(neighborId))
                {
                    _pathfinder.UpdateEdgeCost(neighborId, polygonId, float.PositiveInfinity);
                }
            }
        }

        _pathfinder.ApplyChanges();
    }

    #endregion
}
