using System.Runtime.CompilerServices;

namespace Gnosis.Assets.Formats.BcCompression;

/// <summary>
/// BC 块压缩底层编码工具，提供 RGB565 编解码、调色板计算、块编码等功能
/// </summary>
public static class BcBlockEncoder
{
    #region RGB565 编码解码

    /// <summary>
    /// 将 RGB 颜色编码为 RGB565 格式
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort EncodeRgb565(byte r, byte g, byte b)
    {
        return (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
    }

    /// <summary>
    /// 从 RGB565 格式解码为 RGB 颜色
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DecodeRgb565(ushort value, out byte r, out byte g, out byte b)
    {
        r = (byte)(((value >> 11) & 0x1F) * 255 / 31);
        g = (byte)(((value >> 5) & 0x3F) * 255 / 63);
        b = (byte)((value & 0x1F) * 255 / 31);
    }

    #endregion

    #region 颜色距离计算

    /// <summary>
    /// 计算两个 RGB 颜色之间的平方距离
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ColorDistanceSq(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
    {
        int dr = r1 - r2;
        int dg = g1 - g2;
        int db = b1 - b2;
        return dr * dr + dg * dg + db * db;
    }

    /// <summary>
    /// 计算两个 RGBA 颜色之间的平方距离
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ColorDistanceSqAlpha(byte r1, byte g1, byte b1, byte a1, byte r2, byte g2, byte b2, byte a2)
    {
        int dr = r1 - r2;
        int dg = g1 - g2;
        int db = b1 - b2;
        int da = a1 - a2;
        return dr * dr + dg * dg + db * db + da * da;
    }

    #endregion

    #region 调色板计算

    /// <summary>
    /// 计算 BC1 四色调色板，palette 布局为 [R0,G0,B0,0, R1,G1,B1,0, R2,G2,B2,0, R3,G3,B3,0]
    /// </summary>
    public static void ComputeBc1Palette(ushort c0, ushort c1, Span<byte> palette)
    {
        DecodeRgb565(c0, out byte r0, out byte g0, out byte b0);
        DecodeRgb565(c1, out byte r1, out byte g1, out byte b1);

        palette[0] = r0;
        palette[1] = g0;
        palette[2] = b0;
        palette[3] = 0;

        palette[4] = r1;
        palette[5] = g1;
        palette[6] = b1;
        palette[7] = 0;

        if (c0 > c1)
        {
            palette[8] = (byte)((2 * r0 + r1) / 3);
            palette[9] = (byte)((2 * g0 + g1) / 3);
            palette[10] = (byte)((2 * b0 + b1) / 3);
            palette[11] = 0;

            palette[12] = (byte)((r0 + 2 * r1) / 3);
            palette[13] = (byte)((g0 + 2 * g1) / 3);
            palette[14] = (byte)((b0 + 2 * b1) / 3);
            palette[15] = 0;
        }
        else
        {
            palette[8] = (byte)((r0 + r1) / 2);
            palette[9] = (byte)((g0 + g1) / 2);
            palette[10] = (byte)((b0 + b1) / 2);
            palette[11] = 0;

            palette[12] = 0;
            palette[13] = 0;
            palette[14] = 0;
            palette[15] = 0;
        }
    }

    /// <summary>
    /// 计算 BC3/BC4 八值 Alpha 调色板
    /// </summary>
    public static void ComputeAlphaPalette(byte a0, byte a1, Span<byte> palette)
    {
        palette[0] = a0;
        palette[1] = a1;

        if (a0 > a1)
        {
            palette[2] = (byte)((6 * a0 + 1 * a1) / 7);
            palette[3] = (byte)((5 * a0 + 2 * a1) / 7);
            palette[4] = (byte)((4 * a0 + 3 * a1) / 7);
            palette[5] = (byte)((3 * a0 + 4 * a1) / 7);
            palette[6] = (byte)((2 * a0 + 5 * a1) / 7);
            palette[7] = (byte)((1 * a0 + 6 * a1) / 7);
        }
        else
        {
            palette[2] = (byte)((4 * a0 + 1 * a1) / 5);
            palette[3] = (byte)((3 * a0 + 2 * a1) / 5);
            palette[4] = (byte)((2 * a0 + 3 * a1) / 5);
            palette[5] = (byte)((1 * a0 + 4 * a1) / 5);
            palette[6] = 0;
            palette[7] = 255;
        }
    }

    #endregion

    #region 最近索引查找

    /// <summary>
    /// 在调色板中查找最近颜色的索引
    /// </summary>
    public static int FindNearestColorIndex(byte r, byte g, byte b, ReadOnlySpan<byte> palette, int paletteCount)
    {
        int bestIndex = 0;
        int bestDist = int.MaxValue;

        for (int i = 0; i < paletteCount; i++)
        {
            int offset = i * 4;
            int dist = ColorDistanceSq(r, g, b, palette[offset], palette[offset + 1], palette[offset + 2]);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    /// <summary>
    /// 在 Alpha 调色板中查找最近值的索引
    /// </summary>
    public static int FindNearestAlphaIndex(byte alpha, ReadOnlySpan<byte> palette)
    {
        int bestIndex = 0;
        int bestDist = int.MaxValue;

        for (int i = 0; i < 8; i++)
        {
            int dist = Math.Abs(alpha - palette[i]);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    #endregion

    #region BC1 颜色块编码

    /// <summary>
    /// 编码 BC1 颜色块（8 字节），forceFourColorMode 为 true 时强制四色模式（用于 BC3 颜色块）
    /// </summary>
    public static void EncodeBc1ColorBlock(ReadOnlySpan<byte> rgbaBlock, Span<byte> output, bool forceFourColorMode = false)
    {
        int minR = 255, minG = 255, minB = 255;
        int maxR = 0, maxG = 0, maxB = 0;

        for (int i = 0; i < 16; i++)
        {
            byte r = rgbaBlock[i * 4];
            byte g = rgbaBlock[i * 4 + 1];
            byte b = rgbaBlock[i * 4 + 2];

            if (r < minR) minR = r;
            if (g < minG) minG = g;
            if (b < minB) minB = b;
            if (r > maxR) maxR = r;
            if (g > maxG) maxG = g;
            if (b > maxB) maxB = b;
        }

        ushort c0 = EncodeRgb565((byte)maxR, (byte)maxG, (byte)maxB);
        ushort c1 = EncodeRgb565((byte)minR, (byte)minG, (byte)minB);

        bool hasAlpha = !forceFourColorMode && HasTransparentPixels(rgbaBlock);

        if (hasAlpha)
        {
            if (c0 > c1)
            {
                (c0, c1) = (c1, c0);
            }
        }
        else
        {
            if (c0 < c1)
            {
                (c0, c1) = (c1, c0);
            }
            else if (c0 == c1)
            {
                if (c0 < 0xFFFF)
                {
                    c0++;
                }
                else
                {
                    c1--;
                }
            }
        }

        Span<byte> palette = stackalloc byte[16];
        ComputeBc1Palette(c0, c1, palette);

        output[0] = (byte)(c0 & 0xFF);
        output[1] = (byte)((c0 >> 8) & 0xFF);
        output[2] = (byte)(c1 & 0xFF);
        output[3] = (byte)((c1 >> 8) & 0xFF);

        int paletteCount = (c0 > c1) ? 4 : 3;
        uint indices = 0;

        for (int i = 0; i < 16; i++)
        {
            byte r = rgbaBlock[i * 4];
            byte g = rgbaBlock[i * 4 + 1];
            byte b = rgbaBlock[i * 4 + 2];
            byte a = rgbaBlock[i * 4 + 3];

            int index;
            if (hasAlpha && a < 128)
            {
                index = 3;
            }
            else
            {
                index = FindNearestColorIndex(r, g, b, palette, paletteCount);
            }

            indices |= (uint)(index << (i * 2));
        }

        output[4] = (byte)(indices & 0xFF);
        output[5] = (byte)((indices >> 8) & 0xFF);
        output[6] = (byte)((indices >> 16) & 0xFF);
        output[7] = (byte)((indices >> 24) & 0xFF);
    }

    #endregion

    #region Alpha 块编码

    /// <summary>
    /// 编码 BC3/BC4 Alpha 块（8 字节），用于 Alpha 通道或单通道压缩
    /// </summary>
    public static void EncodeAlphaBlock(ReadOnlySpan<byte> alphaBlock, Span<byte> output)
    {
        byte minA = 255, maxA = 0;

        for (int i = 0; i < 16; i++)
        {
            if (alphaBlock[i] < minA) minA = alphaBlock[i];
            if (alphaBlock[i] > maxA) maxA = alphaBlock[i];
        }

        output[0] = maxA;
        output[1] = minA;

        Span<byte> palette = stackalloc byte[8];
        ComputeAlphaPalette(maxA, minA, palette);

        ulong indices = 0;

        for (int i = 0; i < 16; i++)
        {
            int index = FindNearestAlphaIndex(alphaBlock[i], palette);
            indices |= (ulong)index << (i * 3);
        }

        output[2] = (byte)(indices & 0xFF);
        output[3] = (byte)((indices >> 8) & 0xFF);
        output[4] = (byte)((indices >> 16) & 0xFF);
        output[5] = (byte)((indices >> 24) & 0xFF);
        output[6] = (byte)((indices >> 32) & 0xFF);
        output[7] = (byte)((indices >> 40) & 0xFF);
    }

    #endregion

    #region BC7 块编码

    /// <summary>
    /// 编码简化的 BC7 Mode 6 块（16 字节），使用单子集、4 位索引
    /// </summary>
    public static void EncodeBc7Mode6Block(ReadOnlySpan<byte> rgbaBlock, Span<byte> output)
    {
        output.Clear();

        int minR = 255, minG = 255, minB = 255, minA = 255;
        int maxR = 0, maxG = 0, maxB = 0, maxA = 0;

        for (int i = 0; i < 16; i++)
        {
            byte r = rgbaBlock[i * 4];
            byte g = rgbaBlock[i * 4 + 1];
            byte b = rgbaBlock[i * 4 + 2];
            byte a = rgbaBlock[i * 4 + 3];

            if (r < minR) minR = r;
            if (g < minG) minG = g;
            if (b < minB) minB = b;
            if (a < minA) minA = a;
            if (r > maxR) maxR = r;
            if (g > maxG) maxG = g;
            if (b > maxB) maxB = b;
            if (a > maxA) maxA = a;
        }

        byte ep0R = (byte)(maxR >> 1);
        byte ep0G = (byte)(maxG >> 1);
        byte ep0B = (byte)(maxB >> 1);
        byte ep0A = (byte)(maxA >> 1);
        byte ep1R = (byte)(minR >> 1);
        byte ep1G = (byte)(minG >> 1);
        byte ep1B = (byte)(minB >> 1);
        byte ep1A = (byte)(minA >> 1);

        byte pBit = 0;

        byte fullEp0R = (byte)((ep0R << 1) | pBit);
        byte fullEp0G = (byte)((ep0G << 1) | pBit);
        byte fullEp0B = (byte)((ep0B << 1) | pBit);
        byte fullEp0A = (byte)((ep0A << 1) | pBit);
        byte fullEp1R = (byte)((ep1R << 1) | pBit);
        byte fullEp1G = (byte)((ep1G << 1) | pBit);
        byte fullEp1B = (byte)((ep1B << 1) | pBit);
        byte fullEp1A = (byte)((ep1A << 1) | pBit);

        Span<byte> paletteR = stackalloc byte[16];
        Span<byte> paletteG = stackalloc byte[16];
        Span<byte> paletteB = stackalloc byte[16];
        Span<byte> paletteA = stackalloc byte[16];

        for (int i = 0; i < 16; i++)
        {
            paletteR[i] = (byte)(((16 - i) * fullEp0R + i * fullEp1R) / 15);
            paletteG[i] = (byte)(((16 - i) * fullEp0G + i * fullEp1G) / 15);
            paletteB[i] = (byte)(((16 - i) * fullEp0B + i * fullEp1B) / 15);
            paletteA[i] = (byte)(((16 - i) * fullEp0A + i * fullEp1A) / 15);
        }

        Span<byte> pixelIndices = stackalloc byte[16];
        pixelIndices[0] = 0;

        for (int i = 1; i < 16; i++)
        {
            byte r = rgbaBlock[i * 4];
            byte g = rgbaBlock[i * 4 + 1];
            byte b = rgbaBlock[i * 4 + 2];
            byte a = rgbaBlock[i * 4 + 3];

            int bestIndex = 0;
            int bestDist = int.MaxValue;

            for (int j = 0; j < 16; j++)
            {
                int dist = ColorDistanceSqAlpha(r, g, b, a, paletteR[j], paletteG[j], paletteB[j], paletteA[j]);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = j;
                }
            }

            pixelIndices[i] = (byte)bestIndex;
        }

        output[0] = 0x40;

        int dataBitPos = 0;
        WriteDataBits(output, ep0R, 7, ref dataBitPos);
        WriteDataBits(output, ep0G, 7, ref dataBitPos);
        WriteDataBits(output, ep0B, 7, ref dataBitPos);
        WriteDataBits(output, ep0A, 7, ref dataBitPos);
        WriteDataBits(output, ep1R, 7, ref dataBitPos);
        WriteDataBits(output, ep1G, 7, ref dataBitPos);
        WriteDataBits(output, ep1B, 7, ref dataBitPos);
        WriteDataBits(output, ep1A, 7, ref dataBitPos);
        WriteDataBits(output, pBit, 1, ref dataBitPos);

        for (int i = 1; i < 16; i++)
        {
            WriteDataBits(output, pixelIndices[i], 4, ref dataBitPos);
        }
    }

    #endregion

    #region 位操作工具

    /// <summary>
    /// 向 BC7 块中写入数据位，自动跳过 mode 位（位置 6）
    /// </summary>
    private static void WriteDataBits(Span<byte> output, int value, int bitCount, ref int dataBitPos)
    {
        for (int i = 0; i < bitCount; i++)
        {
            if ((value & (1 << i)) != 0)
            {
                int outputPos = MapDataToOutputPos(dataBitPos);
                int bytePos = outputPos >> 3;
                int bitOffset = outputPos & 7;
                output[bytePos] |= (byte)(1 << bitOffset);
            }

            dataBitPos++;
        }
    }

    /// <summary>
    /// 将数据位位置映射到输出位位置，跳过 mode 位（位置 6）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MapDataToOutputPos(int dataBitPos)
    {
        return dataBitPos < 6 ? dataBitPos : dataBitPos + 1;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 检查 4x4 块中是否存在透明像素（Alpha 小于 128）
    /// </summary>
    private static bool HasTransparentPixels(ReadOnlySpan<byte> rgbaBlock)
    {
        for (int i = 0; i < 16; i++)
        {
            if (rgbaBlock[i * 4 + 3] < 128)
            {
                return true;
            }
        }

        return false;
    }

    #endregion
}
