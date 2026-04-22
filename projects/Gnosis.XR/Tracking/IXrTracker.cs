namespace Gnosis.XR.Tracking;

/// <summary>
/// XR 追踪器接口，提供设备空间追踪能力
/// </summary>
public interface IXrTracker
{
    /// <summary>
    /// 追踪器类型
    /// </summary>
    XrTrackerType TrackerType { get; }

    /// <summary>
    /// 是否已连接
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 是否正在追踪
    /// </summary>
    bool IsTracking { get; }

    /// <summary>
    /// 当前追踪快照
    /// </summary>
    XrTrackingSnapshot CurrentSnapshot { get; }

    /// <summary>
    /// 追踪状态变更事件
    /// </summary>
    event EventHandler<XrTrackerEventArgs>? TrackingChanged;

    /// <summary>
    /// 更新追踪数据（每帧调用）
    /// </summary>
    void Update();

    /// <summary>
    /// 获取指定追踪空间的姿态
    /// </summary>
    /// <param name="space">追踪空间</param>
    /// <returns>追踪快照</returns>
    XrTrackingSnapshot GetPose(XrTrackingSpace space);
}

/// <summary>
/// XR 追踪器事件参数
/// </summary>
public class XrTrackerEventArgs : EventArgs
{
    /// <summary>
    /// 追踪器类型
    /// </summary>
    public XrTrackerType TrackerType { get; }

    /// <summary>
    /// 是否正在追踪
    /// </summary>
    public bool IsTracking { get; }

    /// <summary>
    /// 初始化追踪器事件参数
    /// </summary>
    public XrTrackerEventArgs(XrTrackerType trackerType, bool isTracking)
    {
        TrackerType = trackerType;
        IsTracking = isTracking;
    }
}
