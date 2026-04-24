namespace Gnosis.Asset.Format.BcCompression;

/// <summary>
/// BC 块压缩算法，提供 BC1/BC3/BC4/BC5/BC7 压缩功能
/// </summary>
public static class BcCompressor
{
    #region BC1 压缩

    /// <summary>
    /// 将 RGBA 数据压缩为 BC1（DXT1）格式，每 4x4 像素块输出 8 字节
    /// </summary>
    public static byte[] CompressBc1(byte[] rgbaData, int width, int height)
    {
        ValidateInput(rgbaData, width, height);

        int blocksX = (width + 3) / 4;
        int blocksY = (height + 3) / 4;
        byte[] output = new byte[blocksX * blocksY * 8];

        Span<byte> block = stackalloc byte[64];
        Span<byte> encoded = stackalloc byte[8];

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                ExtractBlock(rgbaData, width, height, bx, by, block);
                BcBlockEncoder.EncodeBc1ColorBlock(block, encoded);

                int offset = (by * blocksX + bx) * 8;
                encoded.CopyTo(output.AsSpan(offset, 8));
            }
        }

        return output;
    }

    #endregion

    #region BC3 压缩

    /// <summary>
    /// 将 RGBA 数据压缩为 BC3（DXT5）格式，每 4x4 像素块输出 16 字节
    /// </summary>
    public static byte[] CompressBc3(byte[] rgbaData, int width, int height)
    {
        ValidateInput(rgbaData, width, height);

        int blocksX = (width + 3) / 4;
        int blocksY = (height + 3) / 4;
        byte[] output = new byte[blocksX * blocksY * 16];

        Span<byte> block = stackalloc byte[64];
        Span<byte> alphaBlock = stackalloc byte[16];
        Span<byte> colorEncoded = stackalloc byte[8];
        Span<byte> alphaEncoded = stackalloc byte[8];

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                ExtractBlock(rgbaData, width, height, bx, by, block);

                for (int i = 0; i < 16; i++)
                {
                    alphaBlock[i] = block[i * 4 + 3];
                }

                BcBlockEncoder.EncodeBc1ColorBlock(block, colorEncoded, forceFourColorMode: true);
                BcBlockEncoder.EncodeAlphaBlock(alphaBlock, alphaEncoded);

                int offset = (by * blocksX + bx) * 16;
                alphaEncoded.CopyTo(output.AsSpan(offset, 8));
                colorEncoded.CopyTo(output.AsSpan(offset + 8, 8));
            }
        }

        return output;
    }

    #endregion

    #region BC4 压缩

    /// <summary>
    /// 将 RGBA 数据的红色通道压缩为 BC4 格式，每 4x4 像素块输出 8 字节
    /// </summary>
    public static byte[] CompressBc4(byte[] rgbaData, int width, int height)
    {
        ValidateInput(rgbaData, width, height);

        int blocksX = (width + 3) / 4;
        int blocksY = (height + 3) / 4;
        byte[] output = new byte[blocksX * blocksY * 8];

        Span<byte> block = stackalloc byte[64];
        Span<byte> redBlock = stackalloc byte[16];
        Span<byte> encoded = stackalloc byte[8];

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                ExtractBlock(rgbaData, width, height, bx, by, block);

                for (int i = 0; i < 16; i++)
                {
                    redBlock[i] = block[i * 4];
                }

                BcBlockEncoder.EncodeAlphaBlock(redBlock, encoded);

                int offset = (by * blocksX + bx) * 8;
                encoded.CopyTo(output.AsSpan(offset, 8));
            }
        }

        return output;
    }

    #endregion

    #region BC5 压缩

    /// <summary>
    /// 将 RGBA 数据的红绿通道压缩为 BC5 格式，每 4x4 像素块输出 16 字节
    /// </summary>
    public static byte[] CompressBc5(byte[] rgbaData, int width, int height)
    {
        ValidateInput(rgbaData, width, height);

        int blocksX = (width + 3) / 4;
        int blocksY = (height + 3) / 4;
        byte[] output = new byte[blocksX * blocksY * 16];

        Span<byte> block = stackalloc byte[64];
        Span<byte> redBlock = stackalloc byte[16];
        Span<byte> greenBlock = stackalloc byte[16];
        Span<byte> redEncoded = stackalloc byte[8];
        Span<byte> greenEncoded = stackalloc byte[8];

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                ExtractBlock(rgbaData, width, height, bx, by, block);

                for (int i = 0; i < 16; i++)
                {
                    redBlock[i] = block[i * 4];
                    greenBlock[i] = block[i * 4 + 1];
                }

                BcBlockEncoder.EncodeAlphaBlock(redBlock, redEncoded);
                BcBlockEncoder.EncodeAlphaBlock(greenBlock, greenEncoded);

                int offset = (by * blocksX + bx) * 16;
                redEncoded.CopyTo(output.AsSpan(offset, 8));
                greenEncoded.CopyTo(output.AsSpan(offset + 8, 8));
            }
        }

        return output;
    }

    #endregion

    #region BC7 压缩

    /// <summary>
    /// 将 RGBA 数据压缩为 BC7 格式（多模式择优：Mode 5 + Mode 6），每 4x4 像素块输出 16 字节
    /// </summary>
    public static byte[] CompressBc7(byte[] rgbaData, int width, int height)
    {
        ValidateInput(rgbaData, width, height);

        int blocksX = (width + 3) / 4;
        int blocksY = (height + 3) / 4;
        byte[] output = new byte[blocksX * blocksY * 16];

        Span<byte> block = stackalloc byte[64];
        Span<byte> encoded = stackalloc byte[16];

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                ExtractBlock(rgbaData, width, height, bx, by, block);
                Bc7Encoder.EncodeBc7Block(block, encoded);

                int offset = (by * blocksX + bx) * 16;
                encoded.CopyTo(output.AsSpan(offset, 16));
            }
        }

        return output;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 从 RGBA 数据中提取 4x4 像素块，不足部分用边缘像素填充
    /// </summary>
    private static void ExtractBlock(byte[] rgbaData, int width, int height, int bx, int by, Span<byte> block)
    {
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                int px = Math.Min(bx * 4 + x, width - 1);
                int py = Math.Min(by * 4 + y, height - 1);
                int srcOffset = (py * width + px) * 4;
                int dstOffset = (y * 4 + x) * 4;

                block[dstOffset] = rgbaData[srcOffset];
                block[dstOffset + 1] = rgbaData[srcOffset + 1];
                block[dstOffset + 2] = rgbaData[srcOffset + 2];
                block[dstOffset + 3] = rgbaData[srcOffset + 3];
            }
        }
    }

    /// <summary>
    /// 验证输入数据的有效性
    /// </summary>
    private static void ValidateInput(byte[] rgbaData, int width, int height)
    {
        if (rgbaData == null || rgbaData.Length == 0)
        {
            throw new ArgumentException("RGBA 数据不能为空");
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException($"纹理尺寸无效：{width}x{height}");
        }

        int expectedSize = width * height * 4;
        if (rgbaData.Length < expectedSize)
        {
            throw new ArgumentException($"RGBA 数据长度不足：期望 {expectedSize} 字节，实际 {rgbaData.Length} 字节");
        }
    }

    #endregion
}
