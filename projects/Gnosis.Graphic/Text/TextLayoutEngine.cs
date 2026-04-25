using System.Numerics;

namespace Gnosis.Graphic.Text;

#region Unicode 工具

/// <summary>
/// Unicode 工具类，提供 code point 枚举和文本分段
/// </summary>
public static class UnicodeUtils
{
    /// <summary>
    /// 将字符串枚举为 Unicode code point 序列
    /// </summary>
    public static IEnumerable<uint> EnumerateCodePoints(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                uint codePoint = (uint)char.ConvertToUtf32(text[i], text[i + 1]);
                yield return codePoint;
                i++;
            }
            else
            {
                yield return text[i];
            }
        }
    }

    /// <summary>
    /// 判断 code point 是否为 CJK 字符
    /// </summary>
    public static bool IsCjk(uint codePoint)
    {
        return codePoint is >= 0x4E00 and <= 0x9FFF
            or >= 0x3400 and <= 0x4DBF
            or >= 0x20000 and <= 0x2A6DF
            or >= 0x2A700 and <= 0x2B73F
            or >= 0xF900 and <= 0xFAFF
            or >= 0x2F800 and <= 0x2FA1F;
    }

    /// <summary>
    /// 判断 code point 是否为双向文本字符（阿拉伯语、希伯来语等）
    /// </summary>
    public static bool IsRightToLeft(uint codePoint)
    {
        return codePoint is >= 0x0590 and <= 0x05FF
            or >= 0x0600 and <= 0x06FF
            or >= 0x0700 and <= 0x074F
            or >= 0xFB50 and <= 0xFDFF
            or >= 0xFE70 and <= 0xFEFF;
    }

    /// <summary>
    /// 判断 code point 是否为换行符
    /// </summary>
    public static bool IsLineBreak(uint codePoint)
    {
        return codePoint is '\n' or '\r' or 0x2028 or 0x2029;
    }

    /// <summary>
    /// 判断 code point 是否为空白字符
    /// </summary>
    public static bool IsWhitespace(uint codePoint)
    {
        return codePoint is ' ' or '\t' or 0x00A0 or 0x2000 or 0x2001
            or 0x2002 or 0x2003 or 0x2004 or 0x2005 or 0x2006
            or 0x2007 or 0x2008 or 0x2009 or 0x200A or 0x202F
            or 0x205F or 0x3000;
    }
}

#endregion

#region 文本布局

/// <summary>
/// 文本对齐方式
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// 文本布局行
/// </summary>
public sealed class TextLine
{
    public int StartIndex { get; init; }
    public int CodePointCount { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }
    public float Baseline { get; init; }
    public TextAlignment Alignment { get; init; }
}

/// <summary>
/// 文本布局结果
/// </summary>
public sealed class TextLayout
{
    public IReadOnlyList<TextLine> Lines { get; init; } = [];
    public float TotalWidth { get; init; }
    public float TotalHeight { get; init; }
    public uint[] CodePoints { get; init; } = [];
}

/// <summary>
/// 文本布局选项
/// </summary>
public sealed class TextLayoutOptions
{
    public float MaxWidth { get; set; } = float.MaxValue;
    public float LineSpacing { get; set; } = 1.2f;
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    public bool WordWrap { get; set; } = true;
}

/// <summary>
/// 多语言文本布局引擎，支持 Unicode code point、自动换行、文本对齐
/// </summary>
public sealed class TextLayoutEngine
{
    #region 布局计算

