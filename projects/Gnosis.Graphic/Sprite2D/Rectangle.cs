namespace Gnosis.Graphic.Sprite2D;

public record Rectangle
{
    public float X { get; init; }
    public float Y { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }

    public static Rectangle Empty => new();

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public float Left => X;
    public float Right => X + Width;
    public float Top => Y;
    public float Bottom => Y + Height;

    public Rectangle()
    {
    }

    public Rectangle(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Contains(float x, float y)
    {
        return x >= X && x <= Right && y >= Y && y <= Bottom;
    }

    public bool Intersects(Rectangle other)
    {
        return other.Left < Right && Left < other.Right &&
               other.Top < Bottom && Top < other.Bottom;
    }
}
