namespace Gnosis.XR.Composition;

/// <summary>
/// XR 图层混合模式
/// </summary>
public enum XrLayerBlendMode
{
    /// <summary>
    /// 不透明覆盖（替换底层内容）
    /// </summary>
    Opaque,

    /// <summary>
    /// 预乘 Alpha 混合
    /// </summary>
    PremultipliedAlpha,

    /// <summary>
    /// 加法混合
    /// </summary>
    Additive
}
