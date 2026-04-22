namespace Gnosis.XR.Tracking;

/// <summary>
/// XR 手部追踪器接口，提供手部骨骼追踪能力
/// </summary>
public interface IXrHandTracker : IXrTracker
{
    /// <summary>
    /// 手部追踪数据
    /// </summary>
    XrHandTrackingData HandData { get; }

    /// <summary>
    /// 是左手还是右手
    /// </summary>
    bool IsLeftHand { get; }

    /// <summary>
    /// 手部追踪准确度（0.0 ~ 1.0）
    /// </summary>
    float TrackingAccuracy { get; }
}

/// <summary>
/// XR 手部追踪器实现
/// </summary>
public sealed class XrHandTracker : IXrHandTracker
{
    #region 字段

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
    public XrTrackingSnapshot CurrentSnapshot { get; private set; }

    /// <summary>
    /// 手部追踪数据
    /// </summary>
    public XrHandTrackingData HandData { get; }

    /// <summary>
    /// 是左手还是右手
    /// </summary>
    public bool IsLeftHand { get; }

    /// <summary>
    /// 手部追踪准确度
    /// </summary>
    public float TrackingAccuracy => HandData.PositionAccuracy;

    #endregion

    #region 事件

    /// <summary>
    /// 追踪状态变更事件
    /// </summary>
    public event EventHandler<XrTrackerEventArgs>? TrackingChanged;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用手部标识初始化手部追踪器
    /// </summary>
    /// <param name="isLeftHand">是否为左手</param>
    public XrHandTracker(bool isLeftHand)
    {
        IsLeftHand = isLeftHand;
        TrackerType = isLeftHand ? XrTrackerType.LeftHand : XrTrackerType.RightHand;
        HandData = new XrHandTrackingData();
        _isConnected = false;
        _isTracking = false;
        CurrentSnapshot = new XrTrackingSnapshot
        {
            Pose = XrPose.Identity,
            Flags = XrTrackingFlags.None
        };
    }

    #endregion

    #region IXrTracker 实现

    /// <summary>
    /// 更新追踪数据
    /// </summary>
    public void Update()
    {
        if (!_isConnected)
        {
            return;
        }

        var wasTracking = _isTracking;
        _isTracking = HandData.IsTracking;

        if (wasTracking != _isTracking)
        {
            TrackingChanged?.Invoke(this, new XrTrackerEventArgs(TrackerType, _isTracking));
        }
    }

    /// <summary>
    /// 获取指定追踪空间的姿态
    /// </summary>
    public XrTrackingSnapshot GetPose(XrTrackingSpace space)
    {
        return CurrentSnapshot;
    }

    #endregion

    #region 内部方法

    /// <summary>
    /// 设置连接状态
    /// </summary>
    internal void SetConnected(bool connected)
    {
        _isConnected = connected;
    }

    /// <summary>
    /// 更新追踪快照
    /// </summary>
    internal void UpdateSnapshot(XrTrackingSnapshot snapshot)
    {
        CurrentSnapshot = snapshot;
    }

    #endregion
}
