using System.Runtime.CompilerServices;

namespace Gnosis.Asset.Format.BcCompression;

/// <summary>
/// BC7 多模式编码器，支持 Mode 5 和 Mode 6，自动选择最优模式
/// </summary>
public static class Bc7Encoder
{
    #region 公开方法

    /// <summary>
    /// 编码 BC7 块，自动尝试 Mode 5 和 Mode 6，选择误差最小的模式
    /// </summary>
    public static void EncodeBc7Block(ReadOnlySpan<byte> rgbaBlock, Span<byte> output)
    {
        Span<byte> mode5Output = stackalloc byte[16];
        Span<byte> mode6Output = stackalloc byte[16];

        long mode5Error = EncodeBc7Mode5Block(rgbaBlock, mode5Output);
        long mode6Error = EncodeBc7Mode6Block(rgbaBlock, mode6Output);

        if (mode5Error <= mode6Error)
        {
            mode5Output.CopyTo(output);
        }
        else
        {
            mode6Output.CopyTo(output);
        }
    }

    #endregion

    #region Mode 5 编码

    /// <summary>
    /// 编码 BC7 Mode 5 块：1 子集，7 位 RGB + 8 位 Alpha 端点，2 位颜色索引 + 2 位 Alpha 索引
    /// 适用于具有独立 Alpha 变化的纹理
    /// </summary>
    public static long EncodeBc7Mode5Block(ReadOnlySpan<byte> rgbaBlock, Span<byte> output)
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
        byte ep0A = (byte)maxA;
        byte ep1R = (byte)(minR >> 1);
        byte ep1G = (byte)(minG >> 1);
        byte ep1B = (byte)(minB >> 1);
        byte ep1A = (byte)minA;

        long bestError = long.MaxValue;
        Span<byte> bestOutput = stackalloc byte[16];

        for (int pBit = 0; pBit <= 1; pBit++)
        {
            Span<byte> trialOutput = stackalloc byte[16];
            trialOutput.Clear();

            byte fullEp0R = (byte)((ep0R << 1) | pBit);
            byte fullEp0G = (byte)((ep0G << 1) | pBit);
            byte fullEp0B = (byte)((ep0B << 1) | pBit);
            byte fullEp1R = (byte)((ep1R << 1) | pBit);
            byte fullEp1G = (byte)((ep1G << 1) | pBit);
            byte fullEp1B = (byte)((ep1B << 1) | pBit);

            Span<byte> colorPaletteR = stackalloc byte[4];
            Span<byte> colorPaletteG = stackalloc byte[4];
            Span<byte> colorPaletteB = stackalloc byte[4];
            Span<byte> alphaPalette = stackalloc byte[4];

            for (int i = 0; i < 4; i++)
            {
                colorPaletteR[i] = (byte)(((3 - i) * fullEp0R + i * fullEp1R) / 3);
                colorPaletteG[i] = (byte)(((3 - i) * fullEp0G + i * fullEp1G) / 3);
                colorPaletteB[i] = (byte)(((3 - i) * fullEp0B + i * fullEp1B) / 3);
                alphaPalette[i] = (byte)(((3 - i) * ep0A + i * ep1A) / 3);
            }

            Span<byte> colorIndices = stackalloc byte[16];
            Span<byte> alphaIndices = stackalloc byte[16];
            long totalError = 0;

            for (int i = 0; i < 16; i++)
            {
                byte r = rgbaBlock[i * 4];
                byte g = rgbaBlock[i * 4 + 1];
                byte b = rgbaBlock[i * 4 + 2];
                byte a = rgbaBlock[i * 4 + 3];

                int bestColorIdx = 0;
                int bestColorDist = int.MaxValue;

                for (int j = 0; j < 4; j++)
                {
                    int dist = BcBlockEncoder.ColorDistanceSq(r, g, b, colorPaletteR[j], colorPaletteG[j], colorPaletteB[j]);
                    if (dist < bestColorDist)
                    {
                        bestColorDist = dist;
                        bestColorIdx = j;
                    }
                }

                int bestAlphaIdx = 0;
                int bestAlphaDist = int.MaxValue;

                for (int j = 0; j < 4; j++)
                {
                    int dist = (a - alphaPalette[j]) * (a - alphaPalette[j]);
                    if (dist < bestAlphaDist)
                    {
                        bestAlphaDist = dist;
                        bestAlphaIdx = j;
                    }
                }

                colorIndices[i] = (byte)bestColorIdx;
                alphaIndices[i] = (byte)bestAlphaIdx;

                byte reconR = colorPaletteR[bestColorIdx];
                byte reconG = colorPaletteG[bestColorIdx];
                byte reconB = colorPaletteB[bestColorIdx];
                byte reconA = alphaPalette[bestAlphaIdx];

                totalError += BcBlockEncoder.ColorDistanceSqAlpha(r, g, b, a, reconR, reconG, reconB, reconA);
            }

            if (totalError < bestError)
            {
                bestError = totalError;
                trialOutput[0] = 0x20;

                int bitPos = 0;
                WriteBitsSkipMode(trialOutput, 0, 2, 5, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep0R, 7, 5, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep0G, 7, 5, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep0B, 7, 5, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep0A, 8, 5, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep1R, 7, 5, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep1G, 7, 5, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep1B, 7, 5, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep1A, 8, 5, ref bitPos);
                WriteBitsSkipMode(trialOutput, pBit, 1, 5, ref bitPos);

                for (int i = 1; i < 16; i++)
                {
                    WriteBitsSkipMode(trialOutput, colorIndices[i], 2, 5, ref bitPos);
                }

                for (int i = 1; i < 16; i++)
                {
                    WriteBitsSkipMode(trialOutput, alphaIndices[i], 2, 5, ref bitPos);
                }

                trialOutput.CopyTo(bestOutput);
            }
        }

