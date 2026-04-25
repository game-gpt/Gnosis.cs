using System.Numerics;
using Gnosis.Graphic.Text;

namespace Gnosis.Graphic.UI;

public enum TextAlignment
{
    Left,
    Center,
    Right
}

public enum TextWrapMode
{
    None,
    WordWrap,
    CharacterWrap
}

public sealed class TextLayoutEngine
{
    #region 内部状态

    private SdfFont? _font;
    private float _fontSize = 16.0f;
    private float _lineSpacing = 1.2f;
    private float _maxWidth = float.MaxValue;
    private TextAlignment _alignment = TextAlignment.Left;
    private TextWrapMode _wrapMode = TextWrapMode.WordWrap;

    #endregion

    #region 属性

    public SdfFont? Font
    {
        get => _font;
        set => _font = value;
    }

    public float FontSize
    {
        get => _fontSize;
        set => _fontSize = Math.Max(1.0f, value);
    }

    public float LineSpacing
    {
        get => _lineSpacing;
        set => _lineSpacing = Math.Max(0.5f, value);
    }

    public float MaxWidth
    {
        get => _maxWidth;
        set => _maxWidth = Math.Max(0.0f, value);
    }

    public TextAlignment Alignment
    {
        get => _alignment;
        set => _alignment = value;
    }

    public TextWrapMode WrapMode
    {
        get => _wrapMode;
        set => _wrapMode = value;
    }

    #endregion

    #region 公开方法

    public TextLayoutResult Layout(string text)
    {
        if (_font is null || string.IsNullOrEmpty(text))
        {
            return new TextLayoutResult([], Vector2.Zero, 0);
        }

        var scale = _fontSize / _font.FontSize;
        var lineHeight = _font.LineHeight * scale * _lineSpacing;
        var lines = new List<TextLine>();
        var currentLineGlyphs = new List<TextGlyphPosition>();
        var cursorX = 0.0f;
        var lineStartIndex = 0;
        var lastWordBreakIndex = -1;
        var lastWordBreakX = 0.0f;

        for (var i = 0; i < text.Length; i++)
        {
            var codePoint = char.ConvertToUtf32(text, i);
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length)
            {
                i++;
            }

            if (codePoint == '\n')
            {
                lines.Add(CreateLine(currentLineGlyphs, lineHeight, cursorX));
                currentLineGlyphs = new List<TextGlyphPosition>();
                cursorX = 0.0f;
                lineStartIndex = i + 1;
                lastWordBreakIndex = -1;
                lastWordBreakX = 0.0f;
                continue;
            }

            var glyph = _font.GetGlyph((uint)codePoint);
            if (glyph is null)
            {
                cursorX += _font.FontSize * scale * 0.3f;
                continue;
            }

            var kerning = 0.0f;
            if (currentLineGlyphs.Count > 0)
            {
                var prevCodePoint = currentLineGlyphs[^1].CodePoint;
                kerning = _font.GetKerning(prevCodePoint, (uint)codePoint);
            }

            cursorX += kerning * scale;

            if (char.IsWhiteSpace((char)codePoint))
            {
                lastWordBreakIndex = currentLineGlyphs.Count;
                lastWordBreakX = cursorX;
            }

            var needsWrap = _wrapMode != TextWrapMode.None && cursorX + glyph.XAdvance * scale > _maxWidth;

            if (needsWrap && currentLineGlyphs.Count > 0)
            {
                if (_wrapMode == TextWrapMode.WordWrap && lastWordBreakIndex > 0)
                {
                    var wrappedGlyphs = currentLineGlyphs.GetRange(lastWordBreakIndex, currentLineGlyphs.Count - lastWordBreakIndex);
                    currentLineGlyphs.RemoveRange(lastWordBreakIndex, currentLineGlyphs.Count - lastWordBreakIndex);
                    lines.Add(CreateLine(currentLineGlyphs, lineHeight, lastWordBreakX));
                    currentLineGlyphs = wrappedGlyphs;
                    cursorX = 0.0f;
                    foreach (var g in currentLineGlyphs)
                    {
                        cursorX = g.X + g.AdvanceX;
                    }
                }
                else
                {
                    lines.Add(CreateLine(currentLineGlyphs, lineHeight, cursorX));
                    currentLineGlyphs = new List<TextGlyphPosition>();
                    cursorX = 0.0f;
                }

                lastWordBreakIndex = -1;
                lastWordBreakX = 0.0f;
            }

            var glyphPos = new TextGlyphPosition
            {
                CodePoint = (uint)codePoint,
                CharIndex = i,
                X = cursorX,
                Y = 0,
                Width = glyph.Size.X * scale,
                Height = glyph.Size.Y * scale,
                AdvanceX = glyph.XAdvance * scale,
                UVOffset = new Vector2(glyph.UVOffset.X, glyph.UVOffset.Y),
                UVSize = new Vector2(glyph.UVSize.X, glyph.UVSize.Y),
                Offset = new Vector2(glyph.Offset.X * scale, glyph.Offset.Y * scale)
            };

            currentLineGlyphs.Add(glyphPos);
            cursorX += glyph.XAdvance * scale;
        }

