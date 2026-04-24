namespace Gnosis.Asset.Format.AstcCompression;

/// <summary>
/// 改进版 ASTC 编码器，使用主成分分析端点拟合和自适应权重精度
/// </summary>
public static class AstcEncoder
{
    #region 公开方法

    /// <summary>
    /// 将 RGBA 数据压缩为 ASTC 4x4 格式
    /// </summary>
    public static byte[] Compress4x4(byte[] rgbaData, int width, int height)
    {
        return Compress(rgbaData, width, height, 4, 4);
    }

    /// <summary>
    /// 将 RGBA 数据压缩为 ASTC 6x6 格式
    /// </summary>
    public static byte[] Compress6x6(byte[] rgbaData, int width, int height)
    {
        return Compress(rgbaData, width, height, 6, 6);
    }

    /// <summary>
    /// 将 RGBA 数据压缩为 ASTC 8x8 格式
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

    private static void EncodeAstcBlock(ReadOnlySpan<byte> block, int blockWidth, int blockHeight, Span<byte> output)
    {
        int texelCount = blockWidth * blockHeight;

        bool isVoidExtent = true;
        byte firstR = block[0], firstG = block[1], firstB = block[2], firstA = block[3];

        for (int i = 1; i < texelCount; i++)
        {
            int srcIdx = i * 4;
            if (srcIdx + 3 >= block.Length) break;

            if (block[srcIdx] != firstR || block[srcIdx + 1] != firstG ||
                block[srcIdx + 2] != firstB || block[srcIdx + 3] != firstA)
            {
                isVoidExtent = false;
                break;
            }
        }

        if (isVoidExtent)
        {
            EncodeVoidExtentBlock(firstR, firstG, firstB, firstA, output);
            return;
        }

        EncodePcaBlock(block, texelCount, blockWidth, blockHeight, output);
    }

    /// <summary>
    /// 编码 void-extent 块（所有纹素相同颜色）
    /// </summary>
    private static void EncodeVoidExtentBlock(byte r, byte g, byte b, byte a, Span<byte> output)
    {
        output.Clear();
        output[0] = 0xFC;

        for (int i = 1; i < 6; i++)
        {
            output[i] = 0xFF;
        }

        ushort r16 = ScaleTo16(r);
        ushort g16 = ScaleTo16(g);
        ushort b16 = ScaleTo16(b);
        ushort a16 = ScaleTo16(a);

        output[6] = (byte)(r16 & 0xFF);
        output[7] = (byte)((r16 >> 8) | ((g16 << 4) & 0xF0));
        output[8] = (byte)((g16 >> 4) & 0xFF);
        output[9] = (byte)(b16 & 0xFF);
        output[10] = (byte)((b16 >> 8) | ((a16 << 4) & 0xF0));
        output[11] = (byte)((a16 >> 4) & 0xFF);
    }

    /// <summary>
    /// 使用主成分分析拟合端点，并选择最优权重精度
    /// </summary>
    private static void EncodePcaBlock(ReadOnlySpan<byte> block, int texelCount, int blockWidth, int blockHeight, Span<byte> output)
    {
        ComputePcaEndpoints(block, texelCount, out float[] ep0, out float[] ep1);

        int bestWeightRange = SelectBestWeightRange(block, texelCount, ep0, ep1, out long bestError);

        int weightBits = WeightRangeToBits(bestWeightRange);
        int weightMax = (1 << weightBits) - 1;

        output.Clear();

        int bitPos = 0;
        WriteBits(output, 0b011, 3, ref bitPos);
        WriteBits(output, (uint)bestWeightRange, 4, ref bitPos);
        WriteBits(output, (uint)(blockWidth - 4), 3, ref bitPos);
        WriteBits(output, (uint)(blockHeight - 4), 3, ref bitPos);
        WriteBits(output, 0, 2, ref bitPos);
        WriteBits(output, 0, 4, ref bitPos);
        WriteBits(output, 1, 2, ref bitPos);
        WriteBits(output, 0, 1, ref bitPos);

        WriteEndpointColor(output, QuantizeEndpoint(ep0, 8), ref bitPos);
        WriteEndpointColor(output, QuantizeEndpoint(ep1, 8), ref bitPos);

        for (int i = 0; i < texelCount; i++)
        {
            int srcIdx = i * 4;
            if (srcIdx + 3 >= block.Length)
            {
                WriteBits(output, 0, weightBits, ref bitPos);
                continue;
            }

            int weight = ComputeOptimalWeight(block, srcIdx, ep0, ep1, weightMax);
            WriteBits(output, (uint)weight, weightBits, ref bitPos);
        }
    }

