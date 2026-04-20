namespace Gnosis;

public readonly struct WidgetEdgeInsets
{
    public readonly float Top;
    public readonly float Right;
    public readonly float Bottom;
    public readonly float Left;

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;

    public WidgetEdgeInsets(float top, float right, float bottom, float left)
    {
        Top = top;
        Right = right;
        Bottom = bottom;
        Left = left;
    }

    public WidgetEdgeInsets(float all) : this(all, all, all, all) { }

    public WidgetEdgeInsets(float vertical, float horizontal) : this(vertical, horizontal, vertical, horizontal) { }

    public static WidgetEdgeInsets Zero { get; } = new(0);
}
