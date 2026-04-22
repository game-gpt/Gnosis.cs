namespace Gnosis.Widget.Element;

public readonly struct Color
{
    public readonly float R;
    public readonly float G;
    public readonly float B;
    public readonly float A;

    public static Color Transparent { get; } = new(0, 0, 0, 0);
    public static Color White { get; } = new(1, 1, 1, 1);
    public static Color Black { get; } = new(0, 0, 0, 1);

    public static Color FromRgb(byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f, 1.0f);

    public static Color FromRgba(byte r, byte g, byte b, byte a) => new(r / 255f, g / 255f, b / 255f, a / 255f);

    public Color(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}
