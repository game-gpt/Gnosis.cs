namespace Gnosis.XR.Display;

/// <summary>
/// XR 视图配置类型
/// </summary>
public enum XrViewConfigurationType
{
    /// <summary>
    /// 单眼配置（非立体）
    /// </summary>
    Mono,

    /// <summary>
    /// 双眼立体配置
    /// </summary>
    Stereo,

    /// <summary>
    /// 双眼立体配置（单通道多视图优化）
    /// </summary>
    StereoMultiview
}
