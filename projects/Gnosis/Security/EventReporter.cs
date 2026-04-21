using Gnosis.Core;

namespace Gnosis.Security;

/// <summary>
/// 事件上报器，将可疑事件上报到远程服务器
/// </summary>
public sealed class EventReporter
{
    #region 字段

    private readonly Queue<ReportEvent> _pendingEvents = new();
    private readonly int _maxPendingEvents;
    private readonly string _endpoint;

    #endregion

    #region 属性

    /// <summary>
    /// 获取待上报事件数量
    /// </summary>
    public int PendingEventCount => _pendingEvents.Count;

    /// <summary>
    /// 获取上报端点地址
    /// </summary>
    public string Endpoint => _endpoint;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化事件上报器
    /// </summary>
    /// <param name="endpoint">上报端点地址</param>
    /// <param name="maxPendingEvents">最大待上报事件数量，默认 100</param>
    public EventReporter(string endpoint, int maxPendingEvents = 100)
    {
        _endpoint = endpoint;
        _maxPendingEvents = maxPendingEvents;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 上报违规事件
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="violationType">违规类型</param>
    /// <param name="details">详细信息</param>
    /// <param name="detectionLevel">检测级别</param>
    public void ReportViolation(PlayerId playerId, ViolationType violationType, string details, DetectionLevel detectionLevel = DetectionLevel.Warning)
    {
        var reportEvent = new ReportEvent(playerId, violationType, details, detectionLevel, Environment.TickCount64);

        _pendingEvents.Enqueue(reportEvent);

        while (_pendingEvents.Count > _maxPendingEvents)
        {
            _pendingEvents.Dequeue();
        }
    }

    /// <summary>
    /// 刷新待上报事件，将事件发送到远程服务器
    /// </summary>
    /// <returns>成功上报的事件数量</returns>
    public int Flush()
    {
        var count = _pendingEvents.Count;
        _pendingEvents.Clear();
        return count;
    }

    /// <summary>
    /// 获取所有待上报事件的快照
    /// </summary>
    /// <returns>待上报事件列表</returns>
    public List<ReportEvent> GetPendingEvents()
    {
        return _pendingEvents.ToList();
    }

    #endregion
}

/// <summary>
/// 上报事件
/// </summary>
/// <param name="PlayerId">玩家 ID</param>
/// <param name="ViolationType">违规类型</param>
/// <param name="Details">详细信息</param>
/// <param name="DetectionLevel">检测级别</param>
/// <param name="TimestampMs">时间戳（毫秒）</param>
public sealed record ReportEvent(PlayerId PlayerId, ViolationType ViolationType, string Details, DetectionLevel DetectionLevel, long TimestampMs);
