using Gnosis.Core;
using Gnosis.Core.Event;

namespace Gnosis.Security.AntiCheat;

public sealed class EventReporter
{
    #region 字段

    private readonly Queue<ReportEvent> _pendingEvents = new();
    private readonly int _maxPendingEvents;
    private readonly string _endpoint;

    #endregion

    #region 属性

    public int PendingEventCount => _pendingEvents.Count;
    public string Endpoint => _endpoint;

    #endregion

    #region 构造函数

    public EventReporter(string endpoint, int maxPendingEvents = 100)
    {
        _endpoint = endpoint;
        _maxPendingEvents = maxPendingEvents;
    }

    #endregion

    #region 公开方法

    public void ReportViolation(PlayerId playerId, ViolationType violationType, string details, DetectionLevel detectionLevel = DetectionLevel.Warning)
    {
        var reportEvent = new ReportEvent(playerId, violationType, details, detectionLevel, Environment.TickCount64);
        _pendingEvents.Enqueue(reportEvent);

        while (_pendingEvents.Count > _maxPendingEvents)
        {
            _pendingEvents.Dequeue();
        }
    }

    public int Flush()
    {
        var count = _pendingEvents.Count;
        _pendingEvents.Clear();
        return count;
    }

    public List<ReportEvent> GetPendingEvents()
    {
        return _pendingEvents.ToList();
    }

    #endregion
}

public sealed record ReportEvent(PlayerId PlayerId, ViolationType ViolationType, string Details, DetectionLevel DetectionLevel, long TimestampMs);
