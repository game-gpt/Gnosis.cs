using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Gnosis.Asset.Format.AstcCompression;
using Gnosis.Asset.Format.BcCompression;
using Gnosis.Asset.Format.EtcCompression;
using Oak.Ktx;

namespace Gnosis.Asset.Format;

/// <summary>
/// 纹理重采样过滤器类型
/// </summary>
public enum ResizeFilter
{
    /// <summary>
    /// 最近邻采样，速度快但质量低
    /// </summary>
    Nearest,

    /// <summary>
    /// 双线性采样，平衡速度与质量
    /// </summary>
    Bilinear,

    /// <summary>
    /// Lanczos3 采样，质量最高但速度慢
    /// </summary>
    Lanczos3
}

/// <summary>
/// 纹理格式处理器，支持图像加载、保存、缩放和 BC 压缩
/// </summary>
public class TextureFormatHandler : FormatHandlerBase, ITextureFormat
{
    #region 常量

    private static readonly byte[] PngMagic = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] JpegMagic = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] BmpMagic = [0x42, 0x4D];
    private static readonly byte[] DdsMagic = [0x44, 0x44, 0x53, 0x20];
    private static readonly byte[] KtxMagic = [0xAB, 0x4B, 0x54, 0x58];

    private const string EngineExtension = ".gnosis-texture";

    private static readonly HashSet<string> ImageSharpSupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tga", ".tiff", ".tif", ".webp"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    #endregion

    #region 属性

    public override FormatType SupportedFormat => FormatType.Texture;

    #endregion

    #region 构造函数

    public TextureFormatHandler() { }

    public TextureFormatHandler(IFileIO fileIO) : base(fileIO) { }

    #endregion

    #region 加载纹理

    /// <summary>
    /// 从文件加载纹理，支持引擎格式、标准图像格式和 KTX 格式
    /// </summary>
    public async Task<TextureData> LoadTextureAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!FileIO.Exists(path))
        {
            throw new FileNotFoundException($"未找到纹理文件：{path}");
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension == EngineExtension)
        {
            return await LoadEngineFormatAsync(path, cancellationToken);
        }

        if (extension is ".ktx" or ".ktx2")
        {
            return await LoadKtxFormatAsync(path, cancellationToken);
        }

        if (extension == ".dds")
        {
            throw new NotSupportedException("DDS 格式暂不支持加载，建议转换为 KTX 或标准图像格式");
        }

        if (extension is ".hdr" or ".exr")
        {
            throw new NotSupportedException("HDR/EXR 格式暂不支持加载，建议转换为 KTX 格式");
        }

        if (ImageSharpSupportedExtensions.Contains(extension))
        {
            return await LoadImageFormatAsync(path, cancellationToken);
        }

        throw new NotSupportedException($"不支持的纹理格式：{extension}");
    }

    /// <summary>
    /// 加载引擎格式纹理（JSON 序列化）
    /// </summary>
    private async Task<TextureData> LoadEngineFormatAsync(string path, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<TextureData>(data)
            ?? throw new InvalidDataException($"纹理反序列化失败：{path}");
    }

    /// <summary>
    /// 使用 ImageSharp 加载标准图像格式
    /// </summary>
    private async Task<TextureData> LoadImageFormatAsync(string path, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            using var image = Image.Load<Rgba32>(path);
            var rawData = ImageToRgbaBytes(image);

            return new TextureData
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Width = image.Width,
                Height = image.Height,
                Depth = 1,
                MipLevels = 1,
                ArrayLayers = 1,
                Format = TextureFormat.R8G8B8A8_UNorm,
                Dimension = TextureDimension.Texture2D,
                RawData = rawData
            };
        }, cancellationToken);
    }

    /// <summary>
    /// 加载 KTX/KTX2 格式纹理
    /// </summary>
    private async Task<TextureData> LoadKtxFormatAsync(string path, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(path, cancellationToken);
        var fileName = Path.GetFileNameWithoutExtension(path);

        var parser = new KtxParser();
        var result = parser.Parse(data);

        return new TextureData
        {
            Name = fileName,
            Width = result.Width,
            Height = result.Height,
            Depth = result.Depth,
            MipLevels = result.MipLevels,
            ArrayLayers = result.ArrayLayers,
            Format = result.Format,
            Dimension = result.Dimension,
            RawData = result.RawData,
            MipData = result.MipData
        };
    }

    #endregion

    #region 保存纹理

    /// <summary>
    /// 保存纹理到文件，支持引擎格式和标准图像格式
    /// </summary>
    public async Task SaveTextureAsync(string path, TextureData texture, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension == EngineExtension)
        {
            await SaveEngineFormatAsync(path, texture, cancellationToken);
            return;
        }

        if (ImageSharpSupportedExtensions.Contains(extension))
        {
            await SaveImageFormatAsync(path, texture, cancellationToken);
            return;
        }

        throw new NotSupportedException($"不支持的保存格式：{extension}");
    }

    /// <summary>
    /// 保存为引擎格式（JSON 序列化）
    /// </summary>
    private async Task SaveEngineFormatAsync(string path, TextureData texture, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(texture, JsonOptions);
        await WriteAsync(path, data, null, cancellationToken);
    }

    /// <summary>
    /// 使用 ImageSharp 保存为标准图像格式
    /// </summary>
    private async Task SaveImageFormatAsync(string path, TextureData texture, CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            using var image = RgbaBytesToImage(texture.RawData, texture.Width, texture.Height);
            image.Save(path);
        }, cancellationToken);
    }

    #endregion

    #region 调整大小

    /// <summary>
    /// 调整纹理大小，默认使用 Lanczos3 采样器
    /// </summary>
    public Task<TextureData> ResizeAsync(TextureData texture, int width, int height, CancellationToken cancellationToken = default)
    {
        return ResizeAsync(texture, width, height, ResizeFilter.Lanczos3, cancellationToken);
    }

    /// <summary>
    /// 调整纹理大小，使用指定的采样过滤器
    /// </summary>
    public async Task<TextureData> ResizeAsync(TextureData texture, int width, int height, ResizeFilter filter, CancellationToken cancellationToken = default)
    {
        if (texture.Format != TextureFormat.R8G8B8A8_UNorm)
        {
            throw new NotSupportedException($"仅支持 R8G8B8A8_UNorm 格式的纹理缩放，当前格式：{texture.Format}");
        }

        if (texture.RawData.Length == 0)
        {
            throw new ArgumentException("纹理数据不能为空");
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException($"目标尺寸无效：{width}x{height}");
        }

        return await Task.Run(() =>
        {
            using var image = RgbaBytesToImage(texture.RawData, texture.Width, texture.Height);
            var sampler = GetResampler(filter);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Sampler = sampler
            }));

            var resizedData = ImageToRgbaBytes(image);

            return texture with
            {
                Width = width,
                Height = height,
                RawData = resizedData
            };
        }, cancellationToken);
    }

    /// <summary>
    /// 获取 ImageSharp 采样器实例
    /// </summary>
    private static IResampler GetResampler(ResizeFilter filter)
    {
        return filter switch
        {
            ResizeFilter.Nearest => KnownResamplers.NearestNeighbor,
            ResizeFilter.Bilinear => KnownResamplers.Triangle,
            ResizeFilter.Lanczos3 => KnownResamplers.Lanczos3,
            _ => KnownResamplers.Lanczos3
        };
    }

    #endregion

    #region 压缩纹理

    /// <summary>
    /// 压缩纹理数据为指定的 BC 压缩格式
    /// </summary>
    public async Task<TextureData> CompressAsync(TextureData texture, TextureCompressionFormat format, CancellationToken cancellationToken = default)
    {
        if (format == TextureCompressionFormat.None)
        {
            return texture with { };
        }

        if (texture.Format != TextureFormat.R8G8B8A8_UNorm)
        {
            throw new NotSupportedException($"仅支持 R8G8B8A8_UNorm 格式的纹理压缩，当前格式：{texture.Format}");
        }

        if (texture.RawData.Length == 0)
        {
            throw new ArgumentException("纹理数据不能为空");
        }

        return await Task.Run(() =>
        {
            var compressedData = format switch
            {
                TextureCompressionFormat.BC1 => BcCompressor.CompressBc1(texture.RawData, texture.Width, texture.Height),
                TextureCompressionFormat.BC3 => BcCompressor.CompressBc3(texture.RawData, texture.Width, texture.Height),
                TextureCompressionFormat.BC4 => BcCompressor.CompressBc4(texture.RawData, texture.Width, texture.Height),
                TextureCompressionFormat.BC5 => BcCompressor.CompressBc5(texture.RawData, texture.Width, texture.Height),
                TextureCompressionFormat.BC7 => BcCompressor.CompressBc7(texture.RawData, texture.Width, texture.Height),
                TextureCompressionFormat.ASTC_4x4 => AstcCompressor.Compress4x4(texture.RawData, texture.Width, texture.Height),
                TextureCompressionFormat.ASTC_6x6 => AstcCompressor.Compress6x6(texture.RawData, texture.Width, texture.Height),
                TextureCompressionFormat.ASTC_8x8 => AstcCompressor.Compress8x8(texture.RawData, texture.Width, texture.Height),
                TextureCompressionFormat.ETC2 => HasAlphaChannel(texture.RawData)
                    ? EtcCompressor.CompressEtc2Rgba(texture.RawData, texture.Width, texture.Height)
                    : EtcCompressor.CompressEtc2Rgb(texture.RawData, texture.Width, texture.Height),
                _ => throw new NotSupportedException($"不支持的压缩格式：{format}")
            };

            var textureFormat = MapCompressionToTextureFormat(format, texture.RawData);

            return texture with
            {
                Format = textureFormat,
                RawData = compressedData
            };
        }, cancellationToken);
    }

    /// <summary>
    /// 将压缩格式映射为纹理格式
    /// </summary>
    private static TextureFormat MapCompressionToTextureFormat(TextureCompressionFormat compressionFormat, byte[] rgbaData)
    {
        return compressionFormat switch
        {
            TextureCompressionFormat.BC1 => HasAlphaChannel(rgbaData)
                ? TextureFormat.BC1_RGBA_UNorm
                : TextureFormat.BC1_RGB_UNorm,
            TextureCompressionFormat.BC3 => TextureFormat.BC3_UNorm,
            TextureCompressionFormat.BC4 => TextureFormat.BC4_UNorm,
            TextureCompressionFormat.BC5 => TextureFormat.BC5_UNorm,
            TextureCompressionFormat.BC7 => TextureFormat.BC7_UNorm,
            TextureCompressionFormat.ASTC_4x4 => TextureFormat.ASTC_4x4,
            TextureCompressionFormat.ASTC_6x6 => TextureFormat.ASTC_6x6,
            TextureCompressionFormat.ASTC_8x8 => TextureFormat.ASTC_8x8,
            TextureCompressionFormat.ETC2 => HasAlphaChannel(rgbaData)
                ? TextureFormat.ETC2_RGBA
                : TextureFormat.ETC2_RGB,
            _ => TextureFormat.Unknown
        };
    }

    #endregion

    #region 验证文件

    /// <summary>
    /// 验证文件格式，检查文件魔数是否匹配
    /// </summary>
    public override async Task<bool> ValidateAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!FileIO.Exists(path))
        {
            return false;
        }

        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension is ".ktx" or ".ktx2")
        {
            return await ValidateKtxAsync(path, cancellationToken);
        }

        var magicBytes = GetMagicBytesForExtension(extension);

        if (magicBytes == null)
        {
            return true;
        }

        try
        {
            var data = await ReadAsync(path, cancellationToken);
            if (data.Length < magicBytes.Length)
            {
                return false;
            }

            for (int i = 0; i < magicBytes.Length; i++)
            {
                if (data[i] != magicBytes[i])
                {
                    return false;
                }
            }

            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// 验证 KTX 文件格式
    /// </summary>
    private async Task<bool> ValidateKtxAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var data = await ReadAsync(path, cancellationToken);

            if (data.Length < 12)
            {
                return false;
            }

            return KtxParser.IsKtxFile(data);
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// 获取文件扩展名对应的魔数字节
    /// </summary>
    private static byte[]? GetMagicBytesForExtension(string extension)
    {
        return extension switch
        {
            ".png" => PngMagic,
            ".jpg" or ".jpeg" => JpegMagic,
            ".bmp" => BmpMagic,
            ".dds" => DdsMagic,
            ".ktx" => KtxMagic,
            _ => null
        };
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 将 ImageSharp 图像转换为 RGBA 字节数组
    /// </summary>
    private static byte[] ImageToRgbaBytes(Image<Rgba32> image)
    {
        var rawData = new byte[image.Width * image.Height * 4];

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                int offset = (y * image.Width + x) * 4;
                rawData[offset] = pixel.R;
                rawData[offset + 1] = pixel.G;
                rawData[offset + 2] = pixel.B;
                rawData[offset + 3] = pixel.A;
            }
        }

        return rawData;
    }

    /// <summary>
    /// 将 RGBA 字节数组转换为 ImageSharp 图像
    /// </summary>
    private static Image<Rgba32> RgbaBytesToImage(byte[] rgbaData, int width, int height)
    {
        return Image.LoadPixelData<Rgba32>(rgbaData, width, height);
    }

    /// <summary>
    /// 检查 RGBA 数据中是否包含非不透明 Alpha 通道
    /// </summary>
    private static bool HasAlphaChannel(byte[] rgbaData)
    {
        for (int i = 3; i < rgbaData.Length; i += 4)
        {
            if (rgbaData[i] < 255)
            {
                return true;
            }
        }

        return false;
    }

    protected override IReadOnlyList<string> GetSupportedExtensions()
    {
        return new List<string>
        {
            EngineExtension, ".png", ".jpg", ".jpeg", ".tga", ".bmp",
            ".hdr", ".exr", ".dds", ".ktx", ".ktx2", ".scirpttexture"
        };
    }

    #endregion
}
