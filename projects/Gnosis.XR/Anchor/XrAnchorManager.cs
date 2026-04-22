using Gnosis.XR.Session;
using Gnosis.XR.Tracking;

namespace Gnosis.XR.Anchor;

/// <summary>
/// 空间锚点管理器实现，管理锚点的创建、销毁、持久化与查询
/// </summary>
public sealed class XrAnchorManager : IXrAnchorManager
{
    #region 字段

    private readonly Dictionary<Guid, SpatialAnchor> _anchors;
    private readonly Dictionary<string, SpatialAnchor> _persistedAnchors;
    private bool _isDisposed;

    #endregion

    #region 属性

    /// <summary>
    /// 关联的 XR 会话
    /// </summary>
    public IXrSession Session { get; }

    /// <summary>
    /// 是否支持空间锚点
    /// </summary>
    public bool SupportsAnchors { get; }

    /// <summary>
    /// 是否支持锚点持久化
    /// </summary>
    public bool SupportsPersistence { get; }

    /// <summary>
    /// 当前活跃的锚点列表
    /// </summary>
    public IReadOnlyList<SpatialAnchor> Anchors => _anchors.Values.ToList().AsReadOnly();

    #endregion

    #region 事件

    /// <summary>
    /// 锚点状态变更事件
    /// </summary>
    public event EventHandler<XrAnchorStateChangedEventArgs>? AnchorStateChanged;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用 XR 会话初始化锚点管理器
    /// </summary>
    /// <param name="session">XR 会话</param>
    public XrAnchorManager(IXrSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
        SupportsAnchors = true;
        SupportsPersistence = true;
        _anchors = new Dictionary<Guid, SpatialAnchor>();
        _persistedAnchors = new Dictionary<string, SpatialAnchor>();
        _isDisposed = false;
    }

    #endregion

    #region IXrAnchorManager 实现

    /// <summary>
    /// 创建空间锚点
    /// </summary>
    /// <param name="createInfo">锚点创建信息</param>
    /// <returns>新创建的空间锚点</returns>
    public SpatialAnchor CreateAnchor(XrAnchorCreateInfo createInfo)
    {
        ArgumentNullException.ThrowIfNull(createInfo);
        ThrowIfDisposed();

        if (!SupportsAnchors)
        {
            throw new InvalidOperationException("当前 XR 运行时不支持空间锚点");
        }

        var anchor = new SpatialAnchor(createInfo);
        anchor.StateChanged += OnAnchorStateChanged;
        _anchors[anchor.Id] = anchor;

        return anchor;
    }

    /// <summary>
    /// 销毁空间锚点
    /// </summary>
    /// <param name="anchorId">锚点标识</param>
    public void DestroyAnchor(Guid anchorId)
    {
        ThrowIfDisposed();

        if (_anchors.TryGetValue(anchorId, out var anchor))
        {
            anchor.StateChanged -= OnAnchorStateChanged;
            anchor.Dispose();
            _anchors.Remove(anchorId);
        }
    }

    /// <summary>
    /// 获取指定标识的锚点
    /// </summary>
    /// <param name="anchorId">锚点标识</param>
    /// <returns>空间锚点</returns>
    public SpatialAnchor? GetAnchor(Guid anchorId)
    {
        return _anchors.GetValueOrDefault(anchorId);
    }

    /// <summary>
    /// 持久化锚点
    /// </summary>
    /// <param name="anchorId">锚点标识</param>
    /// <returns>持久化存储标识</returns>
    public string PersistAnchor(Guid anchorId)
    {
        ThrowIfDisposed();

        if (!SupportsPersistence)
        {
            throw new InvalidOperationException("当前 XR 运行时不支持锚点持久化");
        }

        if (!_anchors.TryGetValue(anchorId, out var anchor))
        {
            throw new InvalidOperationException($"未找到标识为 {anchorId} 的锚点");
        }

        var persistenceUuid = anchor.RequestPersistence();
        _persistedAnchors[persistenceUuid] = anchor;
        return persistenceUuid;
    }

    /// <summary>
    /// 从持久化存储加载锚点
    /// </summary>
    /// <param name="persistenceUuid">持久化存储标识</param>
    /// <returns>加载的空间锚点</returns>
    public SpatialAnchor LoadPersistedAnchor(string persistenceUuid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(persistenceUuid);
        ThrowIfDisposed();

        if (!SupportsPersistence)
        {
            throw new InvalidOperationException("当前 XR 运行时不支持锚点持久化");
        }

        if (_persistedAnchors.TryGetValue(persistenceUuid, out var existingAnchor))
        {
            return existingAnchor;
        }

        var anchor = new SpatialAnchor(new XrAnchorCreateInfo
        {
            Pose = XrPose.Identity,
            RequestPersistence = true,
            Name = $"persisted_{persistenceUuid}"
        });

        anchor.StateChanged += OnAnchorStateChanged;
        _anchors[anchor.Id] = anchor;
        _persistedAnchors[persistenceUuid] = anchor;

        return anchor;
    }

    /// <summary>
    /// 删除持久化锚点
    /// </summary>
    /// <param name="persistenceUuid">持久化存储标识</param>
    public void ErasePersistedAnchor(string persistenceUuid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(persistenceUuid);

        _persistedAnchors.Remove(persistenceUuid);
    }

    /// <summary>
    /// 获取所有持久化锚点标识列表
    /// </summary>
    /// <returns>持久化存储标识列表</returns>
    public IReadOnlyList<string> GetPersistedAnchorUuids()
    {
        return _persistedAnchors.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// 更新所有锚点追踪状态
    /// </summary>
    public void Update()
    {
        ThrowIfDisposed();

        if (!Session.IsRunning)
        {
            return;
        }

        foreach (var anchor in _anchors.Values)
        {
            anchor.Update();
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 锚点状态变更回调
    /// </summary>
    private void OnAnchorStateChanged(object? sender, XrAnchorStateChangedEventArgs e)
    {
        AnchorStateChanged?.Invoke(this, e);
    }

    /// <summary>
    /// 若已释放则抛出异常
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(XrAnchorManager));
        }
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放锚点管理器资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        foreach (var anchor in _anchors.Values)
        {
            anchor.StateChanged -= OnAnchorStateChanged;
            anchor.Dispose();
        }

        _anchors.Clear();
        _persistedAnchors.Clear();
        _isDisposed = true;
    }

    #endregion
}
