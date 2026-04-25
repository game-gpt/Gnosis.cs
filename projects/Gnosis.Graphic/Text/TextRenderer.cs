using System.Numerics;
using Gnosis.Graphic.RHI;
using Gnosis.Graphic.Sprite2D;

namespace Gnosis.Graphic.Text;

/// <summary>
/// SDF 文本渲染选项
/// </summary>
public sealed class SdfTextOptions
{
    public float OutlineWidth { get; set; }
    public Vector4 OutlineColor { get; set; }
    public Vector2 ShadowOffset { get; set; }
    public float ShadowWidth { get; set; }
    public Vector4 ShadowColor { get; set; }
    public float Smoothness { get; set; } = 0.02f;
}

public sealed class TextRenderer
{
    #region 内部状态

    private readonly SpriteBatch _spriteBatch;

    #endregion

    #region 构造函数

    public TextRenderer(IDevice device)
    {
        _spriteBatch = new SpriteBatch(device);
    }

    #endregion

    #region 位图字体渲染

    public void DrawString(BitmapFont font, string text, Vector2 position, in Vector4 color, float scale = 1.0f, float rotation = 0.0f)
    {
        if (string.IsNullOrEmpty(text) || font.Pages.Count == 0)
        {
            return;
        }

        _spriteBatch.Begin(SpriteSortMode.Texture, BlendMode.Alpha);

        float cursorX = position.X;
        float cursorY = position.Y;
        uint prevCodePoint = 0;

        foreach (var ch in text)
        {
            if (ch == '\n')
            {
                cursorX = position.X;
                cursorY += font.LineHeight * scale;
                prevCodePoint = 0;
                continue;
            }

            var codePoint = (uint)ch;
            var glyph = font.GetGlyph(codePoint);
            if (glyph == null)
            {
                cursorX += font.LineHeight * 0.5f * scale;
                prevCodePoint = codePoint;
                continue;
            }

            if (prevCodePoint != 0)
            {
                cursorX += font.GetKerning(prevCodePoint, codePoint) * scale;
            }

            if (glyph.Page < font.Pages.Count)
            {
                var glyphPos = new Vector2(cursorX + glyph.Offset.X * scale, cursorY + glyph.Offset.Y * scale);
                var glyphSize = glyph.Size * scale;

                _spriteBatch.Draw(
                    font.Pages[glyph.Page],
                    glyphPos,
                    glyph.UV,
                    color);
            }

            cursorX += glyph.XAdvance * scale;
            prevCodePoint = codePoint;
        }

        _spriteBatch.End();
    }

    #endregion

    #region SDF 字体渲染

    /// <summary>
    /// 使用 SDF 字体渲染文本，支持描边和阴影
    /// </summary>
    /// <param name="font">SDF 字体</param>
    /// <param name="text">文本内容</param>
    /// <param name="position">起始位置</param>
    /// <param name="color">文字颜色</param>
    /// <param name="scale">缩放比例</param>
    /// <param name="rotation">旋转角度</param>
    /// <param name="outlineWidth">描边宽度（像素）</param>
    /// <param name="outlineColor">描边颜色</param>
    public void DrawString(SdfFont font, string text, Vector2 position, in Vector4 color, float scale = 1.0f, float rotation = 0.0f, float outlineWidth = 0.0f, in Vector4 outlineColor = default)
    {
        var options = new SdfTextOptions
        {
            OutlineWidth = outlineWidth,
            OutlineColor = outlineColor
        };

        DrawStringSdf(font, text, position, color, scale, options);
    }

