using System.Runtime.CompilerServices;

namespace Gnosis.Core.Hash;

/// <summary>
/// xxHash32 快速非加密哈希算法
/// </summary>
public static class XxHash32
{
    #region 常量

    private const uint Prime1 = 0x9E3779B1U;
    private const uint Prime2 = 0x85EBCA77U;
    private const uint Prime3 = 0xC2B2AE3DU;
    private const uint Prime4 = 0x27D4EB2FU;
    private const uint Prime5 = 0x165667B1U;

    #endregion

    #region 公开方法

    /// <summary>
    /// 计算字节数组的 xxHash32 哈希值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Compute(ReadOnlySpan<byte> data, uint seed = 0)
    {
        var length = data.Length;
        uint h32;

        if (length >= 16)
        {
            h32 = ComputeLarge(data, seed);
        }
        else
        {
            h32 = seed + Prime5;
        }

        h32 += (uint)length;

        var index = length >= 16 ? length - (length & 15) : 0;

        while (index < length)
        {
            h32 += data[index] * Prime5;
            h32 = Rotl(h32, 11) * Prime1;
            index++;
        }

        return Finalize(h32);
    }

    /// <summary>
    /// 计算字符串的 xxHash32 哈希值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Compute(string text, uint seed = 0)
    {
        var maxBytes = System.Text.Encoding.UTF8.GetMaxByteCount(text.Length);
        Span<byte> bytes = stackalloc byte[maxBytes];
        var written = System.Text.Encoding.UTF8.GetBytes(text, bytes);
        return Compute(bytes[..written], seed);
    }

    #endregion

    #region 私有方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ComputeLarge(ReadOnlySpan<byte> data, uint seed)
    {
        var v1 = seed + Prime1 + Prime2;
        var v2 = seed + Prime2;
        var v3 = seed;
        var v4 = seed - Prime1;

        var offset = 0;
        var limit = data.Length - 16;

        while (offset <= limit)
        {
            v1 = Round(v1, ReadUInt32(data, offset));
            v2 = Round(v2, ReadUInt32(data, offset + 4));
            v3 = Round(v3, ReadUInt32(data, offset + 8));
            v4 = Round(v4, ReadUInt32(data, offset + 12));
            offset += 16;
        }

        var h32 = Rotl(v1, 1) + Rotl(v2, 7) + Rotl(v3, 12) + Rotl(v4, 18);

        return h32;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Round(uint acc, uint input)
    {
        acc += input * Prime2;
        acc = Rotl(acc, 13);
        acc *= Prime1;
        return acc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Finalize(uint h32)
    {
        h32 ^= h32 >> 15;
        h32 *= Prime2;
        h32 ^= h32 >> 13;
        h32 *= Prime3;
        h32 ^= h32 >> 16;
        return h32;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Rotl(uint x, int r)
    {
        return (x << r) | (x >> (32 - r));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    #endregion
}
