namespace Gnosis.Asset.Format;

public interface ITextureFormat : IFormatHandler
{
    Task<TextureData> LoadTextureAsync(string path, CancellationToken cancellationToken = default);
    Task SaveTextureAsync(string path, TextureData texture, CancellationToken cancellationToken = default);
    Task<TextureData> ResizeAsync(TextureData texture, int width, int height, CancellationToken cancellationToken = default);
    Task<TextureData> CompressAsync(TextureData texture, TextureCompressionFormat format, CancellationToken cancellationToken = default);
}

public record TextureData
{
    public string Name { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public int Depth { get; init; } = 1;
    public int MipLevels { get; init; } = 1;
    public int ArrayLayers { get; init; } = 1;
    public TextureFormat Format { get; init; }
    public TextureDimension Dimension { get; init; }
    public byte[] RawData { get; init; } = [];
    public IReadOnlyList<byte[]> MipData { get; init; } = new List<byte[]>();
}

public enum TextureFormat
{
    Unknown = 0,
    R8G8B8A8_UNorm = 1,
    R8G8B8A8_SRGB = 2,
    B8G8R8A8_UNorm = 3,
    B8G8R8A8_SRGB = 4,
    R16G16B16A16_Float = 5,
    R32G32B32A32_Float = 6,
    R8_UNorm = 7,
    R16_Float = 8,
    R32_Float = 9,
    BC1_RGB_UNorm = 10,
    BC1_RGBA_UNorm = 11,
    BC2_UNorm = 12,
    BC3_UNorm = 13,
    BC4_UNorm = 14,
    BC5_UNorm = 15,
    BC6H_UFloat = 16,
    BC7_UNorm = 17,
    ASTC_4x4 = 18,
    ASTC_6x6 = 19,
    ASTC_8x8 = 20,
    ETC2_RGB = 21,
    ETC2_RGBA = 22,
    PVRTC_RGB_4BPP = 23,
    PVRTC_RGBA_4BPP = 24
}

public enum TextureDimension
{
    Unknown = 0,
    Texture1D = 1,
    Texture2D = 2,
    Texture3D = 3,
    TextureCube = 4,
    Texture2DArray = 5,
    TextureCubeArray = 6
}

public enum TextureCompressionFormat
{
    None = 0,
    BC1 = 1,
    BC3 = 2,
    BC4 = 3,
    BC5 = 4,
    BC6H = 5,
    BC7 = 6,
    ASTC_4x4 = 7,
    ASTC_6x6 = 8,
    ASTC_8x8 = 9,
    ETC2 = 10,
    PVRTC = 11
}
