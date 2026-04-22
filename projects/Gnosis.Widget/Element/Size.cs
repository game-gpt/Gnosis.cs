namespace Gnosis.Widget.Element;

public readonly struct Size
{
    public readonly float Width;
    public readonly float Height;

    public static Size Infinity { get; } = new(float.PositiveInfinity, float.PositiveInfinity);
    public static Size Zero { get; } = new(0, 0);

    public bool IsInfinity => float.IsPositiveInfinity(Width) && float.IsPositiveInfinity(Height);

    public Size(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public Size Clamp(Size min, Size max)
    {
        return new Size(
            Math.Clamp(Width, min.Width, max.Width),
            Math.Clamp(Height, min.Height, max.Height)
        );
    }

    public Size Deflate(EdgeInsets padding)
    {
        return new Size(
            Math.Max(0, Width - padding.Horizontal),
            Math.Max(0, Height - padding.Vertical)
        );
    }

    public Size Inflate(EdgeInsets padding)
    {
        return new Size(Width + padding.Horizontal, Height + padding.Vertical);
    }
}
