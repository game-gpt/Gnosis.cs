using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Texture;

public sealed class TextureLoader
{
    #region 字段

    private readonly IDevice _device;

    #endregion

    #region 构造函数

    public TextureLoader(IDevice device)
    {
        _device = device;
    }

    #endregion

    #region 加载方法

    public IResource LoadFromRawRgba(byte[] rgbaData, int width, int height)
    {
        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = (uint)width,
            Height = (uint)height,
            Depth = 1,
            Format = ResourceFormat.R8G8B8A8Unorm,
            Usage = TextureUsage.ShaderResource,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
        };

        return _device.CreateTexture(desc);
    }

    public IResource LoadFromRawBgra(byte[] bgraData, int width, int height)
    {
        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = (uint)width,
            Height = (uint)height,
            Depth = 1,
            Format = ResourceFormat.B8G8R8A8Unorm,
            Usage = TextureUsage.ShaderResource,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
        };

        return _device.CreateTexture(desc);
    }

    public IResource CreateSolidColor(byte r, byte g, byte b, byte a, int width = 1, int height = 1)
    {
        var data = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            data[i * 4] = r;
            data[i * 4 + 1] = g;
            data[i * 4 + 2] = b;
            data[i * 4 + 3] = a;
        }

        return LoadFromRawRgba(data, width, height);
    }

    public IResource CreateTileTexture(byte r, byte g, byte b, int tileSize = 4)
    {
        var data = new byte[tileSize * tileSize * 4];
        for (var i = 0; i < tileSize * tileSize; i++)
        {
            data[i * 4] = r;
            data[i * 4 + 1] = g;
            data[i * 4 + 2] = b;
            data[i * 4 + 3] = 255;
        }

        return LoadFromRawRgba(data, tileSize, tileSize);
    }

    #endregion
}
