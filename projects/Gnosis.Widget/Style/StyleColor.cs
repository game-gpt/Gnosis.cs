namespace Gnosis.Widget.Style;

public readonly struct StyleColor
{
    public readonly float R;
    public readonly float G;
    public readonly float B;
    public readonly float A;

    public static StyleColor Transparent { get; } = new(0, 0, 0, 0);
    public static StyleColor White { get; } = new(1, 1, 1, 1);
    public static StyleColor Black { get; } = new(0, 0, 0, 1);

    public static StyleColor FromRgb(byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f, 1.0f);

    public static StyleColor FromRgba(byte r, byte g, byte b, byte a) => new(r / 255f, g / 255f, b / 255f, a / 255f);

    public static StyleColor FromHex(string hex)
    {
        var span = hex.AsSpan().TrimStart('#');

        return span.Length switch
        {
            3 => new StyleColor(
                ParseHex2(span[0], span[0]),
                ParseHex2(span[1], span[1]),
                ParseHex2(span[2], span[2]),
                1.0f),
            4 => new StyleColor(
                ParseHex2(span[0], span[0]),
                ParseHex2(span[1], span[1]),
                ParseHex2(span[2], span[2]),
                ParseHex2(span[3], span[3])),
            6 => new StyleColor(
                ParseHex1(span[0..2]),
                ParseHex1(span[2..4]),
                ParseHex1(span[4..6]),
                1.0f),
            8 => new StyleColor(
                ParseHex1(span[0..2]),
                ParseHex1(span[2..4]),
                ParseHex1(span[4..6]),
                ParseHex1(span[6..8])),
            _ => Transparent
        };
    }

    public Element.Color ToWidgetColor() => new(R, G, B, A);

    public StyleColor(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    private static float ParseHex1(ReadOnlySpan<char> s)
    {
        return byte.Parse(s, System.Globalization.NumberStyles.HexNumber) / 255f;
    }

    private static float ParseHex2(char hi, char lo)
    {
        var value = byte.Parse($"{hi}{lo}", System.Globalization.NumberStyles.HexNumber);
        return value / 255f;
    }
}
