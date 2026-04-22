namespace Gnosis.Asset.Format.EtcCompression;

public static class EtcCompressor
{
    #region 公开方法

    /// <summary>
    /// 将 RGBA 数据压缩为 ETC2 RGB 格式（无 Alpha），每 4x4 像素块输出 8 字节
    /// </summary>
    public static byte[] CompressEtc2Rgb(byte[] rgbaData, int width, int height)
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
                EncodeEtc2RgbBlock(block, encoded);

                int offset = (by * blocksX + bx) * 8;
                encoded.CopyTo(output.AsSpan(offset, 8));
            }
        }

        return output;
    }

    /// <summary>
    /// 将 RGBA 数据压缩为 ETC2 RGBA 格式，每 4x4 像素块输出 16 字节
    /// </summary>
    public static byte[] CompressEtc2Rgba(byte[] rgbaData, int width, int height)
    {
        ValidateInput(rgbaData, width, height);

        int blocksX = (width + 3) / 4;
        int blocksY = (height + 3) / 4;
        byte[] output = new byte[blocksX * blocksY * 16];

        Span<byte> block = stackalloc byte[64];
        Span<byte> alphaEncoded = stackalloc byte[8];
        Span<byte> colorEncoded = stackalloc byte[8];

        for (int by = 0; by < blocksY; by++)
        {
            for (int bx = 0; bx < blocksX; bx++)
            {
                ExtractBlock(rgbaData, width, height, bx, by, block);
                EncodeEtc2AlphaBlock(block, alphaEncoded);
                EncodeEtc2RgbBlock(block, colorEncoded);

                int offset = (by * blocksX + bx) * 16;
                alphaEncoded.CopyTo(output.AsSpan(offset, 8));
                colorEncoded.CopyTo(output.AsSpan(offset + 8, 8));
            }
        }

        return output;
    }

    #endregion

    #region ETC2 RGB 编码

    /// <summary>
    /// 编码 ETC2 RGB 块
    /// </summary>
    private static void EncodeEtc2RgbBlock(ReadOnlySpan<byte> block, Span<byte> output)
    {
        int avgR = 0, avgG = 0, avgB = 0;

        for (int i = 0; i < 16; i++)
        {
            avgR += block[i * 4];
            avgG += block[i * 4 + 1];
            avgB += block[i * 4 + 2];
        }

        avgR /= 16;
        avgG /= 16;
        avgB /= 16;

        int baseR = ClampByte(avgR);
        int baseG = ClampByte(avgG);
        int baseB = ClampByte(avgB);

        int bestTable = 0;
        int bestFlip = 0;
        int bestError = int.MaxValue;
        uint bestIndices = 0;

        for (int flip = 0; flip <= 1; flip++)
        {
            for (int table = 0; table < 8; table++)
            {
                uint indices = 0;
                int error = 0;

                for (int i = 0; i < 16; i++)
                {
                    int r = block[i * 4];
                    int g = block[i * 4 + 1];
                    int b = block[i * 4 + 2];

                    int bestIdx = 0;
                    int bestPixelError = int.MaxValue;

                    for (int idx = 0; idx < 4; idx++)
                    {
                        int modifier = Etc2ModifierTable[table, idx];
                        int pr = ClampByte(baseR + modifier);
                        int pg = ClampByte(baseG + modifier);
                        int pb = ClampByte(baseB + modifier);

                        int dr = r - pr;
                        int dg = g - pg;
                        int db = b - pb;
                        int pixelError = dr * dr + dg * dg + db * db;

                        if (pixelError < bestPixelError)
                        {
                            bestPixelError = pixelError;
                            bestIdx = idx;
                        }
                    }

                    indices |= (uint)bestIdx << (i * 2);
                    error += bestPixelError;
                }

                if (error < bestError)
                {
                    bestError = error;
                    bestTable = table;
                    bestFlip = flip;
                    bestIndices = indices;
                }
            }
        }

        uint blockData = 0u;
        blockData |= ((uint)bestFlip << 0);
        blockData |= ((uint)bestTable << 5);

        blockData |= ((uint)(baseR >> 3) << 23);
        blockData |= ((uint)(baseG >> 3) << 15);
        blockData |= ((uint)(baseB >> 3) << 7);

        blockData |= (bestIndices << 32) >> 32;

        output[0] = (byte)((baseR >> 1) & 0xF8);
        output[0] |= (byte)((baseG >> 5) & 0x07);
        output[1] = (byte)((baseG << 3) & 0xE0);
        output[1] |= (byte)((baseB >> 1) & 0x1F);
        output[2] = (byte)(((uint)bestTable << 5) | ((uint)bestFlip << 4) | ((bestIndices >> 24) & 0x0FU));
        output[3] = (byte)((bestIndices >> 16) & 0xFF);
        output[4] = (byte)((bestIndices >> 8) & 0xFF);
        output[5] = (byte)(bestIndices & 0xFF);
        output[6] = 0;
        output[7] = 0;
    }

    #endregion

    #region ETC2 Alpha 编码

    /// <summary>
    /// 编码 ETC2 Alpha 块（EAC）
    /// </summary>
    private static void EncodeEtc2AlphaBlock(ReadOnlySpan<byte> block, Span<byte> output)
    {
        int avgA = 0;

        for (int i = 0; i < 16; i++)
        {
            avgA += block[i * 4 + 3];
        }

        avgA /= 16;

        int baseA = ClampByte(avgA);

        int bestMultiplier = 1;
        int bestError = int.MaxValue;
        ulong bestIndices = 0;

        for (int multiplier = 1; multiplier <= 15; multiplier++)
        {
            ulong indices = 0;
            int error = 0;

            for (int i = 0; i < 16; i++)
            {
                int a = block[i * 4 + 3];

                int bestIdx = 0;
                int bestPixelError = int.MaxValue;

                for (int idx = 0; idx < 8; idx++)
                {
                    int modifier = EacAlphaModifierTable[idx];
                    int pa = ClampByte(baseA + modifier * multiplier);

                    int da = a - pa;
                    int pixelError = da * da;

                    if (pixelError < bestPixelError)
                    {
                        bestPixelError = pixelError;
                        bestIdx = idx;
                    }
                }

                indices |= (ulong)bestIdx << (i * 3);
                error += bestPixelError;
            }

            if (error < bestError)
            {
                bestError = error;
                bestMultiplier = multiplier;
                bestIndices = indices;
            }
        }

        output[0] = (byte)baseA;
        output[1] = (byte)(bestMultiplier << 4);

        for (int i = 0; i < 6; i++)
        {
            output[2 + i] = 0;
        }

        for (int i = 0; i < 16; i++)
        {
            int idx = (int)((bestIndices >> (i * 3)) & 0x7);
            int byteIdx = 2 + (i * 3) / 8;
            int bitIdx = (i * 3) % 8;

            if (byteIdx < 8)
            {
                output[byteIdx] |= (byte)(idx << bitIdx);
            }
        }
    }

    #endregion

    #region 修改器表

    private static readonly int[,] Etc2ModifierTable = new int[8, 4]
    {
        { -8, -2, 2, 8 },
        { -17, -5, 5, 17 },
        { -29, -9, 9, 29 },
        { -42, -13, 13, 42 },
        { -60, -18, 18, 60 },
        { -80, -24, 24, 80 },
        { -106, -33, 33, 106 },
        { -183, -47, 47, 183 }
    };

    private static readonly int[] EacAlphaModifierTable = new int[8]
    {
        -3, -6, -9, -15, 2, 5, 8, 13
    };

    #endregion

    #region 辅助方法

    private static void ExtractBlock(byte[] rgbaData, int width, int height, int bx, int by, Span<byte> block)
    {
        int startX = bx * 4;
        int startY = by * 4;

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                int px = Math.Min(startX + x, width - 1);
                int py = Math.Min(startY + y, height - 1);
                int srcIdx = (py * width + px) * 4;
                int dstIdx = (y * 4 + x) * 4;

                block[dstIdx] = rgbaData[srcIdx];
                block[dstIdx + 1] = rgbaData[srcIdx + 1];
                block[dstIdx + 2] = rgbaData[srcIdx + 2];
                block[dstIdx + 3] = rgbaData[srcIdx + 3];
            }
        }
    }

    private static int ClampByte(int v)
    {
        return Math.Clamp(v, 0, 255);
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
