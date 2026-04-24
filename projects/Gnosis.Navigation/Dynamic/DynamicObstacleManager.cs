using Gnosis.Core.Math;
using Gnosis.Navigation.NavMesh;

namespace Gnosis.Navigation.Dynamic;

/// <summary>
/// 动态障碍物管理器，负责追踪障碍物变化并触发导航网格局部更新
/// </summary>
public sealed class DynamicObstacleManager
{
    #region 字段

    private readonly List<DynamicObstacle> _obstacles = new();
    private INavMesh? _navMesh;
    private float _updateInterval;
    private float _elapsedSinceUpdate;

    #endregion

    #region 属性

    /// <summary>
    /// 所有动态障碍物
    /// </summary>
    public IReadOnlyList<DynamicObstacle> Obstacles => _obstacles;

    /// <summary>
    /// 障碍物数量
    /// </summary>
    public int Count => _obstacles.Count;

    /// <summary>
    /// 更新间隔（秒），0 表示每帧更新
    /// </summary>
    public float UpdateInterval
    {
        get => _updateInterval;
        set => _updateInterval = Math.Max(0, value);
    }

    /// <summary>
    /// 是否有待处理的障碍物变更
    /// </summary>
    public bool HasPendingUpdates
    {
        get
        {
            foreach (var obstacle in _obstacles)
            {
                if (obstacle.IsDirty)
                {
                    return true;
                }
            }

            return false;
        }
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建动态障碍物管理器
    /// </summary>
    /// <param name="navMesh">关联的导航网格</param>
    /// <param name="updateInterval">更新间隔（秒）</param>
    public DynamicObstacleManager(INavMesh? navMesh = null, float updateInterval = 0.5f)
    {
        _navMesh = navMesh;
        _updateInterval = updateInterval;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 设置关联的导航网格
    /// </summary>
    /// <param name="navMesh">导航网格</param>
    public void SetNavMesh(INavMesh navMesh)
    {
        _navMesh = navMesh;
    }

    /// <summary>
    /// 添加动态障碍物
    /// </summary>
    /// <param name="obstacle">动态障碍物</param>
    public void AddObstacle(DynamicObstacle obstacle)
    {
        _obstacles.Add(obstacle);
    }

    /// <summary>
    /// 移除动态障碍物
    /// </summary>
    /// <param name="obstacle">动态障碍物</param>
    public void RemoveObstacle(DynamicObstacle obstacle)
    {
        _obstacles.Remove(obstacle);
        obstacle.IsActive = false;
    }

    /// <summary>
    /// 更新管理器，检测障碍物变化并触发导航网格更新
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void Update(float delta)
    {
        _elapsedSinceUpdate += delta;

        if (_updateInterval > 0 && _elapsedSinceUpdate < _updateInterval)
        {
            return;
        }

        if (!HasPendingUpdates)
        {
            return;
        }

        ApplyObstacleChanges();
        _elapsedSinceUpdate = 0;
    }

    /// <summary>
    /// 检测指定位置是否被任何激活的障碍物阻挡
    /// </summary>
    /// <param name="point">检测点</param>
    /// <returns>是否被阻挡</returns>
    public bool IsBlocked(Vector3 point)
    {
        foreach (var obstacle in _obstacles)
        {
            if (obstacle.IsActive && obstacle.ContainsPoint(point))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取影响指定区域的所有障碍物
    /// </summary>
    /// <param name="center">区域中心</param>
    /// <param name="radius">区域半径</param>
    /// <returns>影响该区域的障碍物列表</returns>
    public IReadOnlyList<DynamicObstacle> GetAffectingObstacles(Vector3 center, float radius)
    {
        var results = new List<DynamicObstacle>();
        var radiusSq = radius * radius;

        foreach (var obstacle in _obstacles)
        {
            if (!obstacle.IsActive)
            {
                continue;
            }

            var distSq = Vector3.DistanceSquared(center, obstacle.Position);
            var obstacleRadius = Math.Max(obstacle.GetBounds().X, obstacle.GetBounds().Z) * 0.5f;
            var maxDist = radius + obstacleRadius;

            if (distSq <= maxDist * maxDist)
            {
                results.Add(obstacle);
            }
        }

        return results;
    }

    /// <summary>
    /// 清空所有障碍物
    /// </summary>
    public void Clear()
    {
        _obstacles.Clear();
    }

    #endregion

    #region 私有方法

    private void ApplyObstacleChanges()
    {
        foreach (var obstacle in _obstacles)
        {
            if (obstacle.IsDirty)
            {
                obstacle.ClearDirty();
            }
        }
    }

    #endregion
}