        bestOutput.CopyTo(output);
        return bestError;
    }

    #endregion

    #region Mode 6 编码（改进版，P-bit 优化）

    /// <summary>
    /// 编码 BC7 Mode 6 块：1 子集，7 位 RGBA 端点，4 位索引
    /// 改进版：尝试两种 P-bit 值，选择误差最小的
    /// </summary>
    public static long EncodeBc7Mode6Block(ReadOnlySpan<byte> rgbaBlock, Span<byte> output)
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

        long bestError = long.MaxValue;
        Span<byte> bestOutput = stackalloc byte[16];

        for (int pBit = 0; pBit <= 1; pBit++)
        {
            Span<byte> trialOutput = stackalloc byte[16];
            trialOutput.Clear();

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
            long totalError = 0;

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
                    int dist = BcBlockEncoder.ColorDistanceSqAlpha(r, g, b, a, paletteR[j], paletteG[j], paletteB[j], paletteA[j]);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIndex = j;
                    }
                }

                pixelIndices[i] = (byte)bestIndex;
                totalError += bestDist;
            }

            byte reconR0 = paletteR[0];
            byte reconG0 = paletteG[0];
            byte reconB0 = paletteB[0];
            byte reconA0 = paletteA[0];
            totalError += BcBlockEncoder.ColorDistanceSqAlpha(
                rgbaBlock[0], rgbaBlock[1], rgbaBlock[2], rgbaBlock[3],
                reconR0, reconG0, reconB0, reconA0);

            if (totalError < bestError)
            {
                bestError = totalError;
                trialOutput[0] = 0x40;

                int bitPos = 0;
                WriteBitsSkipMode(trialOutput, ep0R, 7, 6, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep0G, 7, 6, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep0B, 7, 6, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep0A, 7, 6, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep1R, 7, 6, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep1G, 7, 6, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep1B, 7, 6, ref bitPos);
                WriteBitsSkipMode(trialOutput, ep1A, 7, 6, ref bitPos);
                WriteBitsSkipMode(trialOutput, pBit, 1, 6, ref bitPos);

                for (int i = 1; i < 16; i++)
                {
                    WriteBitsSkipMode(trialOutput, pixelIndices[i], 4, 6, ref bitPos);
                }

                trialOutput.CopyTo(bestOutput);
            }
        }

        bestOutput.CopyTo(output);
        return bestError;
    }

    #endregion

    #region 位操作

    /// <summary>
    /// 向 BC7 块中写入数据位，跳过指定的 mode 位位置
    /// </summary>
    private static void WriteBitsSkipMode(Span<byte> output, int value, int bitCount, int modeBitPos, ref int dataBitPos)
    {
        for (int i = 0; i < bitCount; i++)
        {
            if ((value & (1 << i)) != 0)
            {
                int outputPos = MapDataToOutputPos(dataBitPos, modeBitPos);
                int bytePos = outputPos >> 3;
                int bitOffset = outputPos & 7;

                if (bytePos < 16)
                {
                    output[bytePos] |= (byte)(1 << bitOffset);
                }
            }

            dataBitPos++;
        }
    }

    /// <summary>
    /// 将数据位位置映射到输出位位置，跳过 mode 位
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MapDataToOutputPos(int dataBitPos, int modeBitPos)
    {
        return dataBitPos < modeBitPos ? dataBitPos : dataBitPos + 1;
    }

    #endregion
}
