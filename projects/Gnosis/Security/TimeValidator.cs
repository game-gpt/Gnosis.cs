using Gnosis.Infrastructure;

namespace Gnosis.Security;

/// <summary>
/// 时间校验器，检测变速齿轮等时间篡改行为
/// </summary>
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

    /// <summary>
    /// 获取是否检测到时间异常
    /// </summary>
    public bool IsTimeAnomalyDetected { get; private set; }

    /// <summary>
    /// 获取检测到异常的次数
    /// </summary>
    public int AnomalyCount { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化时间校验器
    /// </summary>
    /// <param name="toleranceRatio">时间偏差容忍比例，默认 0.1（10%）</param>
    public TimeValidator(double toleranceRatio = 0.1)
    {
        _toleranceRatio = toleranceRatio;
        _lastCheckTime = Timestamp.Now;
        _lastRealTimeMs = Environment.TickCount64;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 记录游戏帧的时间增量，用于与真实时间对比
    /// </summary>
    /// <param name="gameDeltaMs">游戏帧的时间增量（毫秒）</param>
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

    /// <summary>
    /// 重置校验器状态
    /// </summary>
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
        if (_accumulatedRealDelta <= 0)
        {
            return;
        }

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
