using System.Runtime.CompilerServices;

namespace Gnosis.Core.Hash;

/// <summary>
/// xxHash64 快速非加密哈希算法
/// </summary>
public static class XxHash64
{
    #region 常量

    private const ulong Prime1 = 0x9E3779B185EBCA87UL;
    private const ulong Prime2 = 0xC2B2AE3D27D4EB4FUL;
    private const ulong Prime3 = 0x165667B19E3779F9UL;
    private const ulong Prime4 = 0x85EBCA77C2B2AE63UL;
    private const ulong Prime5 = 0x27D4EB2F165667C5UL;

    #endregion

    #region 公开方法

    /// <summary>
    /// 计算字节数组的 xxHash64 哈希值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Compute(ReadOnlySpan<byte> data, ulong seed = 0)
    {
        var length = data.Length;
        ulong h64;

        if (length >= 32)
        {
            h64 = ComputeLarge(data, seed);
        }
        else
        {
            h64 = seed + Prime5;
        }

        h64 += (ulong)length;

        var index = length >= 32 ? length - (length & 31) : 0;

        while (index + 8 <= length)
        {
            h64 ^= Round1(ReadUInt64(data, index));
            h64 = Rotl(h64, 27) * Prime1 + Prime4;
            index += 8;
        }

        if (index + 4 <= length)
        {
            h64 ^= ReadUInt32(data, index) * Prime1;
            h64 = Rotl(h64, 23) * Prime2 + Prime3;
            index += 4;
        }

        while (index < length)
        {
            h64 ^= data[index] * Prime5;
            h64 = Rotl(h64, 11) * Prime1;
            index++;
        }

        return Finalize(h64);
    }

    /// <summary>
    /// 计算字符串的 xxHash64 哈希值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Compute(string text, ulong seed = 0)
    {
        var maxBytes = System.Text.Encoding.UTF8.GetMaxByteCount(text.Length);
        Span<byte> bytes = stackalloc byte[maxBytes];
        var written = System.Text.Encoding.UTF8.GetBytes(text, bytes);
        return Compute(bytes[..written], seed);
    }

    #endregion

    #region 私有方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ComputeLarge(ReadOnlySpan<byte> data, ulong seed)
    {
        var v1 = seed + Prime1 + Prime2;
        var v2 = seed + Prime2;
        var v3 = seed;
        var v4 = seed - Prime1;

        var offset = 0;
        var limit = data.Length - 32;

        while (offset <= limit)
        {
            v1 = Round(v1, ReadUInt64(data, offset));
            v2 = Round(v2, ReadUInt64(data, offset + 8));
            v3 = Round(v3, ReadUInt64(data, offset + 16));
            v4 = Round(v4, ReadUInt64(data, offset + 24));
            offset += 32;
        }

        var h64 = Rotl(v1, 1) + Rotl(v2, 7) + Rotl(v3, 12) + Rotl(v4, 18);

        h64 = MergeRound(h64, v1);
        h64 = MergeRound(h64, v2);
        h64 = MergeRound(h64, v3);
        h64 = MergeRound(h64, v4);

        return h64;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Round(ulong acc, ulong input)
    {
        acc += input * Prime2;
        acc = Rotl(acc, 31);
        acc *= Prime1;
        return acc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Round1(ulong input)
    {
        return Rotl(input * Prime2, 31) * Prime1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MergeRound(ulong acc, ulong val)
    {
        acc ^= Round1(val);
        acc *= Prime1;
        return acc + Prime4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Finalize(ulong h64)
    {
        h64 ^= h64 >> 33;
        h64 *= Prime2;
        h64 ^= h64 >> 29;
        h64 *= Prime3;
        h64 ^= h64 >> 32;
        return h64;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Rotl(ulong x, int r)
    {
        return (x << r) | (x >> (64 - r));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ReadUInt64(ReadOnlySpan<byte> data, int offset)
    {
        return data[offset]
             | ((ulong)data[offset + 1] << 8)
             | ((ulong)data[offset + 2] << 16)
             | ((ulong)data[offset + 3] << 24)
             | ((ulong)data[offset + 4] << 32)
             | ((ulong)data[offset + 5] << 40)
             | ((ulong)data[offset + 6] << 48)
             | ((ulong)data[offset + 7] << 56);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    #endregion
}
