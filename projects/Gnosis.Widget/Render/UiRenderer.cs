using Gnosis.Widget.Element;

namespace Gnosis.Widget.Render;

public sealed class UiRenderer : IWidgetRenderer
{
    #region 字段

    private readonly byte[] _framebuffer;
    private readonly Stack<Rect> _clipStack = new();

    #endregion

    #region 属性

    public int Width { get; }
    public int Height { get; }

    #endregion

    #region 构造函数

    public UiRenderer(int width, int height)
    {
        Width = width;
        Height = height;
        _framebuffer = new byte[width * height * 4];
    }

    #endregion

    #region 生命周期

    public void Begin()
    {
        _clipStack.Clear();
    }

    public void End()
    {
    }

    #endregion

    #region 清屏

    public void Clear(float r, float g, float b, float a = 1.0f)
    {
        var bb = ToByte(b);
        var gb = ToByte(g);
        var rb = ToByte(r);
        var ab = ToByte(a);

        for (var i = 0; i < _framebuffer.Length; i += 4)
        {
            _framebuffer[i] = bb;
            _framebuffer[i + 1] = gb;
            _framebuffer[i + 2] = rb;
            _framebuffer[i + 3] = ab;
        }
    }

    #endregion

    #region 绘制矩形

    public void DrawRect(float x, float y, float width, float height, float r, float g, float b, float a = 1.0f)
    {
        var clip = GetCurrentClip();

        var x0 = Math.Max((int)x, (int)clip.X);
        var y0 = Math.Max((int)y, (int)clip.Y);
        var x1 = Math.Min((int)(x + width), (int)(clip.X + clip.Width));
        var y1 = Math.Min((int)(y + height), (int)(clip.Y + clip.Height));

        if (x0 >= x1 || y0 >= y1)
        {
            return;
        }

        if (a >= 1.0f)
        {
            var bb = ToByte(b);
            var gb = ToByte(g);
            var rb = ToByte(r);
            var ab = ToByte(a);

            for (var py = y0; py < y1; py++)
            {
                var rowOffset = py * Width * 4;
                for (var px = x0; px < x1; px++)
                {
                    var offset = rowOffset + px * 4;
                    _framebuffer[offset] = bb;
                    _framebuffer[offset + 1] = gb;
                    _framebuffer[offset + 2] = rb;
                    _framebuffer[offset + 3] = ab;
                }
            }
        }
        else
        {
            for (var py = y0; py < y1; py++)
            {
                var rowOffset = py * Width * 4;
                for (var px = x0; px < x1; px++)
                {
                    BlendPixel(rowOffset + px * 4, r, g, b, a);
                }
            }
        }
    }

    #endregion

    #region 绘制文本

    public void DrawText(string text, float x, float y, float size, float r, float g, float b)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var scale = size / BitmapFont.GlyphSize;
        var cursorX = x;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            var glyph = BitmapFont.GetGlyph(ch);

            var glyphW = BitmapFont.GlyphSize * scale;
            var glyphH = BitmapFont.GlyphSize * scale;

            DrawGlyph(glyph, cursorX, y, glyphW, glyphH, r, g, b);

