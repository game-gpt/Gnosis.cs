using Gnosis.Asset.Bundle;
using Gnosis.Asset.Format;
using Gnosis.Core.Platform;

namespace Gnosis.Toolchain.Cooker.Cook;

public sealed record CookOptions
{
    public PlatformType TargetPlatform { get; init; } = PlatformType.Windows;
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
    public PlatformType TargetPlatform { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class PlatformCooker
{
    private readonly IFormatRegistry _formatRegistry;

    public PlatformCooker(IFormatRegistry formatRegistry)
    {
        _formatRegistry = formatRegistry ?? throw new ArgumentNullException(nameof(formatRegistry));
    }

    public TextureCompressionFormat GetDefaultTextureCompression(PlatformType platform)
    {
        return platform switch
        {
            PlatformType.Windows or PlatformType.Linux or PlatformType.PlayStation or PlatformType.Xbox
                => TextureCompressionFormat.Bc7,
            PlatformType.macOS
                => TextureCompressionFormat.Bc7,
            PlatformType.Android
                => TextureCompressionFormat.Astc6x6,
            PlatformType.iOS
                => TextureCompressionFormat.Astc6x6,
            PlatformType.WebAssembly
                => TextureCompressionFormat.Bc7,
            PlatformType.Switch
                => TextureCompressionFormat.Astc4x4,
            _ => TextureCompressionFormat.Bc7
        };
    }

    public AudioEncodingFormat GetDefaultAudioEncoding(PlatformType platform)
    {
        return platform switch
        {
            PlatformType.Android or PlatformType.iOS
                => AudioEncodingFormat.Vorbis,
            PlatformType.WebAssembly
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
