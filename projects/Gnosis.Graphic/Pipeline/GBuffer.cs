using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Pipeline;

public sealed class GBuffer : IDisposable
{
    #region 字段

    private readonly IDevice _device;
    private bool _isDisposed;

    #endregion

    #region 属性

    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public IResource AlbedoTexture { get; private set; }
    public IResource NormalTexture { get; private set; }
    public IResource MaterialTexture { get; private set; }
    public IResource DepthTexture { get; private set; }

    #endregion

    #region 构造函数

    public GBuffer(IDevice device, uint width, uint height)
    {
        _device = device;
        Width = width;
        Height = height;

        AlbedoTexture = CreateTexture(ResourceFormat.R8G8B8A8Unorm, TextureUsage.ShaderResource | TextureUsage.RenderTarget);
        NormalTexture = CreateTexture(ResourceFormat.R16G16B16A16Float, TextureUsage.ShaderResource | TextureUsage.RenderTarget);
        MaterialTexture = CreateTexture(ResourceFormat.R8G8B8A8Unorm, TextureUsage.ShaderResource | TextureUsage.RenderTarget);
        DepthTexture = CreateTexture(ResourceFormat.D32FloatS8Uint, TextureUsage.DepthStencil);
    }

    #endregion

    #region 公开方法

    public void Resize(uint width, uint height)
    {
        if (width == Width && height == Height)
        {
            return;
        }

        Width = width;
        Height = height;

        AlbedoTexture.Dispose();
        NormalTexture.Dispose();
        MaterialTexture.Dispose();
        DepthTexture.Dispose();

        AlbedoTexture = CreateTexture(ResourceFormat.R8G8B8A8Unorm, TextureUsage.ShaderResource | TextureUsage.RenderTarget);
        NormalTexture = CreateTexture(ResourceFormat.R16G16B16A16Float, TextureUsage.ShaderResource | TextureUsage.RenderTarget);
        MaterialTexture = CreateTexture(ResourceFormat.R8G8B8A8Unorm, TextureUsage.ShaderResource | TextureUsage.RenderTarget);
        DepthTexture = CreateTexture(ResourceFormat.D32FloatS8Uint, TextureUsage.DepthStencil);
    }

    #endregion

    #region 私有方法

    private IResource CreateTexture(ResourceFormat format, TextureUsage usage)
    {
        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = Width,
            Height = Height,
            Depth = 1,
            Format = format,
            Usage = usage,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
        };

        return _device.CreateTexture(desc);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        AlbedoTexture.Dispose();
        NormalTexture.Dispose();
        MaterialTexture.Dispose();
        DepthTexture.Dispose();

        _isDisposed = true;
    }

    #endregion
}
