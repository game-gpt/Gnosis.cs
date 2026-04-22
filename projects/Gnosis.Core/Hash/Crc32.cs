using System.Runtime.CompilerServices;

namespace Gnosis.Core.Hash;

/// <summary>
/// CRC32 校验和算法
/// </summary>
public static class Crc32
{
    #region 字段

    private static readonly uint[] Table;

    #endregion

    #region 静态构造

    static Crc32()
    {
        Table = new uint[256];

        for (var i = 0; i < 256; i++)
        {
            var crc = (uint)i;
            for (var j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                {
                    crc = (crc >> 1) ^ 0xEDB88320U;
                }
                else
                {
                    crc >>= 1;
                }
            }

            Table[i] = crc;
        }
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 计算字节数组的 CRC32 校验和
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Compute(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFU;

        for (var i = 0; i < data.Length; i++)
        {
            crc = (crc >> 8) ^ Table[(crc ^ data[i]) & 0xFF];
        }

        return crc ^ 0xFFFFFFFFU;
    }

    /// <summary>
    /// 计算字符串的 CRC32 校验和
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Compute(string text)
    {
        var maxBytes = System.Text.Encoding.UTF8.GetMaxByteCount(text.Length);
        Span<byte> bytes = stackalloc byte[maxBytes];
        var written = System.Text.Encoding.UTF8.GetBytes(text, bytes);
        return Compute(bytes[..written]);
    }

    /// <summary>
    /// 继续计算 CRC32，支持流式处理
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Continue(uint crc, ReadOnlySpan<byte> data)
    {
        for (var i = 0; i < data.Length; i++)
        {
            crc = (crc >> 8) ^ Table[(crc ^ data[i]) & 0xFF];
        }

        return crc;
    }

    /// <summary>
    /// 开始新的 CRC32 计算，返回初始值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Begin()
    {
        return 0xFFFFFFFFU;
    }

    /// <summary>
    /// 完成 CRC32 计算，返回最终值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint End(uint crc)
    {
        return crc ^ 0xFFFFFFFFU;
    }

    #endregion
}
