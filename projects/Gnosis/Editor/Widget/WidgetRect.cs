namespace Gnosis.Editor.Widget;

public readonly struct WidgetRect
{
    public readonly float X;
    public readonly float Y;
    public readonly float Width;
    public readonly float Height;

    public static WidgetRect Zero { get; } = new(0, 0, 0, 0);

    public float Right => X + Width;
    public float Bottom => Y + Height;

    public WidgetRect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public WidgetRect Deflate(WidgetEdgeInsets padding)
    {
        return new WidgetRect(
            X + padding.Left,
            Y + padding.Top,
            Math.Max(0, Width - padding.Horizontal),
            Math.Max(0, Height - padding.Vertical)
        );
    }
}
