using Gnosis.Assets.VFS;
using Gnosis.Core;

namespace Gnosis.Infrastructure;

/// <summary>
/// Gnosis 引擎入口，统一初始化和管理各子系统
/// </summary>
public class GnosisEngine : IDisposable
{
    #region 字段

    private readonly ILogger _logger;
    private readonly ITimeManager _timeManager;
    private readonly IGameLoop _gameLoop;
    private bool _isInitialized;
    private bool _isDisposed;

    #endregion

    #region 属性

    /// <summary>
    /// 日志器
    /// </summary>
    public ILogger Logger => _logger;

    /// <summary>
    /// 时间管理器
    /// </summary>
    public ITimeManager TimeManager => _timeManager;

    /// <summary>
    /// 游戏主循环
    /// </summary>
    public IGameLoop GameLoop => _gameLoop;

    /// <summary>
    /// 配置系统
    /// </summary>
    public IConfigSystem? ConfigSystem { get; private set; }

    /// <summary>
    /// 引擎是否已初始化
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// 引擎是否正在运行
    /// </summary>
    public bool IsRunning => _gameLoop.IsRunning;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 Gnosis 引擎
    /// </summary>
    /// <param name="logger">日志器</param>
    /// <param name="timeManager">时间管理器</param>
    /// <param name="gameLoop">游戏主循环</param>
    public GnosisEngine(ILogger logger, ITimeManager timeManager, IGameLoop gameLoop)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
        _gameLoop = gameLoop ?? throw new ArgumentNullException(nameof(gameLoop));
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 初始化引擎子系统
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("引擎已初始化，不允许重复初始化");
        }

        _logger.LogInfo("Gnosis 引擎初始化开始");

        _timeManager.Reset();

        _isInitialized = true;

        _logger.LogInfo("Gnosis 引擎初始化完成");
    }

    /// <summary>
    /// 设置配置系统
    /// </summary>
    /// <param name="configSystem">配置系统实例</param>
    public void SetConfigSystem(IConfigSystem configSystem)
    {
        ConfigSystem = configSystem ?? throw new ArgumentNullException(nameof(configSystem));
        _logger.LogInfo("配置系统已设置");
    }

    /// <summary>
    /// 注册系统到引擎
    /// </summary>
    /// <param name="system">要注册的系统</param>
    public void AddSystem(ISystem system)
    {
        if (system == null)
        {
            throw new ArgumentNullException(nameof(system));
        }

        _gameLoop.AddSystem(system);
        _logger.LogInfo($"系统已注册：{system.GetType().Name}");
    }

    /// <summary>
    /// 启动引擎，开始游戏主循环
    /// </summary>
    public void Run()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("引擎未初始化，请先调用 Initialize()");
        }

        _logger.LogInfo("Gnosis 引擎启动");
        _gameLoop.Run();
    }

    /// <summary>
    /// 关闭引擎，停止游戏主循环
    /// </summary>
    public void Shutdown()
    {
        if (!_isInitialized)
        {
            return;
        }

        _logger.LogInfo("Gnosis 引擎关闭中");

        _gameLoop.Stop();

        ShutdownSystems();

        _isInitialized = false;

        _logger.LogInfo("Gnosis 引擎已关闭");
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放引擎资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        Shutdown();
        _isDisposed = true;
    }

    #endregion

    #region 私有方法

    private void ShutdownSystems()
    {
        if (_gameLoop is GameLoop loop)
        {
            loop.Stop();
        }

        if (ConfigSystem is ISystem system)
        {
            system.Shutdown();
        }

        if (_logger is ConsoleLogger consoleLogger)
        {
            consoleLogger.Dispose();
        }
    }

    #endregion
}
