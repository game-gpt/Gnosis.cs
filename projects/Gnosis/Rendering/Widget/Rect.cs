namespace Gnosis.Rendering.Widget;

public readonly struct Rect
{
    public readonly float X;
    public readonly float Y;
    public readonly float Width;
    public readonly float Height;

    public static Rect Zero { get; } = new(0, 0, 0, 0);

    public float Right => X + Width;
    public float Bottom => Y + Height;

    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public Rect Deflate(EdgeInsets padding)
    {
        return new Rect(
            X + padding.Left,
            Y + padding.Top,
            Math.Max(0, Width - padding.Horizontal),
            Math.Max(0, Height - padding.Vertical)
        );
    }
}