            cursorX += glyphW;
        }
    }

    #endregion

    #region 绘制线段

    public void DrawLine(float x1, float y1, float x2, float y2, float r, float g, float b, float a = 1.0f, float thickness = 1.0f)
    {
        if (Math.Abs(y2 - y1) < 0.5f)
        {
            var halfThick = thickness * 0.5f;
            DrawRect(
                Math.Min(x1, x2), y1 - halfThick,
                Math.Abs(x2 - x1), thickness,
                r, g, b, a
            );
        }
        else if (Math.Abs(x2 - x1) < 0.5f)
        {
            var halfThick = thickness * 0.5f;
            DrawRect(
                x1 - halfThick, Math.Min(y1, y2),
                thickness, Math.Abs(y2 - y1),
                r, g, b, a
            );
        }
    }

    #endregion

    #region 裁剪

    public void PushClip(float x, float y, float width, float height)
    {
        var newRect = new Rect(x, y, width, height);

        if (_clipStack.Count > 0)
        {
            var current = _clipStack.Peek();
            var intersectX = Math.Max(current.X, newRect.X);
            var intersectY = Math.Max(current.Y, newRect.Y);
            var intersectRight = Math.Min(current.X + current.Width, newRect.X + newRect.Width);
            var intersectBottom = Math.Min(current.Y + current.Height, newRect.Y + newRect.Height);

            if (intersectRight > intersectX && intersectBottom > intersectY)
            {
                _clipStack.Push(new Rect(intersectX, intersectY, intersectRight - intersectX, intersectBottom - intersectY));
            }
            else
            {
                _clipStack.Push(Rect.Zero);
            }
        }
        else
        {
            _clipStack.Push(newRect);
        }
    }

    public void PopClip()
    {
        if (_clipStack.Count > 0)
        {
            _clipStack.Pop();
        }
    }

    private Rect GetCurrentClip()
    {
        if (_clipStack.Count > 0)
        {
            return _clipStack.Peek();
        }

        return new Rect(0, 0, Width, Height);
    }

    #endregion

    #region 帧缓冲区

    public byte[] GetFramebufferData()
    {
        return _framebuffer;
    }

    #endregion

    #region 私有辅助方法

    private void BlendPixel(int offset, float r, float g, float b, float a)
    {
        var srcR = r;
        var srcG = g;
        var srcB = b;
        var srcA = a;

        var dstB = _framebuffer[offset] / 255f;
        var dstG = _framebuffer[offset + 1] / 255f;
        var dstR = _framebuffer[offset + 2] / 255f;
        var dstA = _framebuffer[offset + 3] / 255f;

        var outA = srcA + dstA * (1.0f - srcA);
        var invOutA = outA > 0.0f ? 1.0f / outA : 0.0f;

        var outR = (srcR * srcA + dstR * dstA * (1.0f - srcA)) * invOutA;
        var outG = (srcG * srcA + dstG * dstA * (1.0f - srcA)) * invOutA;
        var outB = (srcB * srcA + dstB * dstA * (1.0f - srcA)) * invOutA;

        _framebuffer[offset] = ToByte(outB);
        _framebuffer[offset + 1] = ToByte(outG);
        _framebuffer[offset + 2] = ToByte(outR);
        _framebuffer[offset + 3] = ToByte(outA);
    }

    private void DrawGlyph(byte[] glyph, float destX, float destY, float glyphW, float glyphH, float r, float g, float b)
    {
        var clip = GetCurrentClip();

        var x0 = Math.Max((int)destX, (int)clip.X);
        var y0 = Math.Max((int)destY, (int)clip.Y);
        var x1 = Math.Min((int)(destX + glyphW), (int)(clip.X + clip.Width));
        var y1 = Math.Min((int)(destY + glyphH), (int)(clip.Y + clip.Height));

        if (x0 >= x1 || y0 >= y1)
        {
            return;
        }

        for (var py = y0; py < y1; py++)
        {
            var srcY = (int)((py - destY) / glyphH * BitmapFont.GlyphSize);
            if (srcY is < 0 or >= BitmapFont.GlyphSize)
            {
                continue;
            }

            var rowOffset = py * Width * 4;
            for (var px = x0; px < x1; px++)
            {
                var srcX = (int)((px - destX) / glyphW * BitmapFont.GlyphSize);
                if (srcX is < 0 or >= BitmapFont.GlyphSize)
                {
                    continue;
                }

                var bitIndex = srcY * BitmapFont.GlyphSize + srcX;
                if ((glyph[bitIndex / 8] & (0x80 >> (bitIndex % 8))) != 0)
                {
                    BlendPixel(rowOffset + px * 4, r, g, b, 1.0f);
                }
            }
        }
    }

    private static byte ToByte(float value)
    {
        if (value <= 0.0f)
        {
            return 0;
        }

        if (value >= 1.0f)
        {
            return 255;
        }

        return (byte)(value * 255.0f + 0.5f);
    }

    #endregion
}
