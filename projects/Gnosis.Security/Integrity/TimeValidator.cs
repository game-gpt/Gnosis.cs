using Gnosis.Core.Time;

namespace Gnosis.Security.Integrity;

public sealed class TimeValidator
{
    #region 字段

    private Timestamp _lastCheckTime;
    private double _lastRealTimeMs;
    private double _accumulatedGameDelta;
    private double _accumulatedRealDelta;
    private int _checkCount;
    private readonly double _toleranceRatio;

    #endregion

    #region 属性

    public bool IsTimeAnomalyDetected { get; private set; }
    public int AnomalyCount { get; private set; }

    #endregion

    #region 构造函数

    public TimeValidator(double toleranceRatio = 0.1)
    {
        _toleranceRatio = toleranceRatio;
        _lastCheckTime = Timestamp.Now;
        _lastRealTimeMs = Environment.TickCount64;
    }

    #endregion

    #region 公开方法

    public void RecordFrame(double gameDeltaMs)
    {
        var currentRealTimeMs = Environment.TickCount64;
        var realDeltaMs = currentRealTimeMs - _lastRealTimeMs;

        _accumulatedGameDelta += gameDeltaMs;
        _accumulatedRealDelta += realDeltaMs;
        _lastRealTimeMs = currentRealTimeMs;
        _checkCount++;

        if (_checkCount >= 60)
        {
            ValidateTime();
        }
    }

    public void Reset()
    {
        _accumulatedGameDelta = 0;
        _accumulatedRealDelta = 0;
        _checkCount = 0;
        IsTimeAnomalyDetected = false;
        AnomalyCount = 0;
        _lastCheckTime = Timestamp.Now;
        _lastRealTimeMs = Environment.TickCount64;
    }

    #endregion

    #region 私有方法

    private void ValidateTime()
    {
        if (_accumulatedRealDelta <= 0) return;

        var ratio = _accumulatedGameDelta / _accumulatedRealDelta;
        var deviation = Math.Abs(ratio - 1.0);

        if (deviation > _toleranceRatio)
        {
            IsTimeAnomalyDetected = true;
            AnomalyCount++;
        }

        _accumulatedGameDelta = 0;
        _accumulatedRealDelta = 0;
        _checkCount = 0;
        _lastCheckTime = Timestamp.Now;
    }

    #endregion
}
