using System.Diagnostics;
using Gnosis.Core;
using Gnosis.ECS;

namespace Gnosis.Infrastructure;

/// <summary>
/// 游戏主循环，驱动系统按帧更新
/// </summary>
public class GameLoop : IGameLoop
{
    #region 字段

    private readonly ITimeManager _timeManager;
    private readonly ILogger _logger;
    private readonly List<ISystem> _systems = new();
    private readonly Dictionary<SystemPhase, List<ISystem>> _systemsByPhase = new();
    private volatile bool _isRunning;
    private int _targetFrameRate;

    #endregion

    #region 属性

    /// <summary>
    /// 游戏循环是否正在运行
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// 目标帧率，0 表示不限制
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
            _timeManager.TargetFrameRate = value;
        }
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化游戏主循环
    /// </summary>
    /// <param name="timeManager">时间管理器</param>
    /// <param name="logger">日志器</param>
    public GameLoop(ITimeManager timeManager, ILogger logger)
    {
        _timeManager = timeManager;
        _logger = logger;

        foreach (SystemPhase phase in Enum.GetValues(typeof(SystemPhase)))
        {
            _systemsByPhase[phase] = new List<ISystem>();
        }
    }

    #endregion

    #region IGameLoop 实现

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
        _logger.LogInfo("游戏循环启动");

        InitializeSystems();

        var stopwatch = Stopwatch.StartNew();

        while (_isRunning)
        {
            _timeManager.BeginFrame();

            var deltaTime = _timeManager.DeltaTime;

            UpdateSystems(deltaTime);

            ProcessFixedUpdates();

            _timeManager.WaitForTargetFrameRate();
        }

        stopwatch.Stop();
        _logger.LogInfo($"游戏循环停止，运行时长：{stopwatch.Elapsed.TotalSeconds:F2} 秒，总帧数：{_timeManager.FrameCount}");
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

        _isRunning = false;
        _logger.LogInfo("游戏循环停止请求已发送");
    }

    /// <summary>
    /// 注册系统到游戏循环
    /// </summary>
    public void AddSystem(ISystem system)
    {
        if (system == null)
        {
            throw new ArgumentNullException(nameof(system));
        }

        _systems.Add(system);

        if (!_systemsByPhase.ContainsKey(system.Phase))
        {
            _systemsByPhase[system.Phase] = new List<ISystem>();
        }

        _systemsByPhase[system.Phase].Add(system);
    }

    /// <summary>
    /// 移除系统
    /// </summary>
    public void RemoveSystem(ISystem system)
    {
        if (system == null)
        {
            throw new ArgumentNullException(nameof(system));
        }

        _systems.Remove(system);

        if (_systemsByPhase.ContainsKey(system.Phase))
        {
            _systemsByPhase[system.Phase].Remove(system);
        }
    }

    #endregion

    #region 私有方法

    private void InitializeSystems()
    {
        foreach (var phase in GetOrderedPhases())
        {
            foreach (var system in _systemsByPhase[phase])
            {
                try
                {
                    system.Initialize();
                    _logger.LogInfo($"系统已初始化：{system.GetType().Name}（阶段：{phase}）");
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, $"系统初始化失败：{system.GetType().Name}");
                    throw;
                }
            }
        }
    }

    private void UpdateSystems(float deltaTime)
    {
        foreach (var phase in GetOrderedPhases())
        {
            foreach (var system in _systemsByPhase[phase])
            {
                try
                {
                    system.Update(deltaTime);
                }
                catch (Exception ex)
                {
                    _logger.LogException(ex, $"系统更新失败：{system.GetType().Name}");
                }
            }
        }
    }

    private void ProcessFixedUpdates()
    {
        var fixedDeltaTime = _timeManager.FixedDeltaTime;
        var accumulatedTime = _timeManager.UnscaledDeltaTime;

        while (accumulatedTime >= fixedDeltaTime)
        {
            _timeManager.IncrementFixedFrameCount();
            accumulatedTime -= fixedDeltaTime;
        }
    }

    private static IEnumerable<SystemPhase> GetOrderedPhases()
    {
        return new[]
        {
            SystemPhase.Initialization,
            SystemPhase.PreUpdate,
            SystemPhase.Update,
            SystemPhase.PostUpdate,
            SystemPhase.PreRender,
            SystemPhase.Render,
            SystemPhase.PostRender,
            SystemPhase.Cleanup
        };
    }

    #endregion
}
