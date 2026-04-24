using Gnosis.Core.Math;

namespace Gnosis.Navigation.Link;

/// <summary>
/// 离网连接管理器，管理导航网格中所有离网连接
/// </summary>
public sealed class NavMeshLinkManager
{
    #region 字段

    private readonly List<NavMeshLink> _links = new();
    private readonly Dictionary<int, List<NavMeshLink>> _tileLinks = new();

    #endregion

    #region 属性

    /// <summary>
    /// 所有离网连接
    /// </summary>
    public IReadOnlyList<NavMeshLink> Links => _links;

    /// <summary>
    /// 连接数量
    /// </summary>
    public int Count => _links.Count;

    #endregion

    #region 公开方法

    /// <summary>
    /// 添加离网连接
    /// </summary>
    /// <param name="link">离网连接</param>
    public void AddLink(NavMeshLink link)
    {
        _links.Add(link);
    }

    /// <summary>
    /// 移除离网连接
    /// </summary>
    /// <param name="link">离网连接</param>
    public void RemoveLink(NavMeshLink link)
    {
        _links.Remove(link);
    }

    /// <summary>
    /// 根据名称查找离网连接
    /// </summary>
    /// <param name="name">连接名称</param>
    /// <returns>匹配的连接列表</returns>
    public IReadOnlyList<NavMeshLink> FindLinksByName(string name)
    {
        var results = new List<NavMeshLink>();

        foreach (var link in _links)
        {
            if (link.Name == name)
            {
                results.Add(link);
            }
        }

        return results;
    }

    /// <summary>
    /// 查找指定位置附近的所有可用离网连接
    /// </summary>
    /// <param name="position">检测位置</param>
    /// <param name="radius">搜索半径</param>
    /// <returns>附近的连接列表</returns>
    public IReadOnlyList<NavMeshLink> FindNearbyLinks(Vector3 position, float radius)
    {
        var results = new List<NavMeshLink>();
        var radiusSq = radius * radius;

        foreach (var link in _links)
        {
            if (!link.IsActive)
            {
                continue;
            }

            var startDistSq = Vector3.DistanceSquared(position, link.StartPoint);
            var endDistSq = Vector3.DistanceSquared(position, link.EndPoint);

            if (startDistSq <= radiusSq || endDistSq <= radiusSq)
            {
                results.Add(link);
            }
        }

        return results;
    }

    /// <summary>
    /// 查找从指定位置出发可用的到达点
    /// </summary>
    /// <param name="fromPosition">出发位置</param>
    /// <param name="tolerance">容差距离</param>
    /// <returns>可到达的位置列表</returns>
    public IReadOnlyList<Vector3> FindArrivalPoints(Vector3 fromPosition, float tolerance)
    {
        var results = new List<Vector3>();

        foreach (var link in _links)
        {
            if (!link.IsActive)
            {
                continue;
            }

            if (link.TryGetArrivalPoint(fromPosition, tolerance, out var arrivalPoint))
            {
                results.Add(arrivalPoint);
            }
        }

        return results;
    }

    /// <summary>
    /// 根据类型筛选离网连接
    /// </summary>
    /// <param name="linkType">连接类型</param>
    /// <returns>匹配的连接列表</returns>
    public IReadOnlyList<NavMeshLink> FindLinksByType(NavLinkType linkType)
    {
        var results = new List<NavMeshLink>();

        foreach (var link in _links)
        {
            if (link.LinkType == linkType)
            {
                results.Add(link);
            }
        }

        return results;
    }

    /// <summary>
    /// 清空所有离网连接
    /// </summary>
    public void Clear()
    {
        _links.Clear();
        _tileLinks.Clear();
    }

    #endregion
}
