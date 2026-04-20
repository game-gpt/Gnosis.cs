namespace Gnosis;

public readonly struct WidgetColor
{
    public readonly float R;
    public readonly float G;
    public readonly float B;
    public readonly float A;

    public static WidgetColor Transparent { get; } = new(0, 0, 0, 0);
    public static WidgetColor White { get; } = new(1, 1, 1, 1);
    public static WidgetColor Black { get; } = new(0, 0, 0, 1);

    public static WidgetColor FromRgb(byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f, 1.0f);

    public static WidgetColor FromRgba(byte r, byte g, byte b, byte a) => new(r / 255f, g / 255f, b / 255f, a / 255f);

    public WidgetColor(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
}
