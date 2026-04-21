using Gnosis.Core;

namespace Gnosis.Infrastructure;

/// <summary>
/// 游戏主循环接口，定义游戏循环的契约
/// </summary>
public interface IGameLoop
{
    #region Properties

    /// <summary>
    /// 游戏循环是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 目标帧率，0 表示不限制
    /// </summary>
    int TargetFrameRate { get; set; }

    #endregion

    #region Methods

    /// <summary>
    /// 启动游戏循环，阻塞调用线程直到 Stop() 被调用
    /// </summary>
    void Run();

    /// <summary>
    /// 停止游戏循环，在当前帧结束后退出
    /// </summary>
    void Stop();

    /// <summary>
    /// 注册系统到游戏循环
    /// </summary>
    void AddSystem(ISystem system);

    /// <summary>
    /// 移除系统
    /// </summary>
    void RemoveSystem(ISystem system);

    #endregion
}
