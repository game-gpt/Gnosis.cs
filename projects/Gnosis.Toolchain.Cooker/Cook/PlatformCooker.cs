using Gnosis.Asset.Bundle;
using Gnosis.Asset.Format;
using Gnosis.Platform;

namespace Gnosis.Toolchain.Cooker.Cook;

public sealed record CookOptions
{
    public Platform TargetPlatform { get; init; } = new Platform
    {
        OS = PlatformOS.Windows,
        ISA = PlatformISA.X64
    };
    public TextureCompressionFormat TextureCompression { get; init; } = TextureCompressionFormat.Bc7;
    public AudioEncodingFormat AudioEncoding { get; init; } = AudioEncodingFormat.Vorbis;
    public bool StripDebugInfo { get; init; } = true;
    public bool EnableCompression { get; init; } = true;
    public CompressionType BundleCompression { get; init; } = CompressionType.LZ4;
    public bool EnableEncryption { get; init; } = false;
    public byte[]? EncryptionKey { get; init; }
}

public enum TextureCompressionFormat
{
    None,
    Bc1,
    Bc3,
    Bc5,
    Bc7,
    Astc4x4,
    Astc6x6,
    Astc8x8,
    Etc2Rgb,
    Etc2Rgba
}

public enum AudioEncodingFormat
{
    Pcm,
    Adpcm,
    Vorbis,
    Mp3
}

public sealed record CookResult
{
    public bool Success { get; init; }
    public string AssetPath { get; init; } = string.Empty;
    public string? OutputPath { get; init; }
    public Platform TargetPlatform { get; init; } = null!;
    public string? ErrorMessage { get; init; }
}

public sealed class PlatformCooker
{
    private readonly IFormatRegistry _formatRegistry;

    public PlatformCooker(IFormatRegistry formatRegistry)
    {
        _formatRegistry = formatRegistry ?? throw new ArgumentNullException(nameof(formatRegistry));
    }

    /// <summary>
    /// 根据 ISA 推断默认纹理压缩格式
    /// X64 主机/桌面 → BC7，Arm64 移动端 → ASTC
    /// </summary>
    public TextureCompressionFormat GetDefaultTextureCompression(PlatformISA isa)
    {
        return isa switch
        {
            PlatformISA.X64 => TextureCompressionFormat.Bc7,
            PlatformISA.Arm64 => TextureCompressionFormat.Astc6x6,
            PlatformISA.Wasm => TextureCompressionFormat.Bc7,
            _ => TextureCompressionFormat.Bc7
        };
    }

    /// <summary>
    /// 根据 OS 推断默认纹理压缩格式（考虑主机平台特殊需求）
    /// </summary>
    public TextureCompressionFormat GetDefaultTextureCompression(PlatformOS os)
    {
        return os switch
        {
            PlatformOS.Windows or PlatformOS.Linux or PlatformOS.macOS
                => TextureCompressionFormat.Bc7,
            PlatformOS.Android or PlatformOS.iOS
                => TextureCompressionFormat.Astc6x6,
            PlatformOS.Web
                => TextureCompressionFormat.Bc7,
            PlatformOS.PlayStation or PlatformOS.Xbox
                => TextureCompressionFormat.Bc7,
            PlatformOS.Switch
                => TextureCompressionFormat.Astc4x4,
            _ => TextureCompressionFormat.Bc7
        };
    }

    /// <summary>
    /// 根据 OS 推断默认音频编码格式
    /// </summary>
    public AudioEncodingFormat GetDefaultAudioEncoding(PlatformOS os)
    {
        return os switch
        {
            PlatformOS.Android or PlatformOS.iOS
                => AudioEncodingFormat.Vorbis,
            PlatformOS.Web
                => AudioEncodingFormat.Mp3,
            _ => AudioEncodingFormat.Vorbis
        };
    }

    public async Task<CookResult> CookAssetAsync(string assetPath, string outputDirectory, CookOptions options, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(assetPath))
        {
            return new CookResult
            {
                Success = false,
                AssetPath = assetPath,
                TargetPlatform = options.TargetPlatform,
                ErrorMessage = $"资产文件不存在：{assetPath}"
            };
        }

        try
        {
            var handler = _formatRegistry.GetHandler(assetPath);
            if (handler is null)
            {
                return new CookResult
                {
                    Success = false,
                    AssetPath = assetPath,
                    TargetPlatform = options.TargetPlatform,
                    ErrorMessage = $"未找到资产格式处理器：{assetPath}"
                };
            }

            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var extension = GetPlatformExtension(assetPath);
            var outputPath = Path.Combine(outputDirectory, $"{fileName}{extension}");

            var data = await handler.ReadAsync(assetPath, cancellationToken);
            await handler.WriteAsync(outputPath, data, cancellationToken: cancellationToken);

            return new CookResult
            {
                Success = true,
                AssetPath = assetPath,
                OutputPath = outputPath,
                TargetPlatform = options.TargetPlatform
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            return new CookResult
            {
                Success = false,
                AssetPath = assetPath,
                TargetPlatform = options.TargetPlatform,
                ErrorMessage = $"资产烹饪 IO 错误：{ex.Message}"
            };
        }
    }

    public async Task<CookResult> CookAndPackSingleAsync(
        string assetPath,
        string bundleName,
        string outputDirectory,
        CookOptions options,
        CancellationToken cancellationToken = default)
    {
        var cookResult = await CookAssetAsync(assetPath, outputDirectory, options, cancellationToken);

        if (!cookResult.Success)
        {
            return cookResult;
        }

        return cookResult;
    }

    public async Task<AssetBundleResult> CookAndPackAsync(
        IEnumerable<string> assetPaths,
        string bundleName,
        string outputDirectory,
        CookOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var bundler = new AssetBundler(outputDirectory);
        var cookResults = new List<CookResult>();

        foreach (var assetPath in assetPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await CookAssetAsync(assetPath, outputDirectory, options, cancellationToken);
            cookResults.Add(result);

            if (result.Success && result.OutputPath is not null)
            {
                var virtualPath = Path.GetFileName(result.OutputPath);
                var compression = options.EnableCompression ? options.BundleCompression : CompressionType.None;
                bundler.AddAsset(result.OutputPath, virtualPath, compression);
            }
        }

        return bundler.Pack(bundleName, options.EncryptionKey);
    }

    private static string GetPlatformExtension(string assetPath)
    {
        var ext = Path.GetExtension(assetPath).ToLowerInvariant();

        return ext switch
        {
            ".png" or ".jpg" or ".jpeg" or ".ktx" or ".tga" or ".bmp" or ".hdr" => ".gnosis-texture",
            ".obj" or ".gltf" or ".glb" or ".fbx" => ".gnosis-mesh",
            ".wav" or ".mp3" or ".ogg" or ".flac" => ".gnosis-audio",
            ".gnosis-anim" or ".anim" => ".gnosis-anim",
            ".gnosis-mat" or ".mat" => ".gnosis-material",
            ".scene" => ".gnosis-scene",
            ".prefab" => ".gnosis-prefab",
            ".gg-shader" or ".shader" => ".gnosis-shader",
            ".gg-script" or ".script" => ".gnosis-script",
            _ => ext
        };
    }
}
