using System;

namespace Gnosis.Network.Serialization;

/// <summary>
/// 网络数据压缩算法类型
/// </summary>
public enum CompressionAlgorithm : byte
{
    /// <summary>
    /// 无压缩
    /// </summary>
    None = 0,

    /// <summary>
    /// 游程编码，适用于高度重复的数据
    /// </summary>
    Rle = 1,

    /// <summary>
    /// 轻量 LZ77 变体，适用于一般网络数据
    /// </summary>
    LzLite = 2
}

/// <summary>
/// 网络数据压缩器，提供零外部依赖的轻量级压缩与解压缩
/// </summary>
public static class NetworkCompressor
{
    #region 公共方法

    /// <summary>
    /// 压缩数据
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <param name="algorithm">压缩算法</param>
    /// <returns>压缩后的数据（首字节为算法标识）</returns>
    public static byte[] Compress(ReadOnlySpan<byte> data, CompressionAlgorithm algorithm)
    {
        if (data.Length == 0)
        {
            return [(byte)CompressionAlgorithm.None, 0, 0, 0, 0];
        }

        var compressed = algorithm switch
        {
            CompressionAlgorithm.Rle => CompressRle(data),
            CompressionAlgorithm.LzLite => CompressLzLite(data),
            _ => data.ToArray()
        };

        if (compressed.Length >= data.Length)
        {
            var result = new byte[1 + 4 + data.Length];
            result[0] = (byte)CompressionAlgorithm.None;
            WriteInt32BigEndian(result, 1, data.Length);
            data.CopyTo(result.AsSpan(5));
            return result;
        }

        var output = new byte[1 + 4 + compressed.Length];
        output[0] = (byte)algorithm;
        WriteInt32BigEndian(output, 1, data.Length);
        compressed.CopyTo(output.AsSpan(5));
        return output;
    }

    /// <summary>
    /// 解压缩数据
    /// </summary>
    /// <param name="data">压缩数据（首字节为算法标识）</param>
    /// <returns>解压缩后的原始数据</returns>
    public static byte[] Decompress(ReadOnlySpan<byte> data)
    {
        if (data.Length < 5)
        {
            throw new ArgumentException("压缩数据格式无效：长度不足 5 字节");
        }

        var algorithm = (CompressionAlgorithm)data[0];
        var originalLength = ReadInt32BigEndian(data, 1);
        var payload = data[5..];

        return algorithm switch
        {
            CompressionAlgorithm.None => payload.ToArray(),
            CompressionAlgorithm.Rle => DecompressRle(payload, originalLength),
            CompressionAlgorithm.LzLite => DecompressLzLite(payload, originalLength),
            _ => throw new NotSupportedException($"不支持的压缩算法：{algorithm}")
        };
    }

    /// <summary>
    /// 选择最佳压缩算法
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <returns>推荐的压缩算法</returns>
    public static CompressionAlgorithm SelectAlgorithm(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16)
        {
            return CompressionAlgorithm.None;
        }

        var rleEstimate = EstimateRleRatio(data);
        var lzEstimate = EstimateLzLiteRatio(data);

        if (rleEstimate > 0.5f && rleEstimate >= lzEstimate)
        {
            return CompressionAlgorithm.Rle;
        }

        if (lzEstimate > 0.3f)
        {
            return CompressionAlgorithm.LzLite;
        }

