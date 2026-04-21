namespace Gnosis.Infrastructure;

/// <summary>
/// 时间管理器接口，负责帧时间追踪、固定时间步、时间缩放和帧率控制
/// </summary>
public interface ITimeManager
{
    #region Properties

    /// <summary>
    /// 上一帧到当前帧的耗时（秒），受 TimeScale 缩放
    /// </summary>
    float DeltaTime { get; }

    /// <summary>
    /// 未经缩放的实际 delta 时间（秒）
    /// </summary>
    float UnscaledDeltaTime { get; }

    /// <summary>
    /// 自引擎启动以来的总耗时（缩放后，秒）
    /// </summary>
    float TotalTime { get; }

    /// <summary>
    /// 自引擎启动以来的总耗时（原始，秒）
    /// </summary>
    float UnscaledTotalTime { get; }

    /// <summary>
    /// 帧计数
    /// </summary>
    long FrameCount { get; }

    /// <summary>
    /// 固定时间步长，默认 1/60 秒
    /// </summary>
    float FixedDeltaTime { get; set; }

    /// <summary>
    /// 固定步更新计数
    /// </summary>
    long FixedFrameCount { get; }

    /// <summary>
    /// 时间缩放因子，默认 1.0
    /// </summary>
    float TimeScale { get; set; }

    /// <summary>
    /// 目标帧率，默认 0（不限制）
    /// </summary>
    int TargetFrameRate { get; set; }

    #endregion

    #region Methods

    /// <summary>
    /// 每帧开始时调用，计算 delta 时间
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// 每次固定步更新时调用，递增固定帧计数
    /// </summary>
    void IncrementFixedFrameCount();

    /// <summary>
    /// 如果帧提前完成，等待剩余时间以达到目标帧率
    /// </summary>
    void WaitForTargetFrameRate();

    /// <summary>
    /// 重置所有时间状态
    /// </summary>
    void Reset();

    #endregion
}
