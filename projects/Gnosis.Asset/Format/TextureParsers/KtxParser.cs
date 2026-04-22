namespace Gnosis.Asset.Format.TextureParsers;

public sealed class KtxParser
{
    private static readonly byte[] Ktx1Magic = [0xAB, 0x4B, 0x54, 0x58, 0x20, 0x31, 0x31, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Ktx2Magic = [0xAB, 0x4B, 0x54, 0x58, 0x20, 0x32, 0x30, 0xBB, 0x0D, 0x0A, 0x1A, 0x0A];

    public KtxParseResult Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 12)
        {
            throw new InvalidDataException("KTX 文件过小");
        }

        var isKtx1 = data[..12].SequenceEqual(Ktx1Magic);
        var isKtx2 = data[..12].SequenceEqual(Ktx2Magic);

        if (!isKtx1 && !isKtx2)
        {
            throw new InvalidDataException("KTX 魔数无效");
        }

        return isKtx1 ? ParseKtx1(data) : ParseKtx2(data);
    }

    public static bool IsKtxFile(ReadOnlySpan<byte> header)
    {
        if (header.Length < 12)
        {
            return false;
        }

        return header[..12].SequenceEqual(Ktx1Magic) || header[..12].SequenceEqual(Ktx2Magic);
    }

    private static KtxParseResult ParseKtx1(ReadOnlySpan<byte> data)
    {
        if (data.Length < 64)
        {
            throw new InvalidDataException("KTX1 文件头不完整");
        }

        var endianness = BitConverter.ToUInt32(data[12..16]);
        if (endianness != 0x04030201)
        {
            throw new InvalidDataException($"KTX1 字节序不支持：0x{endianness:X8}");
        }

        var glType = BitConverter.ToUInt32(data[16..20]);
        var glTypeSize = BitConverter.ToUInt32(data[20..24]);
        var glFormat = BitConverter.ToUInt32(data[24..28]);
        var glInternalFormat = BitConverter.ToUInt32(data[28..32]);
        var glBaseInternalFormat = BitConverter.ToUInt32(data[32..36]);
        var pixelWidth = BitConverter.ToUInt32(data[36..40]);
        var pixelHeight = BitConverter.ToUInt32(data[40..44]);
        var pixelDepth = BitConverter.ToUInt32(data[44..48]);
        var numberOfArrayElements = BitConverter.ToUInt32(data[48..52]);
        var numberOfFaces = BitConverter.ToUInt32(data[52..56]);
        var numberOfMipmapLevels = BitConverter.ToUInt32(data[56..60]);
        var bytesOfKeyValueData = BitConverter.ToUInt32(data[60..64]);

        var width = (int)pixelWidth;
        var height = (int)pixelHeight;
        var depth = Math.Max(1, (int)pixelDepth);
        var arrayLayers = Math.Max(1, (int)numberOfArrayElements);
        var faces = Math.Max(1, (int)numberOfFaces);
        var mipLevels = Math.Max(1, (int)numberOfMipmapLevels);

        var textureFormat = MapGlInternalFormat(glInternalFormat);
        var dimension = DetermineDimension(pixelDepth, numberOfArrayElements, numberOfFaces);

        var offset = 64 + (int)bytesOfKeyValueData;
        var mipDataList = new List<byte[]>();

        for (var mip = 0; mip < mipLevels && offset < data.Length; mip++)
        {
            if (offset + 4 > data.Length)
            {
                break;
            }

            var imageSize = (int)BitConverter.ToUInt32(data[offset..(offset + 4)]);
            offset += 4;

            var mipWidth = Math.Max(1, width >> mip);
            var mipHeight = Math.Max(1, height >> mip);

            var totalFaceSize = 0;

            for (var face = 0; face < faces; face++)
            {
                if (offset + imageSize > data.Length)
                {
                    break;
                }

                if (mip == 0 && face == 0)
                {
                    var faceData = data[offset..(offset + imageSize)].ToArray();
                    mipDataList.Add(faceData);
                }

                totalFaceSize += imageSize;
                offset += imageSize;

                var cubePadding = (3 - ((imageSize + 3) % 4)) % 4;
                offset += cubePadding;
            }

            var mipPadding = (3 - ((totalFaceSize + 3) % 4)) % 4;
            offset += mipPadding;
        }

        var rawData = mipDataList.Count > 0 ? mipDataList[0] : [];

        return new KtxParseResult
        {
            Width = width,
            Height = height,
            Depth = depth,
            MipLevels = mipLevels,
            ArrayLayers = arrayLayers,
            Format = textureFormat,
            Dimension = dimension,
            RawData = rawData,
            MipData = mipDataList,
            GlInternalFormat = glInternalFormat
        };
    }

    private static KtxParseResult ParseKtx2(ReadOnlySpan<byte> data)
    {
        if (data.Length < 80)
        {
            throw new InvalidDataException("KTX2 文件头不完整");
        }

        var vkFormat = BitConverter.ToUInt32(data[12..16]);
        var typeSize = BitConverter.ToUInt32(data[16..20]);
        var pixelWidth = BitConverter.ToUInt32(data[20..24]);
        var pixelHeight = BitConverter.ToUInt32(data[24..28]);
        var pixelDepth = BitConverter.ToUInt32(data[28..32]);
        var layerCount = BitConverter.ToUInt32(data[32..36]);
        var faceCount = BitConverter.ToUInt32(data[36..40]);
        var levelCount = BitConverter.ToUInt32(data[40..44]);
        var supercompressionScheme = BitConverter.ToUInt32(data[44..48]);

        var dataFormatDescriptorOffset = BitConverter.ToUInt64(data[48..56]);
        var dataFormatDescriptorLength = BitConverter.ToUInt64(data[56..64]);
        var keyValueDataOffset = BitConverter.ToUInt64(data[64..72]);
        var keyValueDataLength = BitConverter.ToUInt64(data[72..80]);

        var width = (int)pixelWidth;
        var height = (int)pixelHeight;
        var depth = Math.Max(1, (int)pixelDepth);
        var arrayLayers = Math.Max(1, (int)layerCount);
        var faces = Math.Max(1, (int)faceCount);
        var mipLevels = Math.Max(1, (int)levelCount);

        var textureFormat = MapVkFormat(vkFormat);
        var dimension = DetermineDimension(pixelDepth, layerCount, faceCount);

        var levelOffset = 80;
        var mipDataList = new List<byte[]>();
        var rawData = Array.Empty<byte>();

        for (var level = 0; level < mipLevels && levelOffset + 24 <= data.Length; level++)
        {
            var byteOffset = BitConverter.ToUInt64(data[levelOffset..(levelOffset + 8)]);
            var byteLength = BitConverter.ToUInt64(data[(levelOffset + 8)..(levelOffset + 16)]);
            var uncompressedByteLength = BitConverter.ToUInt64(data[(levelOffset + 16)..(levelOffset + 24)]);

            if (byteOffset + byteLength <= (ulong)data.Length)
            {
                var levelData = data[(int)byteOffset..(int)(byteOffset + byteLength)].ToArray();

                if (level == 0)
                {
                    rawData = levelData;
                }

                mipDataList.Add(levelData);
            }

            levelOffset += 24;
        }

        return new KtxParseResult
        {
            Width = width,
            Height = height,
            Depth = depth,
            MipLevels = mipLevels,
            ArrayLayers = arrayLayers,
            Format = textureFormat,
            Dimension = dimension,
            RawData = rawData,
            MipData = mipDataList,
            VkFormat = vkFormat
        };
    }

    private static TextureFormat MapGlInternalFormat(uint glFormat)
    {
        return glFormat switch
        {
            0x8D94 => TextureFormat.ETC2_RGB,
            0x9278 => TextureFormat.ETC2_RGBA,
            0x93B0 => TextureFormat.ASTC_4x4,
            0x93B2 => TextureFormat.ASTC_6x6,
            0x93B4 => TextureFormat.ASTC_8x8,
            0x83F1 => TextureFormat.BC1_RGB_UNorm,
            0x83F2 => TextureFormat.BC1_RGBA_UNorm,
            0x83F3 => TextureFormat.BC2_UNorm,
            0x83F4 => TextureFormat.BC3_UNorm,
            0x8DBE => TextureFormat.BC4_UNorm,
            0x8DBF => TextureFormat.BC5_UNorm,
            0x8E8F => TextureFormat.BC6H_UFloat,
            0x8E8D => TextureFormat.BC7_UNorm,
            0x8058 => TextureFormat.R8G8B8A8_UNorm,
            0x8D62 => TextureFormat.R8G8B8A8_SRGB,
            0x8C3A => TextureFormat.R16G16B16A16_Float,
            0x8814 => TextureFormat.R32G32B32A32_Float,
            0x8229 => TextureFormat.R8_UNorm,
            0x822D => TextureFormat.R16_Float,
            0x822E => TextureFormat.R32_Float,
            0x8C00 => TextureFormat.PVRTC_RGB_4BPP,
            0x8C01 => TextureFormat.PVRTC_RGBA_4BPP,
            _ => TextureFormat.Unknown
        };
    }

    private static TextureFormat MapVkFormat(uint vkFormat)
    {
        return vkFormat switch
        {
            37 => TextureFormat.R8G8B8A8_UNorm,
            38 => TextureFormat.R8G8B8A8_SRGB,
            43 => TextureFormat.B8G8R8A8_UNorm,
            44 => TextureFormat.B8G8R8A8_SRGB,
            55 => TextureFormat.R16G16B16A16_Float,
            109 => TextureFormat.R32G32B32A32_Float,
            9 => TextureFormat.R8_UNorm,
            76 => TextureFormat.R16_Float,
            98 => TextureFormat.R32_Float,
            131 => TextureFormat.BC1_RGB_UNorm,
            132 => TextureFormat.BC1_RGBA_UNorm,
            134 => TextureFormat.BC2_UNorm,
            136 => TextureFormat.BC3_UNorm,
            139 => TextureFormat.BC4_UNorm,
            141 => TextureFormat.BC5_UNorm,
            145 => TextureFormat.BC6H_UFloat,
            147 => TextureFormat.BC7_UNorm,
            158 => TextureFormat.ETC2_RGB,
            159 => TextureFormat.ETC2_RGBA,
            164 => TextureFormat.ASTC_4x4,
            166 => TextureFormat.ASTC_6x6,
            168 => TextureFormat.ASTC_8x8,
            180 => TextureFormat.PVRTC_RGB_4BPP,
            181 => TextureFormat.PVRTC_RGBA_4BPP,
            _ => TextureFormat.Unknown
        };
    }

    private static TextureDimension DetermineDimension(uint depth, uint arrayLayers, uint faces)
    {
        if (faces == 6)
        {
            return arrayLayers > 1 ? TextureDimension.TextureCubeArray : TextureDimension.TextureCube;
        }

        if (depth > 0)
        {
            return TextureDimension.Texture3D;
        }

        if (arrayLayers > 1)
        {
            return TextureDimension.Texture2DArray;
        }

        return TextureDimension.Texture2D;
    }
}

public class KtxParseResult
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int Depth { get; init; } = 1;
    public int MipLevels { get; init; } = 1;
    public int ArrayLayers { get; init; } = 1;
    public TextureFormat Format { get; init; }
    public TextureDimension Dimension { get; init; }
    public byte[] RawData { get; init; } = [];
    public List<byte[]> MipData { get; init; } = [];
    public uint GlInternalFormat { get; init; }
    public uint VkFormat { get; init; }
}
