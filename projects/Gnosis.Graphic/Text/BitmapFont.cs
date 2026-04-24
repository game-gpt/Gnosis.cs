using System.Numerics;
using Gnosis.Graphic.RHI;
using Gnosis.Graphic.Sprite2D;

namespace Gnosis.Graphic.Text;

public record GlyphUV
{
    public float U0 { get; init; }
    public float V0 { get; init; }
    public float U1 { get; init; }
    public float V1 { get; init; }
}

public sealed class BitmapFont
{
    #region 属性

    public string Name { get; }
    public float LineHeight { get; }
    public float BaseLine { get; }
    public IReadOnlyDictionary<uint, GlyphInfo> Glyphs => _glyphs;
    public IReadOnlyList<IResource> Pages => _pages;

    #endregion

    #region 内部状态

    private readonly Dictionary<uint, GlyphInfo> _glyphs = [];
    private readonly List<IResource> _pages = [];
    private readonly Dictionary<(uint, uint), float> _kernings = [];

    #endregion

    #region 构造函数

    public BitmapFont(string name, float lineHeight, float baseLine)
    {
        Name = name;
        LineHeight = lineHeight;
        BaseLine = baseLine;
    }

    #endregion

    #region 字形管理

    public void AddGlyph(GlyphInfo glyph)
    {
        _glyphs[glyph.CodePoint] = glyph;
    }

    public void AddKerning(uint left, uint right, float offset)
    {
        _kernings[(left, right)] = offset;
    }

    public void AddPage(IResource texture)
    {
        _pages.Add(texture);
    }

    public GlyphInfo? GetGlyph(uint codePoint)
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
