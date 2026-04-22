namespace Gnosis.XR.Tracking;

/// <summary>
/// XR 追踪器实现，提供设备空间追踪能力
/// </summary>
public sealed class XrTracker : IXrTracker
{
    #region 字段

    private XrTrackingSnapshot _currentSnapshot;
    private bool _isTracking;
    private bool _isConnected;

    #endregion

    #region 属性

    /// <summary>
    /// 追踪器类型
    /// </summary>
    public XrTrackerType TrackerType { get; }

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// 是否正在追踪
    /// </summary>
    public bool IsTracking => _isTracking;

    /// <summary>
    /// 当前追踪快照
    /// </summary>
    public XrTrackingSnapshot CurrentSnapshot => _currentSnapshot;

    #endregion

    #region 事件

    /// <summary>
    /// 追踪状态变更事件
    /// </summary>
    public event EventHandler<XrTrackerEventArgs>? TrackingChanged;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用追踪器类型初始化 XR 追踪器
    /// </summary>
    /// <param name="trackerType">追踪器类型</param>
    public XrTracker(XrTrackerType trackerType)
    {
        TrackerType = trackerType;
        _isConnected = false;
        _isTracking = false;
        _currentSnapshot = new XrTrackingSnapshot
        {
            Pose = XrPose.Identity,
            Flags = XrTrackingFlags.None
        };
    }

    #endregion

    #region IXrTracker 实现

    /// <summary>
    /// 更新追踪数据（每帧调用）
    /// </summary>
    public void Update()
    {
        if (!_isConnected)
        {
            return;
        }

        var wasTracking = _isTracking;
        _isTracking = _currentSnapshot.IsTrackingOk;

        if (wasTracking != _isTracking)
        {
            TrackingChanged?.Invoke(this, new XrTrackerEventArgs(TrackerType, _isTracking));
        }
    }

    /// <summary>
    /// 获取指定追踪空间的姿态
    /// </summary>
    /// <param name="space">追踪空间</param>
    /// <returns>追踪快照</returns>
    public XrTrackingSnapshot GetPose(XrTrackingSpace space)
    {
        return _currentSnapshot;
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 设置连接状态
    /// </summary>
    /// <param name="connected">是否已连接</param>
    internal void SetConnected(bool connected)
    {
        _isConnected = connected;
    }

    /// <summary>
    /// 更新追踪快照
    /// </summary>
    /// <param name="snapshot">新的追踪快照</param>
    internal void UpdateSnapshot(XrTrackingSnapshot snapshot)
    {
        _currentSnapshot = snapshot;
    }

    #endregion
}
