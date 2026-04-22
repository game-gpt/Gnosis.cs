using Gnosis.Graphic.RHI;
using Gnosis.XR.Session;

namespace Gnosis.XR.Composition;

/// <summary>
/// XR 合成器接口，管理图层合成与帧提交
/// </summary>
public interface IXrCompositor : IDisposable
{
    /// <summary>
    /// 关联的 XR 会话
    /// </summary>
    IXrSession Session { get; }

    /// <summary>
    /// 图层列表
    /// </summary>
    IReadOnlyList<XrLayerInfo> Layers { get; }

    /// <summary>
    /// 环境混合模式
    /// </summary>
    XrLayerBlendMode EnvironmentBlendMode { get; }

    /// <summary>
    /// 添加图层
    /// </summary>
    /// <param name="layer">图层信息</param>
    void AddLayer(XrLayerInfo layer);

    /// <summary>
    /// 移除图层
    /// </summary>
    /// <param name="layer">图层信息</param>
    void RemoveLayer(XrLayerInfo layer);

    /// <summary>
    /// 获取指定类型的图层列表
    /// </summary>
    /// <typeparam name="T">图层类型</typeparam>
    /// <returns>图层列表</returns>
    IReadOnlyList<T> GetLayers<T>() where T : XrLayerInfo;

    /// <summary>
    /// 合成所有图层并提交到 XR 运行时
    /// </summary>
    /// <param name="leftEyeTexture">左眼渲染结果纹理</param>
    /// <param name="rightEyeTexture">右眼渲染结果纹理</param>
    void Composite(IResource? leftEyeTexture, IResource? rightEyeTexture);

    /// <summary>
    /// 获取推荐的交换链纹理描述
    /// </summary>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>纹理描述</returns>
    TextureDesc GetRecommendedSwapchainTextureDesc(uint width, uint height);
}
