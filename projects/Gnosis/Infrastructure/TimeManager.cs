using System.Diagnostics;

namespace Gnosis.Infrastructure;

/// <summary>
/// 时间管理器，负责帧时间追踪、固定时间步、时间缩放和帧率控制
/// </summary>
public class TimeManager
{
    #region 字段

    private readonly Stopwatch _stopwatch;
    private long _lastFrameTicks;
    private float _unscaledDeltaTime;
    private float _deltaTime;
    private float _unscaledTotalTime;
    private float _totalTime;
    private long _frameCount;
    private long _fixedFrameCount;
    private float _timeScale;
    private int _targetFrameRate;
    private long _frameStartTicks;

    #endregion

    #region 属性

    /// <summary>
    /// 上一帧到当前帧的耗时（秒），受 TimeScale 缩放
    /// </summary>
    public float DeltaTime => _deltaTime;

    /// <summary>
    /// 未经缩放的实际 delta 时间（秒）
    /// </summary>
    public float UnscaledDeltaTime => _unscaledDeltaTime;

    /// <summary>
    /// 自引擎启动以来的总耗时（缩放后，秒）
    /// </summary>
    public float TotalTime => _totalTime;

    /// <summary>
    /// 自引擎启动以来的总耗时（原始，秒）
    /// </summary>
    public float UnscaledTotalTime => _unscaledTotalTime;

    /// <summary>
    /// 帧计数
    /// </summary>
    public long FrameCount => _frameCount;

    /// <summary>
    /// 固定时间步长，默认 1/60 秒
    /// </summary>
    public float FixedDeltaTime { get; set; }

    /// <summary>
    /// 固定步更新计数
    /// </summary>
    public long FixedFrameCount => _fixedFrameCount;

    /// <summary>
    /// 时间缩放因子，默认 1.0
    /// </summary>
    public float TimeScale
    {
        get => _timeScale;
        set
        {
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "时间缩放因子不能为负数");
            }
            _timeScale = value;
        }
    }

    /// <summary>
    /// 目标帧率，默认 0（不限制）
    /// </summary>
    public int TargetFrameRate
    {
        get => _targetFrameRate;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "目标帧率不能为负数");
            }
            _targetFrameRate = value;
        }
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化时间管理器
    /// </summary>
    public TimeManager()
    {
        _stopwatch = new Stopwatch();
        FixedDeltaTime = 1f / 60f;
        _timeScale = 1f;
        _targetFrameRate = 0;
        _frameCount = 0;
        _fixedFrameCount = 0;
        _unscaledDeltaTime = 0f;
        _deltaTime = 0f;
        _unscaledTotalTime = 0f;
        _totalTime = 0f;
        _lastFrameTicks = 0;
        _frameStartTicks = 0;

        _stopwatch.Start();
        _lastFrameTicks = _stopwatch.ElapsedTicks;
        _frameStartTicks = _lastFrameTicks;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 每帧开始时调用，计算 delta 时间
    /// </summary>
    public void BeginFrame()
    {
        var currentTicks = _stopwatch.ElapsedTicks;
        _unscaledDeltaTime = (float)(currentTicks - _lastFrameTicks) / Stopwatch.Frequency;
        _lastFrameTicks = currentTicks;
        _frameStartTicks = currentTicks;

        _deltaTime = _unscaledDeltaTime * _timeScale;
        _unscaledTotalTime += _unscaledDeltaTime;
        _totalTime += _deltaTime;
        _frameCount++;
    }

    /// <summary>
    /// 每次固定步更新时调用，递增固定帧计数
    /// </summary>
    public void IncrementFixedFrameCount()
    {
        _fixedFrameCount++;
    }

    /// <summary>
    /// 如果帧提前完成，等待剩余时间以达到目标帧率
    /// </summary>
    public void WaitForTargetFrameRate()
    {
        if (_targetFrameRate <= 0)
        {
            return;
        }

        var targetFrameDurationMs = 1000.0 / _targetFrameRate;
        var elapsedMs = (double)(_stopwatch.ElapsedTicks - _frameStartTicks) / Stopwatch.Frequency * 1000.0;
        var remainingMs = targetFrameDurationMs - elapsedMs;

        if (remainingMs > 0)
        {
            Thread.Sleep((int)remainingMs);
        }
    }

    /// <summary>
    /// 重置所有时间状态
    /// </summary>
    public void Reset()
    {
        _stopwatch.Restart();
        _lastFrameTicks = _stopwatch.ElapsedTicks;
        _frameStartTicks = _lastFrameTicks;
        _unscaledDeltaTime = 0f;
        _deltaTime = 0f;
        _unscaledTotalTime = 0f;
        _totalTime = 0f;
        _frameCount = 0;
        _fixedFrameCount = 0;
        _timeScale = 1f;
        _targetFrameRate = 0;
        FixedDeltaTime = 1f / 60f;
    }

    #endregion
}
