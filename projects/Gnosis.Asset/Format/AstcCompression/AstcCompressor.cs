namespace Gnosis.Asset.Format.AstcCompression;

public static class AstcCompressor
{
    #region 公开方法

    /// <summary>
    /// 将 RGBA 数据压缩为 ASTC 4x4 格式，每 4x4 像素块输出 16 字节
    /// </summary>
    public static byte[] Compress4x4(byte[] rgbaData, int width, int height)
    {
        return Compress(rgbaData, width, height, 4, 4);
    }

    /// <summary>
    /// 将 RGBA 数据压缩为 ASTC 6x6 格式，每 6x6 像素块输出 16 字节
    /// </summary>
    public static byte[] Compress6x6(byte[] rgbaData, int width, int height)
    {
        return Compress(rgbaData, width, height, 6, 6);
    }

    /// <summary>
    /// 将 RGBA 数据压缩为 ASTC 8x8 格式，每 8x8 像素块输出 16 字节
    /// </summary>
    public static byte[] Compress8x8(byte[] rgbaData, int width, int height)
    {
        return Compress(rgbaData, width, height, 8, 8);
    }

    /// <summary>
    /// 将 RGBA 数据压缩为指定块大小的 ASTC 格式
    /// </summary>
    public static byte[] Compress(byte[] rgbaData, int width, int height, int blockWidth, int blockHeight)
    {
        ValidateInput(rgbaData, width, height);

        int blocksX = (width + blockWidth - 1) / blockWidth;
        int blocksY = (height + blockHeight - 1) / blockHeight;

        byte[] output = new byte[blocksX * blocksY * 16];

        Span<byte> block = stackalloc byte[blockWidth * blockHeight * 4];
        Span<byte> encoded = stackalloc byte[16];

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                ExtractBlock(rgbaData, width, height, bx, by, blockWidth, blockHeight, block);
                EncodeAstcBlock(block, blockWidth, blockHeight, encoded);

                int offset = (by * blocksX + bx) * 16;
                encoded.CopyTo(output.AsSpan(offset, 16));
            }
        }

        return output;
    }

    #endregion

    #region ASTC 块编码

    /// <summary>
    /// 编码 ASTC 块，使用简化的颜色端点 + 权重网格方案
    /// </summary>
    private static void EncodeAstcBlock(ReadOnlySpan<byte> block, int blockWidth, int blockHeight, Span<byte> output)
    {
        int texelCount = blockWidth * blockHeight;

        var minColor = new byte[] { 255, 255, 255, 255 };
        var maxColor = new byte[] { 0, 0, 0, 0 };

        for (int i = 0; i < texelCount; i++)
        {
            int srcIdx = i * 4;

            if (srcIdx + 3 >= block.Length)
            {
                break;
            }

            for (int c = 0; c < 4; c++)
            {
                if (block[srcIdx + c] < minColor[c]) minColor[c] = block[srcIdx + c];
                if (block[srcIdx + c] > maxColor[c]) maxColor[c] = block[srcIdx + c];
            }
        }

        bool isVoidExtent = true;

        for (int c = 0; c < 4; c++)
        {
            if (minColor[c] != maxColor[c])
            {
                isVoidExtent = false;
                break;
            }
        }

        if (isVoidExtent)
        {
            EncodeVoidExtentBlock(minColor, output);
            return;
        }

        EncodeWeightBlock(block, texelCount, minColor, maxColor, blockWidth, blockHeight, output);
    }

    /// <summary>
    /// 编码 void-extent 块（所有纹素相同颜色）
    /// </summary>
    private static void EncodeVoidExtentBlock(byte[] color, Span<byte> output)
    {
        output[0] = 0xFC;

        for (int i = 1; i < 6; i++)
        {
            output[i] = 0xFF;
        }

        ushort r = ScaleTo16(color[0]);
        ushort g = ScaleTo16(color[1]);
        ushort b = ScaleTo16(color[2]);
        ushort a = ScaleTo16(color[3]);

        output[6] = (byte)(r & 0xFF);
        output[7] = (byte)((r >> 8) | (g << 4) & 0xF0);
        output[8] = (byte)((g >> 4) & 0xFF);
        output[9] = (byte)(b & 0xFF);
        output[10] = (byte)((b >> 8) | (a << 4) & 0xF0);
        output[11] = (byte)((a >> 4) & 0xFF);

        for (int i = 12; i < 16; i++)
        {
            output[i] = 0;
        }
    }

    /// <summary>
    /// 编码权重块（双端点插值）
    /// </summary>
    private static void EncodeWeightBlock(ReadOnlySpan<byte> block, int texelCount, byte[] minColor, byte[] maxColor, int blockWidth, int blockHeight, Span<byte> output)
    {
        int weightBits = texelCount <= 4 ? 2 : (texelCount <= 16 ? 2 : 1);
        int weightMax = (1 << weightBits) - 1;

        int weightsSize = (texelCount * weightBits + 7) / 8;

        int headerBits = 11;
        int endpointBits = 32;
        int extraBits = 0;
        int totalBits = headerBits + endpointBits + extraBits + weightsSize * 8;

        if (totalBits > 128)
        {
            weightBits = 1;
            weightMax = 1;
            weightsSize = (texelCount + 7) / 8;
        }

        output.Clear();

        int bitPos = 0;

        WriteBits(output, 0b011, 3, ref bitPos);

        int weightRange = weightBits == 2 ? 5 : 1;
        WriteBits(output, (uint)weightRange, 4, ref bitPos);

        WriteBits(output, (uint)(blockWidth - 4), 3, ref bitPos);
        WriteBits(output, (uint)(blockHeight - 4), ref bitPos, 3);

        WriteBits(output, 0, 2, ref bitPos);

        WriteBits(output, 0, 4, ref bitPos);

        WriteBits(output, 1, 2, ref bitPos);

        WriteBits(output, 0, ref bitPos, 1);

        WriteEndpointColor(output, maxColor, ref bitPos);
        WriteEndpointColor(output, minColor, ref bitPos);

        for (int i = 0; i < texelCount; i++)
        {
            int srcIdx = i * 4;

            if (srcIdx + 3 >= block.Length)
            {
                WriteBits(output, 0, weightBits, ref bitPos);
                continue;
            }

            int weight = ComputeWeight(block, srcIdx, minColor, maxColor, weightMax);
            WriteBits(output, (uint)weight, weightBits, ref bitPos);
        }
    }

    /// <summary>
    /// 计算单个纹素的插值权重
    /// </summary>
    private static int ComputeWeight(ReadOnlySpan<byte> block, int srcIdx, byte[] minColor, byte[] maxColor, int weightMax)
    {
        int distMin = 0;
        int distMax = 0;

        for (int c = 0; c < 4; c++)
        {
            int diff = maxColor[c] - minColor[c];
            int dist = block[srcIdx + c] - minColor[c];
            distMin += dist * dist;
            distMax += diff * diff;
        }

        if (distMax == 0)
        {
            return 0;
        }

        int weight = (int)((long)distMin * weightMax / distMax);
        return Math.Clamp(weight, 0, weightMax);
    }

    /// <summary>
    /// 写入端点颜色（8-bit RGBA）
    /// </summary>
    private static void WriteEndpointColor(Span<byte> output, byte[] color, ref int bitPos)
    {
        WriteBits(output, color[0], 8, ref bitPos);
        WriteBits(output, color[1], 8, ref bitPos);
        WriteBits(output, color[2], 8, ref bitPos);
        WriteBits(output, color[3], 8, ref bitPos);
    }

    #endregion

    #region 位操作

    private static void WriteBits(Span<byte> output, uint value, int bitCount, ref int bitPos)
    {
        for (int i = 0; i < bitCount; i++)
        {
            int byteIdx = (bitPos + i) / 8;
            int bitIdx = (bitPos + i) % 8;

            if (byteIdx >= output.Length)
            {
                break;
            }

            if (((value >> i) & 1) != 0)
            {
                output[byteIdx] |= (byte)(1 << bitIdx);
            }
        }

        bitPos += bitCount;
    }

    private static void WriteBits(Span<byte> output, uint value, ref int bitPos, int bitCount)
    {
        WriteBits(output, value, bitCount, ref bitPos);
    }

    #endregion

    #region 辅助方法

    private static ushort ScaleTo16(byte v)
    {
        return (ushort)(v | (v << 8));
    }

    private static void ExtractBlock(byte[] rgbaData, int width, int height, int bx, int by, int blockWidth, int blockHeight, Span<byte> block)
    {
        int startX = bx * blockWidth;
        int startY = by * blockHeight;

        for (int y = 0; y < blockHeight; y++)
        {
            for (int x = 0; x < blockWidth; x++)
            {
                int px = Math.Min(startX + x, width - 1);
                int py = Math.Min(startY + y, height - 1);
                int srcIdx = (py * width + px) * 4;
                int dstIdx = (y * blockWidth + x) * 4;

                block[dstIdx] = rgbaData[srcIdx];
                block[dstIdx + 1] = rgbaData[srcIdx + 1];
                block[dstIdx + 2] = rgbaData[srcIdx + 2];
                block[dstIdx + 3] = rgbaData[srcIdx + 3];
            }
        }
    }

    private static void ValidateInput(byte[] rgbaData, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(rgbaData);

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException($"图像尺寸无效：{width}x{height}");
        }

        int expectedSize = width * height * 4;

        if (rgbaData.Length < expectedSize)
        {
            throw new ArgumentException($"数据长度不足：期望 {expectedSize} 字节，实际 {rgbaData.Length} 字节");
        }
    }

    #endregion
}
