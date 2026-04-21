namespace Gnosis.Editor.Widget;

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
