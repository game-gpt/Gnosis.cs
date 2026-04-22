using System.Runtime.CompilerServices;

namespace Gnosis.Core.Hash;

/// <summary>
/// SHA256 加密哈希算法，使用 System.Security.Cryptography 实现
/// </summary>
public static class Sha256
{
    #region 公开方法

    /// <summary>
    /// 计算字节数组的 SHA256 哈希值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Compute(ReadOnlySpan<byte> data)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        return sha.ComputeHash(data.ToArray());
    }

    /// <summary>
    /// 计算字符串的 SHA256 哈希值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Compute(string text)
    {
        var maxBytes = System.Text.Encoding.UTF8.GetMaxByteCount(text.Length);
        Span<byte> bytes = stackalloc byte[maxBytes];
        var written = System.Text.Encoding.UTF8.GetBytes(text, bytes);
        return Compute(bytes[..written]);
    }

    /// <summary>
    /// 计算字节数组的 SHA256 哈希值并返回十六进制字符串
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ComputeHex(ReadOnlySpan<byte> data)
    {
        var hash = Compute(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 计算字符串的 SHA256 哈希值并返回十六进制字符串
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ComputeHex(string text)
    {
        var hash = Compute(text);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    #endregion
}
