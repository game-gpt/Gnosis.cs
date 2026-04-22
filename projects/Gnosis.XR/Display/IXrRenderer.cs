using Gnosis.Graphic.RHI;
using Gnosis.XR.Session;
using Gnosis.XR.Tracking;

namespace Gnosis.XR.Display;

/// <summary>
/// XR 渲染器接口，提供立体渲染、单通道多视图与注视点渲染能力
/// </summary>
public interface IXrRenderer : IDisposable
{
    /// <summary>
    /// 关联的 XR 会话
    /// </summary>
    IXrSession Session { get; }

    /// <summary>
    /// 视图配置类型
    /// </summary>
    XrViewConfigurationType ViewConfiguration { get; }

    /// <summary>
    /// 渲染目标宽度
    /// </summary>
    uint RenderTargetWidth { get; }

    /// <summary>
    /// 渲染目标高度
    /// </summary>
    uint RenderTargetHeight { get; }

    /// <summary>
    /// 是否支持单通道多视图渲染
    /// </summary>
    bool SupportsMultiview { get; }

    /// <summary>
    /// 是否支持注视点渲染
    /// </summary>
    bool SupportsFoveatedRendering { get; }

    /// <summary>
    /// 当前注视点渲染配置
    /// </summary>
    XrFoveatedRenderingConfig FoveatedRenderingConfig { get; set; }

    /// <summary>
    /// 左眼视图信息
    /// </summary>
    XrViewInfo LeftEyeView { get; }

    /// <summary>
    /// 右眼视图信息
    /// </summary>
    XrViewInfo RightEyeView { get; }

    /// <summary>
    /// 渲染帧开始事件
    /// </summary>
    event EventHandler<XrRenderFrameEventArgs>? FrameBegin;

    /// <summary>
    /// 渲染帧结束事件
    /// </summary>
    event EventHandler<XrRenderFrameEventArgs>? FrameEnd;

    /// <summary>
    /// 初始化渲染器（创建渲染目标、帧缓冲等资源）
    /// </summary>
    /// <param name="device">RHI 图形设备</param>
    void Initialize(IDevice device);

    /// <summary>
    /// 开始渲染帧（获取最新的视图信息与渲染目标）
    /// </summary>
    /// <returns>是否成功获取渲染帧</returns>
    bool BeginFrame();

    /// <summary>
    /// 获取指定眼睛的渲染目标纹理
    /// </summary>
    /// <param name="eye">眼睛标识</param>
    /// <returns>渲染目标纹理资源</returns>
    IResource? GetRenderTarget(XrEye eye);

    /// <summary>
    /// 获取指定眼睛的深度模板纹理
    /// </summary>
    /// <param name="eye">眼睛标识</param>
    /// <returns>深度模板纹理资源</returns>
    IResource? GetDepthStencil(XrEye eye);

    /// <summary>
    /// 获取指定眼睛的帧缓冲
    /// </summary>
    /// <param name="eye">眼睛标识</param>
    /// <returns>帧缓冲对象</returns>
    IRhiFramebuffer? GetFramebuffer(XrEye eye);

    /// <summary>
    /// 获取指定眼睛的视口参数
    /// </summary>
    /// <param name="eye">眼睛标识</param>
    /// <returns>视口参数（x, y, width, height）</returns>
    (float x, float y, float width, float height) GetViewport(XrEye eye);

    /// <summary>
    /// 获取指定眼睛的视图信息
    /// </summary>
    /// <param name="eye">眼睛标识</param>
    /// <returns>视图信息</returns>
    XrViewInfo GetViewInfo(XrEye eye);

    /// <summary>
    /// 结束渲染帧（提交渲染结果到 XR 运行时）
    /// </summary>
    void EndFrame();

    /// <summary>
    /// 更新视图信息（每帧调用，根据追踪数据更新视图矩阵与投影矩阵）
    /// </summary>
    /// <param name="headTracker">头部追踪器</param>
    void UpdateViews(IXrTracker headTracker);

    /// <summary>
    /// 重新创建渲染目标（分辨率变更时调用）
    /// </summary>
    /// <param name="width">新宽度</param>
    /// <param name="height">新高度</param>
    void ResizeRenderTargets(uint width, uint height);
}

/// <summary>
/// XR 渲染帧事件参数
/// </summary>
public class XrRenderFrameEventArgs : EventArgs
{
    /// <summary>
    /// 帧索引
    /// </summary>
    public ulong FrameIndex { get; }

    /// <summary>
    /// 预测的显示时间（纳秒）
    /// </summary>
    public long PredictedDisplayTime { get; }

    /// <summary>
    /// 初始化渲染帧事件参数
    /// </summary>
    public XrRenderFrameEventArgs(ulong frameIndex, long predictedDisplayTime)
    {
        FrameIndex = frameIndex;
        PredictedDisplayTime = predictedDisplayTime;
    }
}
