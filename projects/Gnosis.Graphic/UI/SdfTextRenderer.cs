using System.Numerics;
using Gnosis.Graphic.Text;

namespace Gnosis.Graphic.UI;

public sealed class SdfTextRenderer
{
    #region 内部状态

    private readonly GpuWidgetRenderer _renderer;
    private SdfFont? _font;
    private float _scale = 1.0f;

    #endregion

    #region 属性

    public SdfFont? Font
    {
        get => _font;
        set
        {
            _font = value;
            _scale = 1.0f;
        }
    }

    public float Scale
    {
        get => _scale;
        set => _scale = Math.Max(0.001f, value);
    }

    #endregion

    #region 构造函数

    public SdfTextRenderer(GpuWidgetRenderer renderer)
    {
        _renderer = renderer;
    }

    #endregion

    #region 公开方法

    public void DrawString(string text, float x, float y, float fontSize, in Vector4 color)
    {
        if (_font is null || string.IsNullOrEmpty(text))
        {
            return;
        }

        var scale = fontSize / _font.FontSize * _scale;
        var cursorX = x;
        var cursorY = y;

        for (var i = 0; i < text.Length; i++)
        {
            var codePoint = char.ConvertToUtf32(text, i);
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length)
            {
                i++;
            }

            if (codePoint == '\n')
            {
                cursorX = x;
                cursorY += _font.LineHeight * scale;
                continue;
            }

            var glyph = _font.GetGlyph((uint)codePoint);
            if (glyph is null)
            {
                cursorX += _font.FontSize * scale * 0.3f;
                continue;
            }

            var kerning = 0.0f;
            if (i > 0)
            {
                var prevCodePoint = char.ConvertToUtf32(text, i - 1);
                kerning = _font.GetKerning((uint)prevCodePoint, (uint)codePoint);
            }

            cursorX += kerning * scale;

            var glyphX = cursorX + glyph.Offset.X * scale;
            var glyphY = cursorY + glyph.Offset.Y * scale;
            var glyphW = glyph.Size.X * scale;
            var glyphH = glyph.Size.Y * scale;

            _renderer.DrawSdfGlyph(
                glyphX, glyphY, glyphW, glyphH,
                new Vector2(glyph.UVOffset.X, glyph.UVOffset.Y),
                new Vector2(glyph.UVSize.X, glyph.UVSize.Y),
                color.X, color.Y, color.Z, color.W);

            cursorX += glyph.XAdvance * scale;
        }
    }

    public Vector2 MeasureText(string text, float fontSize)
    {
        if (_font is null || string.IsNullOrEmpty(text))
        {
            return Vector2.Zero;
        }

        return _font.MeasureText(text, fontSize / _font.FontSize * _scale);
    }

    public void DrawStringWithSelection(
        string text, float x, float y, float fontSize,
        in Vector4 textColor, in Vector4 selectionColor, in Vector4 selectedTextColor,
        int selectionStart, int selectionEnd)
    {
        if (_font is null || string.IsNullOrEmpty(text))
        {
            return;
        }

        var scale = fontSize / _font.FontSize * _scale;
        var cursorX = x;
        var cursorY = y;
        var lineHeight = _font.LineHeight * scale;

        for (var i = 0; i < text.Length; i++)
        {
            var codePoint = char.ConvertToUtf32(text, i);
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length)
            {
                i++;
            }

            if (codePoint == '\n')
            {
                cursorX = x;
                cursorY += lineHeight;
                continue;
            }

            var glyph = _font.GetGlyph((uint)codePoint);
            if (glyph is null)
            {
                cursorX += _font.FontSize * scale * 0.3f;
                continue;
            }

            var kerning = 0.0f;
            if (i > 0)
            {
                var prevCodePoint = char.ConvertToUtf32(text, i - 1);
                kerning = _font.GetKerning((uint)prevCodePoint, (uint)codePoint);
            }

            cursorX += kerning * scale;

            var isSelected = i >= selectionStart && i < selectionEnd;
            if (isSelected)
            {
                _renderer.DrawRect(
                    cursorX, cursorY,
                    glyph.XAdvance * scale, lineHeight,
                    selectionColor.X, selectionColor.Y, selectionColor.Z, selectionColor.W);
            }

            var glyphX = cursorX + glyph.Offset.X * scale;
            var glyphY = cursorY + glyph.Offset.Y * scale;
            var glyphW = glyph.Size.X * scale;
            var glyphH = glyph.Size.Y * scale;

            var color = isSelected ? selectedTextColor : textColor;
            _renderer.DrawSdfGlyph(
                glyphX, glyphY, glyphW, glyphH,
                new Vector2(glyph.UVOffset.X, glyph.UVOffset.Y),
                new Vector2(glyph.UVSize.X, glyph.UVSize.Y),
                color.X, color.Y, color.Z, color.W);

            cursorX += glyph.XAdvance * scale;
        }
    }

    #endregion
}
