using Gnosis.Core.Math;

namespace Gnosis.Navigation.Link;

/// <summary>
/// 离网连接接口，支持跳点、传送门等非连续移动
/// </summary>
public interface INavMeshLink
{
    /// <summary>
    /// 连接名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 连接类型
    /// </summary>
    NavLinkType LinkType { get; }

    /// <summary>
    /// 起始点
    /// </summary>
    Vector3 StartPoint { get; }

    /// <summary>
    /// 终止点
    /// </summary>
    Vector3 EndPoint { get; }

    /// <summary>
    /// 是否双向
    /// </summary>
    bool IsBidirectional { get; }

    /// <summary>
    /// 通行代价
    /// </summary>
    float Cost { get; }

    /// <summary>
    /// 是否可用
    /// </summary>
    bool IsActive { get; set; }
}
