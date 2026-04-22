namespace Gnosis.XR.Session;

/// <summary>
/// XR 会话接口，管理 XR 运行时的连接与生命周期
/// </summary>
public interface IXrSession : IDisposable
{
    /// <summary>
    /// 当前会话状态
    /// </summary>
    XrSessionState State { get; }

    /// <summary>
    /// XR 模式
    /// </summary>
    XrMode Mode { get; }

    /// <summary>
    /// 是否正在运行
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 是否支持手部追踪
    /// </summary>
    bool SupportsHandTracking { get; }

    /// <summary>
    /// 是否支持注视点渲染
    /// </summary>
    bool SupportsFoveatedRendering { get; }

    /// <summary>
    /// 推荐的渲染目标宽度
    /// </summary>
    uint RecommendedRenderTargetWidth { get; }

    /// <summary>
    /// 推荐的渲染目标高度
    /// </summary>
    uint RecommendedRenderTargetHeight { get; }

    /// <summary>
    /// 会话状态变更事件
    /// </summary>
    event EventHandler<XrSessionEventArgs>? StateChanged;

    /// <summary>
    /// 启动 XR 会话
    /// </summary>
    void Start();

    /// <summary>
    /// 停止 XR 会话
    /// </summary>
    void Stop();

    /// <summary>
    /// 处理 XR 事件（每帧调用）
    /// </summary>
    void PollEvents();

    /// <summary>
    /// 请求退出 XR 会话
    /// </summary>
    void RequestExit();
}
