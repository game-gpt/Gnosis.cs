namespace Gnosis.XR.Session;

/// <summary>
/// XR 会话实现，管理 XR 运行时的连接与生命周期
/// </summary>
public sealed class XrSession : IXrSession
{
    #region 字段

    private XrSessionState _state;
    private bool _isDisposed;

    #endregion

    #region 属性

    /// <summary>
    /// 当前会话状态
    /// </summary>
    public XrSessionState State => _state;

    /// <summary>
    /// XR 模式
    /// </summary>
    public XrMode Mode { get; }

    /// <summary>
    /// 是否正在运行
    /// </summary>
    public bool IsRunning => _state is XrSessionState.Ready or XrSessionState.Focused;

    /// <summary>
    /// 是否支持手部追踪
    /// </summary>
    public bool SupportsHandTracking { get; }

    /// <summary>
    /// 是否支持注视点渲染
    /// </summary>
    public bool SupportsFoveatedRendering { get; }

    /// <summary>
    /// 推荐的渲染目标宽度
    /// </summary>
    public uint RecommendedRenderTargetWidth { get; }

    /// <summary>
    /// 推荐的渲染目标高度
    /// </summary>
    public uint RecommendedRenderTargetHeight { get; }

    #endregion

    #region 事件

    /// <summary>
    /// 会话状态变更事件
    /// </summary>
    public event EventHandler<XrSessionEventArgs>? StateChanged;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用会话创建信息初始化 XR 会话
    /// </summary>
    /// <param name="createInfo">会话创建信息</param>
    public XrSession(XrSessionCreateInfo createInfo)
    {
        ArgumentNullException.ThrowIfNull(createInfo);

        Mode = createInfo.Mode;
        SupportsHandTracking = createInfo.RequestHandTracking;
        SupportsFoveatedRendering = createInfo.RequestFoveatedRendering;
        RecommendedRenderTargetWidth = createInfo.RecommendedRenderTargetWidth > 0
            ? createInfo.RecommendedRenderTargetWidth
            : 1920;
        RecommendedRenderTargetHeight = createInfo.RecommendedRenderTargetHeight > 0
            ? createInfo.RecommendedRenderTargetHeight
            : 1080;
        _state = XrSessionState.Created;
        _isDisposed = false;
    }

    #endregion

    #region IXrSession 实现

    /// <summary>
    /// 启动 XR 会话
    /// </summary>
    public void Start()
    {
        ThrowIfDisposed();

        if (_state != XrSessionState.Created && _state != XrSessionState.Pending)
        {
            throw new InvalidOperationException($"无法从状态 {_state} 启动 XR 会话");
        }

        SetState(XrSessionState.Pending);
        SetState(XrSessionState.Ready);
    }

    /// <summary>
    /// 停止 XR 会话
    /// </summary>
    public void Stop()
    {
        ThrowIfDisposed();

        if (_state == XrSessionState.Destroyed)
        {
            return;
        }

        SetState(XrSessionState.Quitting);
        SetState(XrSessionState.Destroyed);
    }

    /// <summary>
    /// 处理 XR 事件（每帧调用）
    /// </summary>
    public void PollEvents()
    {
        ThrowIfDisposed();

        if (_state == XrSessionState.Pending)
        {
            SetState(XrSessionState.Ready);
        }

        if (_state == XrSessionState.Ready)
        {
            SetState(XrSessionState.Focused);
        }

        if (_state == XrSessionState.Quitting)
        {
            SetState(XrSessionState.Destroyed);
        }
    }

    /// <summary>
    /// 请求退出 XR 会话
    /// </summary>
    public void RequestExit()
    {
        ThrowIfDisposed();

        if (_state is XrSessionState.Focused or XrSessionState.Ready or XrSessionState.VisibilityLoss)
        {
            SetState(XrSessionState.Quitting);
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 设置会话状态并触发事件
    /// </summary>
    private void SetState(XrSessionState newState)
    {
        var oldState = _state;
        _state = newState;

        if (oldState != newState)
        {
            StateChanged?.Invoke(this, new XrSessionEventArgs(newState, oldState));
        }
    }

    /// <summary>
    /// 若已释放则抛出异常
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(XrSession));
        }
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放会话资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_state != XrSessionState.Destroyed)
        {
            SetState(XrSessionState.Destroyed);
        }

        _isDisposed = true;
    }

    #endregion
}