    /// <summary>
    /// 对 SDF 字体执行文本布局计算
    /// </summary>
    public TextLayout Layout(SdfFont font, string text, TextLayoutOptions? options = null)
    {
        options ??= new TextLayoutOptions();

        var codePoints = UnicodeUtils.EnumerateCodePoints(text).ToArray();
        if (codePoints.Length == 0)
        {
            return new TextLayout
            {
                CodePoints = codePoints,
                TotalWidth = 0,
                TotalHeight = font.LineHeight
            };
        }

        var lines = new List<TextLine>();
        float totalWidth = 0;
        float totalHeight = 0;

        int lineStart = 0;
        float lineWidth = 0;
        float lineHeight = font.LineHeight;
        uint prevCodePoint = 0;
        int lastWordBreak = -1;
        float lastWordBreakWidth = 0;

        for (var i = 0; i < codePoints.Length; i++)
        {
            var cp = codePoints[i];

            if (UnicodeUtils.IsLineBreak(cp))
            {
                var alignment = ComputeAlignment(lineWidth, options);
                lines.Add(new TextLine
                {
                    StartIndex = lineStart,
                    CodePointCount = i - lineStart,
                    Width = lineWidth,
                    Height = lineHeight,
                    Baseline = font.BaseLine,
                    Alignment = alignment
                });

                totalWidth = Math.Max(totalWidth, lineWidth);
                totalHeight += lineHeight * options.LineSpacing;

                lineStart = i + 1;
                lineWidth = 0;
                prevCodePoint = 0;
                lastWordBreak = -1;
                lastWordBreakWidth = 0;
                continue;
            }

            var glyph = font.GetGlyph(cp);
            float advance = glyph?.XAdvance ?? font.LineHeight * 0.5f;

            if (prevCodePoint != 0 && glyph != null)
            {
                lineWidth += font.GetKerning(prevCodePoint, cp);
            }

            if (options.WordWrap && UnicodeUtils.IsWhitespace(cp))
            {
                lastWordBreak = i;
                lastWordBreakWidth = lineWidth;
            }

            lineWidth += advance;
            prevCodePoint = cp;

            if (options.WordWrap && lineWidth > options.MaxWidth && lastWordBreak > lineStart)
            {
                var alignment = ComputeAlignment(lastWordBreakWidth, options);
                lines.Add(new TextLine
                {
                    StartIndex = lineStart,
                    CodePointCount = lastWordBreak - lineStart,
                    Width = lastWordBreakWidth,
                    Height = lineHeight,
                    Baseline = font.BaseLine,
                    Alignment = alignment
                });

                totalWidth = Math.Max(totalWidth, lastWordBreakWidth);
                totalHeight += lineHeight * options.LineSpacing;

                lineStart = lastWordBreak + 1;
                lineWidth = 0;
                prevCodePoint = 0;
                lastWordBreak = -1;
                lastWordBreakWidth = 0;

                for (var j = lineStart; j <= i; j++)
                {
                    var cpJ = codePoints[j];
                    var glyphJ = font.GetGlyph(cpJ);
                    float advJ = glyphJ?.XAdvance ?? font.LineHeight * 0.5f;

                    if (prevCodePoint != 0 && glyphJ != null)
                    {
                        lineWidth += font.GetKerning(prevCodePoint, cpJ);
                    }

                    lineWidth += advJ;
                    prevCodePoint = cpJ;
                }
            }
        }

        if (lineStart < codePoints.Length)
        {
            var alignment = ComputeAlignment(lineWidth, options);
            lines.Add(new TextLine
            {
                StartIndex = lineStart,
                CodePointCount = codePoints.Length - lineStart,
                Width = lineWidth,
                Height = lineHeight,
                Baseline = font.BaseLine,
                Alignment = alignment
            });

            totalWidth = Math.Max(totalWidth, lineWidth);
            totalHeight += lineHeight;
        }

        return new TextLayout
        {
            Lines = lines,
            CodePoints = codePoints,
            TotalWidth = totalWidth,
            TotalHeight = totalHeight
        };
    }