    /// <summary>
    /// 使用主成分分析计算最优端点方向
    /// </summary>
    private static void ComputePcaEndpoints(ReadOnlySpan<byte> block, int texelCount, out float[] ep0, out float[] ep1)
    {
        float meanR = 0, meanG = 0, meanB = 0, meanA = 0;
        int count = 0;

        for (int i = 0; i < texelCount; i++)
        {
            int srcIdx = i * 4;
            if (srcIdx + 3 >= block.Length) break;

            meanR += block[srcIdx];
            meanG += block[srcIdx + 1];
            meanB += block[srcIdx + 2];
            meanA += block[srcIdx + 3];
            count++;
        }

        if (count == 0)
        {
            ep0 = [0, 0, 0, 0];
            ep1 = [255, 255, 255, 255];
            return;
        }

        meanR /= count;
        meanG /= count;
        meanB /= count;
        meanA /= count;

        float covRR = 0, covRG = 0, covRB = 0, covRA = 0;
        float covGG = 0, covGB = 0, covGA = 0;
        float covBB = 0, covBA = 0;
        float covAA = 0;

        for (int i = 0; i < texelCount; i++)
        {
            int srcIdx = i * 4;
            if (srcIdx + 3 >= block.Length) break;

            float dr = block[srcIdx] - meanR;
            float dg = block[srcIdx + 1] - meanG;
            float db = block[srcIdx + 2] - meanB;
            float da = block[srcIdx + 3] - meanA;

            covRR += dr * dr;
            covRG += dr * dg;
            covRB += dr * db;
            covRA += dr * da;
            covGG += dg * dg;
            covGB += dg * db;
            covGA += dg * da;
            covBB += db * db;
            covBA += db * da;
            covAA += da * da;
        }

        float dirR = covRR + covRG + covRB + covRA;
        float dirG = covRG + covGG + covGB + covGA;
        float dirB = covRB + covGB + covBB + covBA;
        float dirA = covRA + covGA + covBA + covAA;

        float dirLen = MathF.Sqrt(dirR * dirR + dirG * dirG + dirB * dirB + dirA * dirA);
        if (dirLen < 1e-10f)
        {
            ep0 = [meanR, meanG, meanB, meanA];
            ep1 = [meanR, meanG, meanB, meanA];
            return;
        }

        dirR /= dirLen;
        dirG /= dirLen;
        dirB /= dirLen;
        dirA /= dirLen;

        float minProj = float.MaxValue;
        float maxProj = float.MinValue;

        for (int i = 0; i < texelCount; i++)
        {
            int srcIdx = i * 4;
            if (srcIdx + 3 >= block.Length) break;

            float proj = block[srcIdx] * dirR + block[srcIdx + 1] * dirG +
                         block[srcIdx + 2] * dirB + block[srcIdx + 3] * dirA;

            if (proj < minProj) minProj = proj;
            if (proj > maxProj) maxProj = proj;
        }

        ep0 = [
            Math.Clamp(meanR + dirR * minProj, 0, 255),
            Math.Clamp(meanG + dirG * minProj, 0, 255),
            Math.Clamp(meanB + dirB * minProj, 0, 255),
            Math.Clamp(meanA + dirA * minProj, 0, 255)
        ];

        ep1 = [
            Math.Clamp(meanR + dirR * maxProj, 0, 255),
            Math.Clamp(meanG + dirG * maxProj, 0, 255),
            Math.Clamp(meanB + dirB * maxProj, 0, 255),
            Math.Clamp(meanA + dirA * maxProj, 0, 255)
        ];
    }

