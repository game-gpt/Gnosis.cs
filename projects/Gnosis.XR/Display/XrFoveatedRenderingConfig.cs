using Gnosis.Core.Math;

namespace Gnosis.XR.Display;

/// <summary>
/// 注视点渲染配置，定义中心区域与外围区域的分辨率比例
/// </summary>
public sealed record XrFoveatedRenderingConfig
{
    /// <summary>
    /// 注视点渲染级别
    /// </summary>
    public XrFoveatedRenderingLevel Level { get; init; } = XrFoveatedRenderingLevel.Disabled;

    /// <summary>
    /// 中心区域水平比例（0.0 ~ 1.0）
    /// </summary>
    public float CenterRegionX { get; init; } = 0.25f;

    /// <summary>
    /// 中心区域垂直比例（0.0 ~ 1.0）
    /// </summary>
    public float CenterRegionY { get; init; } = 0.25f;

    /// <summary>
    /// 中心区域水平偏移（-1.0 ~ 1.0，用于追踪注视点）
    /// </summary>
    public float CenterRegionOffsetX { get; init; } = 0.0f;

    /// <summary>
    /// 中心区域垂直偏移（-1.0 ~ 1.0，用于追踪注视点）
    /// </summary>
    public float CenterRegionOffsetY { get; init; } = 0.0f;

    /// <summary>
    /// 中间区域水平比例（0.0 ~ 1.0）
    /// </summary>
    public float MidRegionX { get; init; } = 0.5f;

    /// <summary>
    /// 中间区域垂直比例（0.0 ~ 1.0）
    /// </summary>
    public float MidRegionY { get; init; } = 0.5f;

    /// <summary>
    /// 外围区域分辨率缩放比例（0.0 ~ 1.0）
    /// </summary>
    public float PeripheralScale { get; init; } = 0.25f;

    /// <summary>
    /// 中间区域分辨率缩放比例（0.0 ~ 1.0）
    /// </summary>
    public float MidScale { get; init; } = 0.5f;

    /// <summary>
    /// 是否启用注视点追踪（动态调整中心区域位置）
    /// </summary>
    public bool EnableEyeTracking { get; init; }

    /// <summary>
    /// 根据级别创建默认配置
    /// </summary>
    /// <param name="level">注视点渲染级别</param>
    /// <returns>对应的注视点渲染配置</returns>
    public static XrFoveatedRenderingConfig FromLevel(XrFoveatedRenderingLevel level)
    {
        return level switch
        {
            XrFoveatedRenderingLevel.Low => new XrFoveatedRenderingConfig
            {
                Level = level,
                CenterRegionX = 0.33f,
                CenterRegionY = 0.33f,
                MidRegionX = 0.6f,
                MidRegionY = 0.6f,
                PeripheralScale = 0.5f,
                MidScale = 0.7f
            },
            XrFoveatedRenderingLevel.Medium => new XrFoveatedRenderingConfig
            {
                Level = level,
                CenterRegionX = 0.25f,
                CenterRegionY = 0.25f,
                MidRegionX = 0.5f,
                MidRegionY = 0.5f,
                PeripheralScale = 0.25f,
                MidScale = 0.5f
            },
            XrFoveatedRenderingLevel.High => new XrFoveatedRenderingConfig
            {
                Level = level,
                CenterRegionX = 0.2f,
                CenterRegionY = 0.2f,
                MidRegionX = 0.4f,
                MidRegionY = 0.4f,
                PeripheralScale = 0.125f,
                MidScale = 0.35f
            },
            _ => new XrFoveatedRenderingConfig
            {
                Level = XrFoveatedRenderingLevel.Disabled
            }
        };
    }
}
