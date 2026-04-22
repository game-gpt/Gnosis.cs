using Gnosis.Core.Diagnostic;
using Gnosis.Core.Thread;

namespace Gnosis.Core.Time;

/// <summary>
/// 游戏主循环，驱动系统调度器按帧更新
/// </summary>
public class GameLoop
{
    #region 字段

    private readonly TimeManager _timeManager;
    private readonly ILogger _logger;
    private readonly IScheduler _scheduler;
    private volatile bool _isRunning;
    private volatile bool _isPaused;
    private float _fixedTimeAccumulator;
    private bool _stopRequested;

    #endregion

    #region 属性

    /// <summary>
    /// 游戏循环是否正在运行
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// 游戏循环是否已暂停
    /// </summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// 每帧最大固定步更新次数，避免死亡螺旋
    /// </summary>
    public int MaxFixedUpdatesPerFrame { get; set; } = 5;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化游戏主循环
    /// </summary>
    /// <param name="timeManager">时间管理器</param>
    /// <param name="scheduler">系统调度器</param>
    /// <param name="logger">日志器</param>
    public GameLoop(TimeManager timeManager, IScheduler scheduler, ILogger logger)
    {
        _timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 启动游戏循环，阻塞调用线程直到 Stop() 被调用
    /// </summary>
    public void Run()
    {
        if (_isRunning)
        {
            throw new InvalidOperationException("游戏循环已在运行中");
        }

        _isRunning = true;
        _isPaused = false;
        _stopRequested = false;
        _fixedTimeAccumulator = 0f;
        _timeManager.Reset();

        _logger.LogInfo("游戏循环启动");

        while (_isRunning && !_stopRequested)
        {
            _timeManager.BeginFrame();

            if (!_isPaused)
            {
                ProcessFixedUpdate();
                _scheduler.Update(_timeManager.DeltaTime);
            }

            _timeManager.WaitForTargetFrameRate();
        }

        _isRunning = false;
        _logger.LogInfo("游戏循环已停止");
    }

    /// <summary>
    /// 暂停游戏循环，停止调度系统更新但循环仍在运行
    /// </summary>
    public void Pause()
    {
        if (!_isRunning)
        {
            return;
        }

        _isPaused = true;
        _fixedTimeAccumulator = 0f;
        _logger.LogInfo("游戏循环已暂停");
    }

    /// <summary>
    /// 恢复游戏循环，从暂停状态恢复调度
    /// </summary>
    public void Resume()
    {
        if (!_isRunning || !_isPaused)
        {
            return;
        }

        _isPaused = false;
        _fixedTimeAccumulator = 0f;
        _logger.LogInfo("游戏循环已恢复");
    }

    /// <summary>
    /// 停止游戏循环，在当前帧结束后退出
    /// </summary>
    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _stopRequested = true;
        _logger.LogInfo("游戏循环停止请求已发送");
    }

    #endregion

    #region 私有方法

    private void ProcessFixedUpdate()
    {
        _fixedTimeAccumulator += _timeManager.UnscaledDeltaTime;

        var fixedDelta = _timeManager.FixedDeltaTime;
        var updateCount = 0;

        while (_fixedTimeAccumulator >= fixedDelta && updateCount < MaxFixedUpdatesPerFrame)
        {
            _timeManager.IncrementFixedFrameCount();
            _scheduler.FixedUpdate(fixedDelta);
            _fixedTimeAccumulator -= fixedDelta;
            updateCount++;
        }

        if (_fixedTimeAccumulator > fixedDelta * MaxFixedUpdatesPerFrame)
        {
            _fixedTimeAccumulator = 0f;
        }
    }

    #endregion
}
