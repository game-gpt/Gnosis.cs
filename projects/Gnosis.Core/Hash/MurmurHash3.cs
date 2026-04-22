using System.Runtime.CompilerServices;

namespace Gnosis.Core.Hash;

/// <summary>
/// MurmurHash3 非加密哈希算法（x86_32 变体）
/// </summary>
public static class MurmurHash3
{
    #region 常量

    private const uint C1 = 0xCC9E2D51U;
    private const uint C2 = 0x1B873593U;

    #endregion

    #region 公开方法

    /// <summary>
    /// 计算字节数组的 MurmurHash3 哈希值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Compute(ReadOnlySpan<byte> data, uint seed = 0)
    {
        var length = data.Length;
        var h1 = seed;
        var nblocks = length / 4;

        for (var i = 0; i < nblocks; i++)
        {
            var k1 = ReadUInt32(data, i * 4);

            k1 *= C1;
            k1 = Rotl(k1, 15);
            k1 *= C2;

            h1 ^= k1;
            h1 = Rotl(h1, 13);
            h1 = h1 * 5 + 0xE6546B64U;
        }

        var tailStart = nblocks * 4;
        var k1Tail = 0U;

        switch (length & 3)
        {
            case 3:
                k1Tail ^= (uint)data[tailStart + 2] << 16;
                goto case 2;
            case 2:
                k1Tail ^= (uint)data[tailStart + 1] << 8;
                goto case 1;
            case 1:
                k1Tail ^= data[tailStart];
                k1Tail *= C1;
                k1Tail = Rotl(k1Tail, 15);
                k1Tail *= C2;
                h1 ^= k1Tail;
                break;
        }

        h1 ^= (uint)length;
        h1 = Fmix(h1);

        return h1;
    }

    /// <summary>
    /// 计算字符串的 MurmurHash3 哈希值
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
    private static uint Rotl(uint x, int r)
    {
        return (x << r) | (x >> (32 - r));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Fmix(uint h)
    {
        h ^= h >> 16;
        h *= 0x85EBCA6BU;
        h ^= h >> 13;
        h *= 0xC2B2AE35U;
        h ^= h >> 16;
        return h;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset)
    {
        return (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
    }

    #endregion
}
