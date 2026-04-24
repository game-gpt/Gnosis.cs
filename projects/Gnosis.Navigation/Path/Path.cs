using Gnosis.Core.Math;

namespace Gnosis.Navigation.Path;

/// <summary>
/// 路径实现类，表示导航系统计算出的路径
/// </summary>
public sealed class Path : IPath
{
    #region 字段

    private readonly List<Vector3> _waypoints;
    private int _currentWaypointIndex;

    #endregion

    #region IPath 实现

    /// <summary>
    /// 路径是否完整（已到达终点）
    /// </summary>
    public bool IsComplete => _currentWaypointIndex >= _waypoints.Count - 1;

    /// <summary>
    /// 路径总长度
    /// </summary>
    public float Length { get; }

    /// <summary>
    /// 路径点列表
    /// </summary>
    public IReadOnlyList<Vector3> Waypoints => _waypoints;

    /// <summary>
    /// 当前路径点索引
    /// </summary>
    public int CurrentWaypointIndex => _currentWaypointIndex;

    /// <summary>
    /// 当前路径点
    /// </summary>
    public Vector3 CurrentWaypoint =>
        _currentWaypointIndex < _waypoints.Count
            ? _waypoints[_currentWaypointIndex]
            : _waypoints[^1];

    /// <summary>
    /// 下一个路径点
    /// </summary>
    public Vector3 NextWaypoint =>
        _currentWaypointIndex + 1 < _waypoints.Count
            ? _waypoints[_currentWaypointIndex + 1]
            : _waypoints[^1];

    #endregion

    #region 构造函数

    public Path(List<Vector3> waypoints)
    {
        _waypoints = waypoints;
        _currentWaypointIndex = 0;
        Length = CalculateLength();
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 前进到下一个路径点
    /// </summary>
    public void Advance()
    {
        if (_currentWaypointIndex < _waypoints.Count - 1)
        {
            _currentWaypointIndex++;
        }
    }

    /// <summary>
    /// 是否已到达路径终点
    /// </summary>
    public bool IsAtPathEnd()
    {
        return _currentWaypointIndex >= _waypoints.Count - 1;
    }

    #endregion

    #region 私有方法

    private float CalculateLength()
    {
        var length = 0.0f;

        for (var i = 1; i < _waypoints.Count; i++)
        {
            length += Vector3.Distance(_waypoints[i - 1], _waypoints[i]);
        }

        return length;
    }

    #endregion
}
