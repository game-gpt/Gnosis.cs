using System.Numerics;
using Gnosis.Graphic.RHI;
using Gnosis.Graphic.Sprite2D;

namespace Gnosis.Graphic.Text;

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

    public void DrawString(SdfFont font, string text, Vector2 position, in Vector4 color, float scale = 1.0f, float rotation = 0.0f, float outlineWidth = 0.0f, in Vector4 outlineColor = default)
    {
        if (string.IsNullOrEmpty(text) || font.AtlasTexture == null)
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

            var glyphPos = new Vector2(cursorX + glyph.Offset.X * scale, cursorY + glyph.Offset.Y * scale);
            var glyphSize = glyph.Size * scale;

            _spriteBatch.Draw(
                font.AtlasTexture,
                new Rectangle(glyphPos.X, glyphPos.Y, glyphSize.X, glyphSize.Y),
                color);

            cursorX += glyph.XAdvance * scale;
            prevCodePoint = codePoint;
        }

        _spriteBatch.End();
    }

    #endregion
}
