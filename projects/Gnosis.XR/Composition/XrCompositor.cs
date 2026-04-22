using Gnosis.Graphic.RHI;
using Gnosis.XR.Session;

namespace Gnosis.XR.Composition;

/// <summary>
/// XR 合成器实现，管理图层合成与帧提交
/// </summary>
public sealed class XrCompositor : IXrCompositor
{
    #region 字段

    private readonly List<XrLayerInfo> _layers;
    private bool _isDisposed;

    #endregion

    #region 属性

    /// <summary>
    /// 关联的 XR 会话
    /// </summary>
    public IXrSession Session { get; }

    /// <summary>
    /// 图层列表
    /// </summary>
    public IReadOnlyList<XrLayerInfo> Layers => _layers.AsReadOnly();

    /// <summary>
    /// 环境混合模式
    /// </summary>
    public XrLayerBlendMode EnvironmentBlendMode { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用 XR 会话初始化合成器
    /// </summary>
    /// <param name="session">XR 会话</param>
    public XrCompositor(IXrSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
        _layers = [];
        EnvironmentBlendMode = session.Mode == XrMode.AR
            ? XrLayerBlendMode.PremultipliedAlpha
            : XrLayerBlendMode.Opaque;
        _isDisposed = false;
    }

    #endregion

    #region IXrCompositor 实现

    /// <summary>
    /// 添加图层
    /// </summary>
    /// <param name="layer">图层信息</param>
    public void AddLayer(XrLayerInfo layer)
    {
        ArgumentNullException.ThrowIfNull(layer);
        ThrowIfDisposed();

        _layers.Add(layer);
        _layers.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
    }

    /// <summary>
    /// 移除图层
    /// </summary>
    /// <param name="layer">图层信息</param>
    public void RemoveLayer(XrLayerInfo layer)
    {
        ArgumentNullException.ThrowIfNull(layer);

        _layers.Remove(layer);
    }

    /// <summary>
    /// 获取指定类型的图层列表
    /// </summary>
    /// <typeparam name="T">图层类型</typeparam>
    /// <returns>图层列表</returns>
    public IReadOnlyList<T> GetLayers<T>() where T : XrLayerInfo
    {
        return _layers.OfType<T>().ToList().AsReadOnly();
    }

    /// <summary>
    /// 合成所有图层并提交到 XR 运行时
    /// </summary>
    /// <param name="leftEyeTexture">左眼渲染结果纹理</param>
    /// <param name="rightEyeTexture">右眼渲染结果纹理</param>
    public void Composite(IResource? leftEyeTexture, IResource? rightEyeTexture)
    {
        ThrowIfDisposed();

        if (!Session.IsRunning)
        {
            return;
        }

        foreach (var layer in _layers)
        {
            if (!layer.IsEnabled)
            {
                continue;
            }
        }
    }

    /// <summary>
    /// 获取推荐的交换链纹理描述
    /// </summary>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>纹理描述</returns>
    public TextureDesc GetRecommendedSwapchainTextureDesc(uint width, uint height)
    {
        return new TextureDesc
        {
            Dimension = TextureDimension.Texture2DArray,
            Width = width,
            Height = height,
            ArrayLayers = 2,
            Format = ResourceFormat.R8G8B8A8Unorm,
            Usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource | TextureUsage.TransferDst,
            SampleCount = 1
        };
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 若已释放则抛出异常
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(XrCompositor));
        }
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放合成器资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _layers.Clear();
        _isDisposed = true;
    }

    #endregion
}