        return CompressionAlgorithm.None;
    }

    #endregion

    #region RLE 压缩

    /// <summary>
    /// RLE 压缩：适用于高度重复的数据（如零填充的状态快照差异）
    /// </summary>
    private static byte[] CompressRle(ReadOnlySpan<byte> data)
    {
        var output = new List<byte>();

        var i = 0;
        while (i < data.Length)
        {
            var current = data[i];
            var runLength = 1;

            while (i + runLength < data.Length && data[i + runLength] == current && runLength < 255)
            {
                runLength++;
            }

            output.Add(current);
            output.Add((byte)runLength);
            i += runLength;
        }

        return output.ToArray();
    }

    /// <summary>
    /// RLE 解压缩
    /// </summary>
    private static byte[] DecompressRle(ReadOnlySpan<byte> data, int originalLength)
    {
        var output = new byte[originalLength];
        var writePos = 0;
        var readPos = 0;

        while (readPos + 1 < data.Length && writePos < originalLength)
        {
            var value = data[readPos];
            var count = data[readPos + 1];
            readPos += 2;

            var end = Math.Min(writePos + count, originalLength);

            for (var j = writePos; j < end; j++)
            {
                output[j] = value;
            }

            writePos = end;
        }

        return output;
    }

    /// <summary>
    /// 估算 RLE 压缩率
    /// </summary>
    private static float EstimateRleRatio(ReadOnlySpan<byte> data)
    {
        var runs = 0;
        var i = 0;

        while (i < data.Length)
        {
            var current = data[i];
            while (i < data.Length && data[i] == current)
            {
                i++;
            }

            runs++;
        }

        var compressedSize = runs * 2;
        return 1.0f - (float)compressedSize / data.Length;
    }

    #endregion

    #region LzLite 压缩

    private const int LzLiteWindowBits = 10;
    private const int LzLiteWindowSize = 1 << LzLiteWindowBits;
    private const int LzLiteMinMatch = 3;
    private const int LzLiteMaxMatch = 18;

    /// <summary>
    /// 轻量 LZ77 变体压缩：适用于一般网络数据（状态快照、RPC 参数等）
    /// </summary>
    private static byte[] CompressLzLite(ReadOnlySpan<byte> data)
    {
        var output = new List<byte>();
        var i = 0;

        while (i < data.Length)
        {
            var bestOffset = 0;
            var bestLength = 0;

            var searchStart = Math.Max(0, i - LzLiteWindowSize);

            for (var j = searchStart; j < i; j++)
            {
                var matchLength = 0;

                while (matchLength < LzLiteMaxMatch && i + matchLength < data.Length && data[j + matchLength] == data[i + matchLength])
                {
                    matchLength++;
                }

                if (matchLength > bestLength && matchLength >= LzLiteMinMatch)
                {
                    bestLength = matchLength;
                    bestOffset = i - j;
                }
            }

            if (bestLength >= LzLiteMinMatch)
            {
                output.Add(0xFF);
                output.Add((byte)((bestOffset >> 2) & 0xFF));
                output.Add((byte)(((bestOffset & 0x3) << 6) | ((bestLength - LzLiteMinMatch) & 0x3F)));
                i += bestLength;
            }
            else
            {
                var literal = data[i];

                if (literal == 0xFF)
                {
                    output.Add(0xFF);
                    output.Add(0x00);
                    output.Add(0x00);
                }
                else
                {
                    output.Add(literal);
                }

                i++;
            }
        }

        return output.ToArray();
    }

    /// <summary>
    /// LzLite 解压缩
    /// </summary>
    private static byte[] DecompressLzLite(ReadOnlySpan<byte> data, int originalLength)
    {
        var output = new byte[originalLength];
        var writePos = 0;
        var readPos = 0;

        while (readPos < data.Length && writePos < originalLength)
        {
            if (data[readPos] == 0xFF)
            {
                if (readPos + 2 >= data.Length)
                {
                    break;
                }

                var b1 = data[readPos + 1];
                var b2 = data[readPos + 2];
                readPos += 3;

                if (b1 == 0x00 && b2 == 0x00)
                {
                    output[writePos++] = 0xFF;
                }
                else
                {
                    var offset = (b1 << 2) | (b2 >> 6);
                    var length = (b2 & 0x3F) + LzLiteMinMatch;

                    for (var j = 0; j < length && writePos < originalLength; j++)
                    {
                        output[writePos] = output[writePos - offset];
                        writePos++;
                    }
                }
            }
            else
            {
                output[writePos++] = data[readPos++];
            }
        }

        return output;
    }

    /// <summary>
    /// 估算 LzLite 压缩率
    /// </summary>
    private static float EstimateLzLiteRatio(ReadOnlySpan<byte> data)
    {
        var matches = 0;
        var i = 0;

        while (i < data.Length)
        {
            var bestLength = 0;
            var searchStart = Math.Max(0, i - LzLiteWindowSize);

            for (var j = searchStart; j < i; j++)
            {
                var matchLength = 0;

                while (matchLength < LzLiteMaxMatch && i + matchLength < data.Length && data[j + matchLength] == data[i + matchLength])
                {
                    matchLength++;
                }

                if (matchLength > bestLength)
                {
                    bestLength = matchLength;
                }
            }

            if (bestLength >= LzLiteMinMatch)
            {
                matches += bestLength;
                i += bestLength;
            }
            else
            {
                i++;
            }
        }

        var estimatedCompressedSize = data.Length - matches + matches * 3 / LzLiteMinMatch;
        return 1.0f - (float)estimatedCompressedSize / data.Length;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 以大端序写入 32 位整数
    /// </summary>
    private static void WriteInt32BigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }

    /// <summary>
    /// 以大端序读取 32 位整数
    /// </summary>
    private static int ReadInt32BigEndian(ReadOnlySpan<byte> data, int offset)
    {
        return (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
    }

    #endregion
}
