using Gnosis.XR.Session;
using Gnosis.XR.Tracking;

namespace Gnosis.XR.Anchor;

/// <summary>
/// 空间锚点管理器接口，管理锚点的创建、销毁、持久化与查询
/// </summary>
public interface IXrAnchorManager : IDisposable
{
    /// <summary>
    /// 关联的 XR 会话
    /// </summary>
    IXrSession Session { get; }

    /// <summary>
    /// 是否支持空间锚点
    /// </summary>
    bool SupportsAnchors { get; }

    /// <summary>
    /// 是否支持锚点持久化
    /// </summary>
    bool SupportsPersistence { get; }

    /// <summary>
    /// 当前活跃的锚点列表
    /// </summary>
    IReadOnlyList<SpatialAnchor> Anchors { get; }

    /// <summary>
    /// 锚点状态变更事件
    /// </summary>
    event EventHandler<XrAnchorStateChangedEventArgs>? AnchorStateChanged;

    /// <summary>
    /// 创建空间锚点
    /// </summary>
    /// <param name="createInfo">锚点创建信息</param>
    /// <returns>新创建的空间锚点</returns>
    SpatialAnchor CreateAnchor(XrAnchorCreateInfo createInfo);

    /// <summary>
    /// 销毁空间锚点
    /// </summary>
    /// <param name="anchorId">锚点标识</param>
    void DestroyAnchor(Guid anchorId);

    /// <summary>
    /// 获取指定标识的锚点
    /// </summary>
    /// <param name="anchorId">锚点标识</param>
    /// <returns>空间锚点，未找到返回 null</returns>
    SpatialAnchor? GetAnchor(Guid anchorId);

    /// <summary>
    /// 持久化锚点（保存到 XR 运行时存储）
    /// </summary>
    /// <param name="anchorId">锚点标识</param>
    /// <returns>持久化存储标识</returns>
    string PersistAnchor(Guid anchorId);

    /// <summary>
    /// 从持久化存储加载锚点
    /// </summary>
    /// <param name="persistenceUuid">持久化存储标识</param>
    /// <returns>加载的空间锚点</returns>
    SpatialAnchor LoadPersistedAnchor(string persistenceUuid);

    /// <summary>
    /// 删除持久化锚点
    /// </summary>
    /// <param name="persistenceUuid">持久化存储标识</param>
    void ErasePersistedAnchor(string persistenceUuid);

    /// <summary>
    /// 获取所有持久化锚点标识列表
    /// </summary>
    /// <returns>持久化存储标识列表</returns>
    IReadOnlyList<string> GetPersistedAnchorUuids();

    /// <summary>
    /// 更新所有锚点追踪状态（每帧调用）
    /// </summary>
    void Update();
}
