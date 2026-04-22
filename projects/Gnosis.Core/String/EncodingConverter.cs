using System;
using System.Text;

namespace Gnosis.Core.String;

/// <summary>
/// 编码转换工具类，提供 UTF-8、UTF-16、UTF-32 和 ASCII 之间的转换方法。
/// </summary>
public static class EncodingConverter
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly Encoding Utf32Bom = new UTF32Encoding(bigEndian: false, byteOrderMark: false, throwOnInvalidCharacters: true);

    #region UTF-8 <-> UTF-16

    /// <summary>
    /// 将 UTF-8 编码的字节数组转换为 UTF-16 字符串。
    /// </summary>
    /// <param name="utf8">UTF-8 编码的字节数组。</param>
    /// <returns>UTF-16 字符串。</returns>
    public static string Utf8ToUtf16(byte[] utf8)
    {
        if (utf8 is null)
        {
            throw new ArgumentNullException(nameof(utf8));
        }

        return Utf8NoBom.GetString(utf8);
    }

    /// <summary>
    /// 将 UTF-8 编码的字节范围转换为 UTF-16 字符串。
    /// </summary>
    /// <param name="utf8">UTF-8 编码的字节范围。</param>
    /// <returns>UTF-16 字符串。</returns>
    public static string Utf8ToUtf16(ReadOnlySpan<byte> utf8)
    {
        return Utf8NoBom.GetString(utf8);
    }

    /// <summary>
    /// 将 UTF-16 字符串转换为 UTF-8 编码的字节数组。
    /// </summary>
    /// <param name="text">UTF-16 字符串。</param>
    /// <returns>UTF-8 编码的字节数组。</returns>
    public static byte[] Utf16ToUtf8(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        return Utf8NoBom.GetBytes(text);
    }

    /// <summary>
    /// 将 UTF-16 字符范围转换为 UTF-8 编码的字节数组。
    /// </summary>
    /// <param name="text">UTF-16 字符范围。</param>
    /// <returns>UTF-8 编码的字节数组。</returns>
    public static byte[] Utf16ToUtf8(ReadOnlySpan<char> text)
    {
        return Utf8NoBom.GetBytes(text.ToString());
    }

    #endregion

    #region UTF-8 <-> UTF-32

    /// <summary>
    /// 将 UTF-8 编码的字节数组转换为 UTF-32 码位数组。
    /// </summary>
    /// <param name="utf8">UTF-8 编码的字节数组。</param>
    /// <returns>UTF-32 码位数组。</returns>
    public static uint[] Utf8ToUtf32(byte[] utf8)
    {
        if (utf8 is null)
        {
            throw new ArgumentNullException(nameof(utf8));
        }

        var text = Utf8NoBom.GetString(utf8);
        return Utf16ToUtf32Core(text);
    }

    /// <summary>
    /// 将 UTF-32 码位数组转换为 UTF-8 编码的字节数组。
    /// </summary>
    /// <param name="utf32">UTF-32 码位数组。</param>
    /// <returns>UTF-8 编码的字节数组。</returns>
    public static byte[] Utf32ToUtf8(uint[] utf32)
    {
        if (utf32 is null)
        {
            throw new ArgumentNullException(nameof(utf32));
        }

        var text = Utf32ToUtf16(utf32);
        return Utf8NoBom.GetBytes(text);
    }

    #endregion

    #region UTF-16 <-> UTF-32

    /// <summary>
    /// 将 UTF-16 字符串转换为 UTF-32 码位数组。
    /// </summary>
    /// <param name="text">UTF-16 字符串。</param>
    /// <returns>UTF-32 码位数组。</returns>
    public static uint[] Utf16ToUtf32(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        return Utf16ToUtf32Core(text);
    }

    /// <summary>
    /// 将 UTF-32 码位数组转换为 UTF-16 字符串。
    /// </summary>
    /// <param name="utf32">UTF-32 码位数组。</param>
    /// <returns>UTF-16 字符串。</returns>
    public static string Utf32ToUtf16(uint[] utf32)
    {
        if (utf32 is null)
        {
            throw new ArgumentNullException(nameof(utf32));
        }

        var byteCount = utf32.Length * 4;
        var bytes = new byte[byteCount];

        for (var i = 0; i < utf32.Length; i++)
        {
            var codePoint = utf32[i];
            var offset = i * 4;
            bytes[offset] = (byte)(codePoint & 0xFF);
            bytes[offset + 1] = (byte)((codePoint >> 8) & 0xFF);
            bytes[offset + 2] = (byte)((codePoint >> 16) & 0xFF);
            bytes[offset + 3] = (byte)((codePoint >> 24) & 0xFF);
        }

        return Utf32Bom.GetString(bytes);
    }

    #endregion

    #region ASCII

    /// <summary>
    /// 将 ASCII 编码的字节数组转换为 UTF-16 字符串。
    /// 将字节视为 ASCII 字符（0-127），转换为对应的 UTF-16 字符串。
    /// </summary>
    /// <param name="ascii">ASCII 编码的字节数组。</param>
    /// <returns>UTF-16 字符串。</returns>
    public static string AsciiToUtf8(byte[] ascii)
    {
        if (ascii is null)
        {
            throw new ArgumentNullException(nameof(ascii));
        }

        return Encoding.ASCII.GetString(ascii);
    }

    /// <summary>
    /// 将 UTF-8 编码的字节数组转换为 ASCII 编码的字节数组。
    /// 仅适用于 ASCII 范围内的字符（0-127），遇到非 ASCII 字符时抛出异常。
    /// </summary>
    /// <param name="utf8">UTF-8 编码的字节数组。</param>
    /// <returns>ASCII 编码的字节数组。</returns>
    public static byte[] Utf8ToAscii(byte[] utf8)
    {
        if (utf8 is null)
        {
            throw new ArgumentNullException(nameof(utf8));
        }

        var text = Utf8NoBom.GetString(utf8);

        var result = new byte[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c > 127)
            {
                throw new ArgumentException($"位置 {i} 处的字符 '{c}' 超出 ASCII 范围", nameof(utf8));
            }

            result[i] = (byte)c;
        }

        return result;
    }

    #endregion

    #region 内部辅助方法

    private static uint[] Utf16ToUtf32Core(string text)
    {
        var codePoints = new List<uint>(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            var codePoint = char.ConvertToUtf32(text, i);
            codePoints.Add((uint)codePoint);

            if (char.IsHighSurrogate(text[i]))
            {
                i++;
            }
        }

        return codePoints.ToArray();
    }

    #endregion
}
