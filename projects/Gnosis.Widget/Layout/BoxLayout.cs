using Gnosis.Widget.Element;

namespace Gnosis.Widget.Layout;

public static class BoxLayout
{
    public static Size ComputeTotalSize(Size contentSize, EdgeInsets padding, EdgeInsets border, EdgeInsets margin)
    {
        return contentSize
            .Inflate(padding)
            .Inflate(border)
            .Inflate(margin);
    }

    public static Rect GetContentRect(Rect layoutRect, EdgeInsets padding, EdgeInsets border, EdgeInsets margin)
    {
        return layoutRect
            .Deflate(margin)
            .Deflate(border)
            .Deflate(padding);
    }

    public static Size ConstrainSize(Size size, float? width, float? height, float minWidth, float minHeight, float maxWidth, float maxHeight)
    {
        var result = size.Clamp(
            new Size(minWidth, minHeight),
            new Size(maxWidth, maxHeight)
        );

        if (width.HasValue)
        {
            result = new Size(width.Value, result.Height);
        }

        if (height.HasValue)
        {
            result = new Size(result.Width, height.Value);
        }

        return result;
    }
}
