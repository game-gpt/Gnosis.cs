using Gnosis.Graphic.RHI;
using Gnosis.XR.Tracking;

namespace Gnosis.XR.Composition;

/// <summary>
/// XR 图层基础信息，定义所有图层类型的公共属性
/// </summary>
public abstract class XrLayerInfo
{
    /// <summary>
    /// 图层类型
    /// </summary>
    public XrLayerType LayerType { get; }

    /// <summary>
    /// 图层排序顺序（数值越大越靠前）
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 混合模式
    /// </summary>
    public XrLayerBlendMode BlendMode { get; set; }

    /// <summary>
    /// 图层纹理资源
    /// </summary>
    public IResource? Texture { get; set; }

    /// <summary>
    /// 初始化图层基础信息
    /// </summary>
    /// <param name="layerType">图层类型</param>
    protected XrLayerInfo(XrLayerType layerType)
    {
        LayerType = layerType;
        SortOrder = 0;
        IsEnabled = true;
        BlendMode = XrLayerBlendMode.PremultipliedAlpha;
    }
}