    /// <summary>
    /// 选择最优权重范围，在可用位预算内尝试不同精度
    /// </summary>
    private static int SelectBestWeightRange(ReadOnlySpan<byte> block, int texelCount, float[] ep0, float[] ep1, out long bestError)
    {
        int[] weightRanges = texelCount <= 4 ? [5, 2] : [5, 1];

        bestError = long.MaxValue;
        int bestRange = 1;

        foreach (int range in weightRanges)
        {
            int weightBits = WeightRangeToBits(range);
            int weightMax = (1 << weightBits) - 1;

            int weightsSize = (texelCount * weightBits + 7) / 8;
            int headerBits = 11;
            int endpointBits = 32;
            int totalBits = headerBits + endpointBits + weightsSize * 8;

            if (totalBits > 128) continue;

            long error = 0;

            for (int i = 0; i < texelCount; i++)
            {
                int srcIdx = i * 4;
                if (srcIdx + 3 >= block.Length) break;

                int weight = ComputeOptimalWeight(block, srcIdx, ep0, ep1, weightMax);

                float t = weightMax > 0 ? (float)weight / weightMax : 0f;
                float reconR = ep0[0] * (1 - t) + ep1[0] * t;
                float reconG = ep0[1] * (1 - t) + ep1[1] * t;
                float reconB = ep0[2] * (1 - t) + ep1[2] * t;
                float reconA = ep0[3] * (1 - t) + ep1[3] * t;

                float dr = block[srcIdx] - reconR;
                float dg = block[srcIdx + 1] - reconG;
                float db = block[srcIdx + 2] - reconB;
                float da = block[srcIdx + 3] - reconA;

                error += (long)(dr * dr + dg * dg + db * db + da * da);
            }

            if (error < bestError)
            {
                bestError = error;
                bestRange = range;
            }
        }

        return bestRange;
    }

    /// <summary>
    /// 计算单个纹素的最优插值权重
    /// </summary>
    private static int ComputeOptimalWeight(ReadOnlySpan<byte> block, int srcIdx, float[] ep0, float[] ep1, int weightMax)
    {
        float r = block[srcIdx];
        float g = block[srcIdx + 1];
        float b = block[srcIdx + 2];
        float a = block[srcIdx + 3];

        float dirR = ep1[0] - ep0[0];
        float dirG = ep1[1] - ep0[1];
        float dirB = ep1[2] - ep0[2];
        float dirA = ep1[3] - ep0[3];

        float dirLenSq = dirR * dirR + dirG * dirG + dirB * dirB + dirA * dirA;

        if (dirLenSq < 1e-10f)
        {
            return 0;
        }

        float offsetR = r - ep0[0];
        float offsetG = g - ep0[1];
        float offsetB = b - ep0[2];
        float offsetA = a - ep0[3];

        float proj = (offsetR * dirR + offsetG * dirG + offsetB * dirB + offsetA * dirA) / dirLenSq;

        int weight = (int)MathF.Round(proj * weightMax);
        return Math.Clamp(weight, 0, weightMax);
    }

    /// <summary>
    /// 将权重范围值转换为权重位数
    /// </summary>
    private static int WeightRangeToBits(int weightRange)
    {
        return weightRange switch
        {
            0 => 1,
            1 => 2,
            2 => 3,
            3 => 4,
            4 => 5,
            5 => 2,
            6 => 3,
            7 => 4,
            _ => 2
        };
    }

    /// <summary>
    /// 将浮点端点值量化为 8 位整数
    /// </summary>
    private static byte[] QuantizeEndpoint(float[] ep, int bits)
    {
        int maxVal = (1 << bits) - 1;
        byte[] result = new byte[4];

        for (int c = 0; c < 4; c++)
        {
            int quantized = (int)MathF.Round(ep[c] * maxVal / 255f);
            result[c] = (byte)Math.Clamp(quantized, 0, maxVal);
        }

        return result;
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
