namespace GnosisEngine.UI.Layout;

// 交叉轴对齐方式
public enum CrossAxisAlignment
{
    Start,
    Center,
    End,
    Stretch
}

public readonly struct EdgeInsets
{
    public readonly float Top;
    public readonly float Right;
    public readonly float Bottom;
    public readonly float Left;

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;

    public EdgeInsets(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public EdgeInsets(float all) : this(all, all, all, all) { }

    public EdgeInsets(float vertical, float horizontal) : this(vertical, horizontal, vertical, horizontal) { }

    public static EdgeInsets Zero { get; } = new(0);
}

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
