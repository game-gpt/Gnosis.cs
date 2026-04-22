namespace Gnosis.XR.Session;

/// <summary>
/// XR 会话创建描述
/// </summary>
public sealed record XrSessionCreateInfo
{
    /// <summary>
    /// 应用名称
    /// </summary>
    public string ApplicationName { get; init; } = "Gnosis XR";

    /// <summary>
    /// 应用版本
    /// </summary>
    public uint ApplicationVersion { get; init; } = 1;

    /// <summary>
    /// 引擎名称
    /// </summary>
    public string EngineName { get; init; } = "Gnosis";

    /// <summary>
    /// 引擎版本
    /// </summary>
    public uint EngineVersion { get; init; } = 1;

    /// <summary>
    /// XR 模式
    /// </summary>
    public XrMode Mode { get; init; } = XrMode.VR;

    /// <summary>
    /// 请求的颜色格式
    /// </summary>
    public string RequestedColorFormat { get; init; } = "B8G8R8A8Unorm";

    /// <summary>
    /// 请求的深度格式
    /// </summary>
    public string RequestedDepthFormat { get; init; } = "D32FloatS8Uint";

    /// <summary>
    /// 是否请求手部追踪
    /// </summary>
    public bool RequestHandTracking { get; init; }

    /// <summary>
    /// 是否请求注视点渲染
    /// </summary>
    public bool RequestFoveatedRendering { get; init; }

    /// <summary>
    /// 是否请求空间锚点
    /// </summary>
    public bool RequestSpatialAnchors { get; init; }

    /// <summary>
    /// 推荐渲染目标宽度
    /// </summary>
    public uint RecommendedRenderTargetWidth { get; init; } = 0;

    /// <summary>
    /// 推荐渲染目标高度
    /// </summary>
    public uint RecommendedRenderTargetHeight { get; init; } = 0;
}
