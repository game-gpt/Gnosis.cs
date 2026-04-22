namespace Gnosis.XR.Display;

/// <summary>
/// 注视点渲染级别
/// </summary>
public enum XrFoveatedRenderingLevel
{
    /// <summary>
    /// 禁用注视点渲染
    /// </summary>
    Disabled,

    /// <summary>
    /// 低级别注视点渲染（外围分辨率降低较少）
    /// </summary>
    Low,

    /// <summary>
    /// 中级别注视点渲染
    /// </summary>
    Medium,

    /// <summary>
    /// 高级别注视点渲染（外围分辨率降低较多）
    /// </summary>
    High
}
