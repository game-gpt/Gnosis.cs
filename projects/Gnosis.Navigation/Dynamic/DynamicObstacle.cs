using Gnosis.Core.Math;

namespace Gnosis.Navigation.Dynamic;

/// <summary>
/// 动态障碍物实现，支持运行时导航网格更新
/// </summary>
public sealed class DynamicObstacle : IDynamicObstacle
{
    #region 字段

    private Vector3 _position;
    private Vector3 _bounds;
    private float _radius;
    private float _height;
    private bool _isActive = true;
    private bool _isDirty;

    #endregion

    #region IDynamicObstacle 实现

    /// <summary>
    /// 障碍物名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 障碍物形状
    /// </summary>
    public ObstacleShape Shape { get; }

    /// <summary>
    /// 障碍物位置
    /// </summary>
    public Vector3 Position => _position;

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                _isDirty = true;
            }
        }
    }

    #endregion

    #region 属性

    /// <summary>
    /// 障碍物是否需要更新导航网格
    /// </summary>
    public bool IsDirty => _isDirty;

    /// <summary>
    /// 盒形障碍物的半尺寸
    /// </summary>
    public Vector3 HalfExtents => _bounds * 0.5f;

    /// <summary>
    /// 圆柱/胶囊形障碍物的半径
    /// </summary>
    public float Radius => _radius;

    /// <summary>
    /// 圆柱/胶囊形障碍物的高度
    /// </summary>
    public float Height => _height;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建盒形动态障碍物
    /// </summary>
    public DynamicObstacle(string name, Vector3 position, Vector3 bounds)
    {
        Name = name;
        Shape = ObstacleShape.Box;
        _position = position;
        _bounds = bounds;
        _radius = Math.Max(bounds.X, bounds.Z) * 0.5f;
        _height = bounds.Y;
        _isDirty = true;
    }

    /// <summary>
    /// 创建圆柱/胶囊形动态障碍物
    /// </summary>
    public DynamicObstacle(string name, ObstacleShape shape, Vector3 position, float radius, float height)
    {
        Name = name;
        Shape = shape;
        _position = position;
        _radius = radius;
        _height = height;
        _bounds = new Vector3(radius * 2, height, radius * 2);
        _isDirty = true;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 更新障碍物位置
    /// </summary>
    /// <param name="position">新位置</param>
    public void UpdatePosition(Vector3 position)
    {
        if (_position != position)
        {
            _position = position;
            _isDirty = true;
        }
    }

    /// <summary>
    /// 获取障碍物占据的导航区域
    /// </summary>
    public Vector3 GetBounds()
    {
        return _bounds;
    }

    /// <summary>
    /// 更新盒形障碍物的尺寸
    /// </summary>
    /// <param name="bounds">新尺寸</param>
    public void UpdateBounds(Vector3 bounds)
    {
        if (_bounds != bounds)
        {
            _bounds = bounds;
            _radius = Math.Max(bounds.X, bounds.Z) * 0.5f;
            _height = bounds.Y;
            _isDirty = true;
        }
    }

    /// <summary>
    /// 更新圆柱/胶囊形障碍物的尺寸
    /// </summary>
    /// <param name="radius">新半径</param>
    /// <param name="height">新高度</param>
    public void UpdateDimensions(float radius, float height)
    {
        if (Math.Abs(_radius - radius) > 0.001f || Math.Abs(_height - height) > 0.001f)
        {
            _radius = radius;
            _height = height;
            _bounds = new Vector3(radius * 2, height, radius * 2);
            _isDirty = true;
        }
    }

    /// <summary>
    /// 检测点是否在障碍物内部
    /// </summary>
    /// <param name="point">检测点</param>
    /// <returns>是否在障碍物内部</returns>
    public bool ContainsPoint(Vector3 point)
    {
        if (!_isActive)
        {
            return false;
        }

        return Shape switch
        {
            ObstacleShape.Box => ContainsPointBox(point),
            ObstacleShape.Cylinder => ContainsPointCylinder(point),
            ObstacleShape.Capsule => ContainsPointCylinder(point),
            ObstacleShape.Convex => ContainsPointBox(point),
            _ => false
        };
    }

    /// <summary>
    /// 标记障碍物已更新完毕
    /// </summary>
    public void ClearDirty()
    {
        _isDirty = false;
    }

    #endregion

    #region 私有方法

    private bool ContainsPointBox(Vector3 point)
    {
        var min = _position - HalfExtents;
        var max = _position + HalfExtents;

        return point.X >= min.X && point.X <= max.X &&
               point.Y >= min.Y && point.Y <= max.Y &&
               point.Z >= min.Z && point.Z <= max.Z;
    }

    private bool ContainsPointCylinder(Vector3 point)
    {
        var dx = point.X - _position.X;
        var dz = point.Z - _position.Z;
        var distSq = dx * dx + dz * dz;

        if (distSq > _radius * _radius)
        {
            return false;
        }

        var minY = _position.Y - _height * 0.5f;
        var maxY = _position.Y + _height * 0.5f;

        return point.Y >= minY && point.Y <= maxY;
    }

    #endregion
}