    /// <summary>
    /// 对位图字体执行文本布局计算
    /// </summary>
    public TextLayout Layout(BitmapFont font, string text, TextLayoutOptions? options = null)
    {
        options ??= new TextLayoutOptions();

        var codePoints = UnicodeUtils.EnumerateCodePoints(text).ToArray();
        if (codePoints.Length == 0)
        {
            return new TextLayout
            {
                CodePoints = codePoints,
                TotalWidth = 0,
                TotalHeight = font.LineHeight
            };
        }

        var lines = new List<TextLine>();
        float totalWidth = 0;
        float totalHeight = 0;

        int lineStart = 0;
        float lineWidth = 0;
        float lineHeight = font.LineHeight;
        uint prevCodePoint = 0;
        int lastWordBreak = -1;
        float lastWordBreakWidth = 0;

        for (var i = 0; i < codePoints.Length; i++)
        {
            var cp = codePoints[i];

            if (UnicodeUtils.IsLineBreak(cp))
            {
                var alignment = ComputeAlignment(lineWidth, options);
                lines.Add(new TextLine
                {
                    StartIndex = lineStart,
                    CodePointCount = i - lineStart,
                    Width = lineWidth,
                    Height = lineHeight,
                    Baseline = 0,
                    Alignment = alignment
                });

                totalWidth = Math.Max(totalWidth, lineWidth);
                totalHeight += lineHeight * options.LineSpacing;

                lineStart = i + 1;
                lineWidth = 0;
                prevCodePoint = 0;
                lastWordBreak = -1;
                lastWordBreakWidth = 0;
                continue;
            }

            var glyph = font.GetGlyph(cp);
            float advance = glyph?.XAdvance ?? font.LineHeight * 0.5f;

            if (prevCodePoint != 0 && glyph != null)
            {
                lineWidth += font.GetKerning(prevCodePoint, cp);
            }

            if (options.WordWrap && UnicodeUtils.IsWhitespace(cp))
            {
                lastWordBreak = i;
                lastWordBreakWidth = lineWidth;
            }

            lineWidth += advance;
            prevCodePoint = cp;

            if (options.WordWrap && lineWidth > options.MaxWidth && lastWordBreak > lineStart)
            {
                var alignment = ComputeAlignment(lastWordBreakWidth, options);
                lines.Add(new TextLine
                {
                    StartIndex = lineStart,
                    CodePointCount = lastWordBreak - lineStart,
                    Width = lastWordBreakWidth,
                    Height = lineHeight,
                    Baseline = 0,
                    Alignment = alignment
                });

                totalWidth = Math.Max(totalWidth, lastWordBreakWidth);
                totalHeight += lineHeight * options.LineSpacing;

                lineStart = lastWordBreak + 1;
                lineWidth = 0;
                prevCodePoint = 0;
                lastWordBreak = -1;
                lastWordBreakWidth = 0;

                for (var j = lineStart; j <= i; j++)
                {
                    var cpJ = codePoints[j];
                    var glyphJ = font.GetGlyph(cpJ);
                    float advJ = glyphJ?.XAdvance ?? font.LineHeight * 0.5f;

                    if (prevCodePoint != 0 && glyphJ != null)
                    {
                        lineWidth += font.GetKerning(prevCodePoint, cpJ);
                    }

                    lineWidth += advJ;
                    prevCodePoint = cpJ;
                }
            }
        }

        if (lineStart < codePoints.Length)
        {
            var alignment = ComputeAlignment(lineWidth, options);
            lines.Add(new TextLine
            {
                StartIndex = lineStart,
                CodePointCount = codePoints.Length - lineStart,
                Width = lineWidth,
                Height = lineHeight,
                Baseline = 0,
                Alignment = alignment
            });

            totalWidth = Math.Max(totalWidth, lineWidth);
            totalHeight += lineHeight;
        }

        return new TextLayout
        {
            Lines = lines,
            CodePoints = codePoints,
            TotalWidth = totalWidth,
            TotalHeight = totalHeight
        };
    }

    #endregion

    #region 辅助方法

    private static TextAlignment ComputeAlignment(float lineWidth, TextLayoutOptions options)
    {
        return options.Alignment;
    }

    #endregion
}

#endregion
