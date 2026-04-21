namespace Gnosis.Rendering.Widget;

public readonly struct WidgetSize
{
    public readonly float Width;
    public readonly float Height;

    public static WidgetSize Infinity { get; } = new(float.PositiveInfinity, float.PositiveInfinity);
    public static WidgetSize Zero { get; } = new(0, 0);

    public bool IsInfinity => float.IsPositiveInfinity(Width) && float.IsPositiveInfinity(Height);

    public WidgetSize(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public WidgetSize Clamp(WidgetSize min, WidgetSize max)
    {
        return new WidgetSize(
            Math.Clamp(Width, min.Width, max.Width),
            Math.Clamp(Height, min.Height, max.Height)
        );
    }

    public WidgetSize Deflate(WidgetEdgeInsets padding)
    {
        return new WidgetSize(
            Math.Max(0, Width - padding.Horizontal),
            Math.Max(0, Height - padding.Vertical)
        );
    }

    public WidgetSize Inflate(WidgetEdgeInsets padding)
    {
        return new WidgetSize(Width + padding.Horizontal, Height + padding.Vertical);
    }
}
