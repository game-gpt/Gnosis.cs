using Gnosis.Core.Math;

namespace Gnosis.Navigation.Link;

/// <summary>
/// 离网连接实现，支持跳点、传送门等非连续移动
/// </summary>
public sealed class NavMeshLink : INavMeshLink
{
    #region 字段

    private bool _isActive = true;

    #endregion

    #region INavMeshLink 实现

    /// <summary>
    /// 连接名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 连接类型
    /// </summary>
    public NavLinkType LinkType { get; }

    /// <summary>
    /// 起始点
    /// </summary>
    public Vector3 StartPoint { get; }

    /// <summary>
    /// 终止点
    /// </summary>
    public Vector3 EndPoint { get; }

    /// <summary>
    /// 是否双向
    /// </summary>
    public bool IsBidirectional { get; }

    /// <summary>
    /// 通行代价
    /// </summary>
    public float Cost { get; }

    /// <summary>
    /// 是否可用
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => _isActive = value;
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建离网连接
    /// </summary>
    /// <param name="name">连接名称</param>
    /// <param name="linkType">连接类型</param>
    /// <param name="startPoint">起始点</param>
    /// <param name="endPoint">终止点</param>
    /// <param name="isBidirectional">是否双向</param>
    /// <param name="cost">通行代价（-1 表示自动计算）</param>
    public NavMeshLink(
        string name,
        NavLinkType linkType,
        Vector3 startPoint,
        Vector3 endPoint,
        bool isBidirectional = true,
        float cost = -1.0f)
    {
        Name = name;
        LinkType = linkType;
        StartPoint = startPoint;
        EndPoint = endPoint;
        IsBidirectional = isBidirectional;
        Cost = cost >= 0 ? cost : CalculateDefaultCost(linkType, startPoint, endPoint);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 检查指定位置是否在连接的起始端附近
    /// </summary>
    /// <param name="position">检测位置</param>
    /// <param name="tolerance">容差距离</param>
    /// <returns>是否在起始端附近</returns>
    public bool IsNearStart(Vector3 position, float tolerance)
    {
        var distance = Vector3.Distance(position, StartPoint);
        return distance <= tolerance;
    }

    /// <summary>
    /// 检查指定位置是否在连接的终止端附近
    /// </summary>
    /// <param name="position">检测位置</param>
    /// <param name="tolerance">容差距离</param>
    /// <returns>是否在终止端附近</returns>
    public bool IsNearEnd(Vector3 position, float tolerance)
    {
        var distance = Vector3.Distance(position, EndPoint);
        return distance <= tolerance;
    }

    /// <summary>
    /// 获取从指定位置出发的到达点
    /// </summary>
    /// <param name="fromPosition">出发位置</param>
    /// <param name="tolerance">容差距离</param>
    /// <param name="arrivalPoint">到达点</param>
    /// <returns>是否成功匹配</returns>
    public bool TryGetArrivalPoint(Vector3 fromPosition, float tolerance, out Vector3 arrivalPoint)
    {
        if (IsNearStart(fromPosition, tolerance))
        {
            arrivalPoint = EndPoint;
            return true;
        }

        if (IsBidirectional && IsNearEnd(fromPosition, tolerance))
        {
            arrivalPoint = StartPoint;
            return true;
        }

        arrivalPoint = Vector3.Zero;
        return false;
    }

    #endregion

    #region 私有方法

    private static float CalculateDefaultCost(NavLinkType linkType, Vector3 start, Vector3 end)
    {
        var distance = Vector3.Distance(start, end);

        return linkType switch
        {
            NavLinkType.Jump => distance * 2.0f,
            NavLinkType.Drop => distance * 0.5f,
            NavLinkType.Teleport => 0.1f,
            NavLinkType.Ladder => distance * 3.0f,
            NavLinkType.Custom => distance,
            _ => distance
        };
    }

    #endregion
}