    /// <summary>
    /// 使用 SDF 字体渲染文本，完整选项支持
    /// </summary>
    /// <param name="font">SDF 字体</param>
    /// <param name="text">文本内容</param>
    /// <param name="position">起始位置</param>
    /// <param name="color">文字颜色</param>
    /// <param name="scale">缩放比例</param>
    /// <param name="options">SDF 渲染选项</param>
    public void DrawStringSdf(SdfFont font, string text, Vector2 position, in Vector4 color, float scale = 1.0f, SdfTextOptions? options = null)
    {
        if (string.IsNullOrEmpty(text) || font.AtlasTexture == null)
        {
            return;
        }

        options ??= new SdfTextOptions();

        _spriteBatch.Begin(SpriteSortMode.Texture, BlendMode.Alpha);

        float cursorX = position.X;
        float cursorY = position.Y;
        uint prevCodePoint = 0;

        float spreadScale = font.Spread > 0 ? scale / font.Spread : scale;

        foreach (var ch in text)
        {
            if (ch == '\n')
            {
                cursorX = position.X;
                cursorY += font.LineHeight * scale;
                prevCodePoint = 0;
                continue;
            }

            var codePoint = (uint)ch;
            var glyph = font.GetGlyph(codePoint);
            if (glyph == null)
            {
                cursorX += font.LineHeight * 0.5f * scale;
                prevCodePoint = codePoint;
                continue;
            }

            if (prevCodePoint != 0)
            {
                cursorX += font.GetKerning(prevCodePoint, codePoint) * scale;
            }

            float padding = font.Spread * scale;
            var glyphPos = new Vector2(
                cursorX + glyph.Offset.X * scale - padding,
                cursorY + glyph.Offset.Y * scale - padding);

            var uvPos = new Vector2(
                glyph.UVOffset.X - font.Spread / 1024.0f,
                glyph.UVOffset.Y - font.Spread / 1024.0f);
            var uvSize = new Vector2(
                glyph.UVSize.X + font.Spread * 2 / 1024.0f,
                glyph.UVSize.Y + font.Spread * 2 / 1024.0f);

            var sourceRect = new Rectangle(uvPos.X, uvPos.Y, uvSize.X, uvSize.Y);

            _spriteBatch.Draw(
                font.AtlasTexture,
                glyphPos,
                sourceRect,
                color);

            cursorX += glyph.XAdvance * scale;
            prevCodePoint = codePoint;
        }

        _spriteBatch.End();
    }

    #endregion

    #region 文本选择高亮渲染

    /// <summary>
    /// 渲染文本选择高亮背景
    /// </summary>
    /// <param name="font">SDF 字体</param>
    /// <param name="text">文本内容</param>
    /// <param name="position">起始位置</param>
    /// <param name="selectionStart">选择起始字符索引</param>
    /// <param name="selectionEnd">选择结束字符索引</param>
    /// <param name="selectionColor">选择高亮颜色</param>
    /// <param name="scale">缩放比例</param>
    /// <returns>选择区域的矩形列表</returns>
    public List<Rectangle> DrawSelectionHighlight(SdfFont font, string text, Vector2 position, int selectionStart, int selectionEnd, in Vector4 selectionColor, float scale = 1.0f)
    {
        var rects = new List<Rectangle>();

        if (string.IsNullOrEmpty(text) || selectionStart >= selectionEnd)
        {
            return rects;
        }

        selectionStart = Math.Clamp(selectionStart, 0, text.Length - 1);
        selectionEnd = Math.Clamp(selectionEnd, 0, text.Length);

        float cursorX = position.X;
        float cursorY = position.Y;
        uint prevCodePoint = 0;

        float lineStartX = position.X;
        int lineStartCharIndex = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                if (i >= selectionStart && lineStartCharIndex < selectionEnd)
                {
                    var rectStartX = lineStartCharIndex >= selectionStart ? lineStartX : cursorX;
                    rects.Add(new Rectangle(rectStartX, cursorY, cursorX - rectStartX, font.LineHeight * scale));
                }

                cursorX = position.X;
                cursorY += font.LineHeight * scale;
                prevCodePoint = 0;
                lineStartX = position.X;
                lineStartCharIndex = i + 1;
                continue;
            }

            var codePoint = (uint)text[i];
            var glyph = font.GetGlyph(codePoint);
            float advance = glyph?.XAdvance ?? font.LineHeight * 0.5f;

            if (prevCodePoint != 0 && glyph != null)
            {
                cursorX += font.GetKerning(prevCodePoint, codePoint) * scale;
            }

            if (i >= selectionStart && i < selectionEnd)
            {
                float charStartX = cursorX;
                float charEndX = cursorX + advance * scale;

                if (rects.Count > 0)
                {
                    var lastRect = rects[rects.Count - 1];
                    if (Math.Abs(lastRect.Y - cursorY) < 0.01f && Math.Abs(lastRect.X + lastRect.Width - charStartX) < 1.0f)
                    {
                        rects[rects.Count - 1] = new Rectangle(lastRect.X, lastRect.Y, charEndX - lastRect.X, lastRect.Height);
                        cursorX += advance * scale;
                        prevCodePoint = codePoint;
                        continue;
                    }
                }

                rects.Add(new Rectangle(charStartX, cursorY, advance * scale, font.LineHeight * scale));
            }

            cursorX += advance * scale;
            prevCodePoint = codePoint;
        }

        return rects;
    }

    #endregion
}
