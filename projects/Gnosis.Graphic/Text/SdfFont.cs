using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Text;

public sealed class SdfFont
{
    #region 属性

    public string Name { get; }
    public float LineHeight { get; }
    public float BaseLine { get; }
    public float FontSize { get; }
    public float Spread { get; }
    public IReadOnlyDictionary<uint, SdfGlyphInfo> Glyphs => _glyphs;
    public IResource? AtlasTexture { get; set; }

    #endregion

    #region 内部状态

    private readonly Dictionary<uint, SdfGlyphInfo> _glyphs = [];
    private readonly Dictionary<(uint, uint), float> _kernings = [];

    #endregion

    #region 构造函数

    public SdfFont(string name, float lineHeight, float baseLine, float fontSize, float spread)
    {
        Name = name;
        LineHeight = lineHeight;
        BaseLine = baseLine;
        FontSize = fontSize;
        Spread = spread;
    }

    #endregion

    #region 字形管理

    public void AddGlyph(SdfGlyphInfo glyph)
    {
        _glyphs[glyph.CodePoint] = glyph;
    }

    public void AddKerning(uint left, uint right, float offset)
    {
        _kernings[(left, right)] = offset;
    }

    public SdfGlyphInfo? GetGlyph(uint codePoint)
    {
        return _glyphs.GetValueOrDefault(codePoint);
    }

    public float GetKerning(uint left, uint right)
    {
        return _kernings.GetValueOrDefault((left, right), 0.0f);
    }

    #endregion

    #region 文本测量

    public Vector2 MeasureText(string text, float scale = 1.0f)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Vector2.Zero;
        }

        float width = 0;
        float height = LineHeight;
        float lineWidth = 0;
        uint prevCodePoint = 0;

        foreach (var ch in text)
        {
            if (ch == '\n')
            {
                width = Math.Max(width, lineWidth);
                lineWidth = 0;
                height += LineHeight;
                prevCodePoint = 0;
                continue;
            }

            var codePoint = (uint)ch;
            var glyph = GetGlyph(codePoint);
            if (glyph == null)
            {
                lineWidth += LineHeight * 0.5f;
                prevCodePoint = codePoint;
                continue;
            }

            if (prevCodePoint != 0)
            {
                lineWidth += GetKerning(prevCodePoint, codePoint);
            }

            lineWidth += glyph.XAdvance;
            prevCodePoint = codePoint;
        }

        width = Math.Max(width, lineWidth);
        return new Vector2(width * scale, height * scale);
    }

    #endregion
}

public record SdfGlyphInfo
{
    public uint CodePoint { get; init; }
    public float XAdvance { get; init; }
    public Vector2 Offset { get; init; }
    public Vector2 Size { get; init; }
    public Vector2 UVOffset { get; init; }
    public Vector2 UVSize { get; init; }
}