        if (currentLineGlyphs.Count > 0)
        {
            lines.Add(CreateLine(currentLineGlyphs, lineHeight, cursorX));
        }

        var totalHeight = lines.Count * lineHeight;
        var maxWidth = 0.0f;
        foreach (var line in lines)
        {
            if (line.Width > maxWidth)
            {
                maxWidth = line.Width;
            }
        }

        return new TextLayoutResult(lines, new Vector2(maxWidth, totalHeight), lines.Count);
    }

    public Vector2 Measure(string text)
    {
        return Layout(text).Size;
    }

    public int GetCharIndexAtPosition(string text, Vector2 position)
    {
        var layout = Layout(text);
        if (layout.Lines.Count == 0)
        {
            return 0;
        }

        var scale = _fontSize / _font!.FontSize;
        var lineHeight = _font.LineHeight * scale * _lineSpacing;

        var lineIndex = (int)(position.Y / lineHeight);
        lineIndex = Math.Clamp(lineIndex, 0, layout.Lines.Count - 1);

        var line = layout.Lines[lineIndex];

        foreach (var glyph in line.Glyphs)
        {
            if (position.X < glyph.X + glyph.AdvanceX * 0.5f)
            {
                return glyph.CharIndex;
            }
        }

        return text.Length;
    }

    #endregion

    #region 私有方法

    private TextLine CreateLine(List<TextGlyphPosition> glyphs, float lineHeight, float width)
    {
        ApplyAlignment(glyphs, width);
        return new TextLine(glyphs.ToArray(), width, lineHeight);
    }

    private void ApplyAlignment(List<TextGlyphPosition> glyphs, float lineWidth)
    {
        if (_alignment == TextAlignment.Left || glyphs.Count == 0)
        {
            return;
        }

        float offset;
        if (_alignment == TextAlignment.Center)
        {
            offset = (_maxWidth - lineWidth) * 0.5f;
        }
        else
        {
            offset = _maxWidth - lineWidth;
        }

        foreach (var glyph in glyphs)
        {
            glyph.X += offset;
        }
    }

    #endregion
}

public sealed class TextLayoutResult
{
    public IReadOnlyList<TextLine> Lines { get; }
    public Vector2 Size { get; }
    public int LineCount { get; }

    public TextLayoutResult(IReadOnlyList<TextLine> lines, Vector2 size, int lineCount)
    {
        Lines = lines;
        Size = size;
        LineCount = lineCount;
    }
}

public sealed class TextLine
{
    public IReadOnlyList<TextGlyphPosition> Glyphs { get; }
    public float Width { get; }
    public float Height { get; }

    public TextLine(IReadOnlyList<TextGlyphPosition> glyphs, float width, float height)
    {
        Glyphs = glyphs;
        Width = width;
        Height = height;
    }
}

public sealed class TextGlyphPosition
{
    public uint CodePoint { get; set; }
    public int CharIndex { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float AdvanceX { get; set; }
    public Vector2 UVOffset { get; set; }
    public Vector2 UVSize { get; set; }
    public Vector2 Offset { get; set; }
}
