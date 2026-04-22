using Gnosis.XR.Tracking;

namespace Gnosis.XR.Anchor;

/// <summary>
/// 空间锚点，在物理空间中创建一个持久化的定位参考点
/// </summary>
public sealed class SpatialAnchor : IDisposable
{
    #region 字段

    private XrAnchorState _state;
    private XrPose _pose;
    private bool _isDisposed;

    #endregion

    #region 属性

    /// <summary>
    /// 锚点唯一标识
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 锚点名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 当前锚点状态
    /// </summary>
    public XrAnchorState State => _state;

    /// <summary>
    /// 当前锚点姿态
    /// </summary>
    public XrPose Pose => _pose;

    /// <summary>
    /// 锚点标志
    /// </summary>
    public XrAnchorFlags Flags { get; private set; }

    /// <summary>
    /// 是否支持持久化
    /// </summary>
    public bool IsPersistable => (Flags & XrAnchorFlags.Persistable) != 0;

    /// <summary>
    /// 是否正在追踪
    /// </summary>
    public bool IsTracking => _state == XrAnchorState.Tracking;

    /// <summary>
    /// 创建时间（UTC）
    /// </summary>
    public DateTimeOffset CreatedTime { get; }

    /// <summary>
    /// 上次更新时间（UTC）
    /// </summary>
    public DateTimeOffset LastUpdatedTime { get; private set; }

    /// <summary>
    /// 持久化存储标识（持久化后由 XR 运行时分配）
    /// </summary>
    public string? PersistenceUuid { get; private set; }

    #endregion

    #region 事件

    /// <summary>
    /// 锚点状态变更事件
    /// </summary>
    public event EventHandler<XrAnchorStateChangedEventArgs>? StateChanged;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用创建信息初始化空间锚点
    /// </summary>
    /// <param name="createInfo">创建信息</param>
    public SpatialAnchor(XrAnchorCreateInfo createInfo)
    {
        ArgumentNullException.ThrowIfNull(createInfo);

        Id = Guid.NewGuid();
        Name = createInfo.Name;
        _pose = createInfo.Pose;
        _state = XrAnchorState.Pending;
        Flags = XrAnchorFlags.PositionValid | XrAnchorFlags.RotationValid;
        if (createInfo.RequestPersistence)
        {
            Flags |= XrAnchorFlags.Persistable;
        }

        CreatedTime = DateTimeOffset.UtcNow;
        LastUpdatedTime = CreatedTime;
        _isDisposed = false;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 更新锚点追踪数据（每帧调用）
    /// </summary>
    public void Update()
    {
        ThrowIfDisposed();

        if (_state == XrAnchorState.Destroyed)
        {
            return;
        }

        LastUpdatedTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 请求持久化锚点
    /// </summary>
    /// <returns>持久化存储标识</returns>
    /// <exception cref="InvalidOperationException">锚点不支持持久化或状态不允许</exception>
    public string RequestPersistence()
    {
        ThrowIfDisposed();

        if (!IsPersistable)
        {
            throw new InvalidOperationException("锚点不支持持久化");
        }

        if (_state != XrAnchorState.Tracking)
        {
            throw new InvalidOperationException("仅追踪中的锚点可以持久化");
        }

        PersistenceUuid = Id.ToString("N");
        return PersistenceUuid;
    }

    /// <summary>
    /// 请求销毁锚点
    /// </summary>
    public void RequestDestroy()
    {
        ThrowIfDisposed();

        SetState(XrAnchorState.Destroyed);
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 设置锚点姿态（供 XR 运行时后端注入追踪数据）
    /// </summary>
    /// <param name="pose">新的锚点姿态</param>
    internal void SetPose(XrPose pose)
    {
        _pose = pose;
        LastUpdatedTime = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// 设置锚点状态
    /// </summary>
    /// <param name="newState">新的锚点状态</param>
    internal void SetState(XrAnchorState newState)
    {
        var oldState = _state;
        _state = newState;

        if (oldState != newState)
        {
            StateChanged?.Invoke(this, new XrAnchorStateChangedEventArgs(Id, newState, oldState));
        }
    }

    /// <summary>
    /// 设置锚点标志
    /// </summary>
    /// <param name="flags">新的锚点标志</param>
    internal void SetFlags(XrAnchorFlags flags)
    {
        Flags = flags;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 若已释放则抛出异常
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(SpatialAnchor));
        }
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放锚点资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_state != XrAnchorState.Destroyed)
        {
            _state = XrAnchorState.Destroyed;
        }

        _isDisposed = true;
    }

    #endregion
}

/// <summary>
/// 锚点状态变更事件参数
/// </summary>
public class XrAnchorStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// 锚点标识
    /// </summary>
    public Guid AnchorId { get; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public XrAnchorState CurrentState { get; }

    /// <summary>
    /// 前一状态
    /// </summary>
    public XrAnchorState PreviousState { get; }

    /// <summary>
    /// 初始化锚点状态变更事件参数
    /// </summary>
    public XrAnchorStateChangedEventArgs(Guid anchorId, XrAnchorState currentState, XrAnchorState previousState)
    {
        AnchorId = anchorId;
        CurrentState = currentState;
        PreviousState = previousState;
    }
}
