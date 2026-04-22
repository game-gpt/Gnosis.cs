namespace Gnosis.Widget.Layout;

public readonly struct GridLength
{
    public GridUnitType Type { get; }

    public float Value { get; }

    public static GridLength Auto { get; } = new(0, GridUnitType.Auto);

    public static GridLength Star { get; } = new(1, GridUnitType.Star);

    public GridLength(float value, GridUnitType type = GridUnitType.Pixel)
    {
        Value = value;
        Type = type;
    }

    public static GridLength Pixel(float value) => new(value, GridUnitType.Pixel);

    public static GridLength StarFrom(float value) => new(value, GridUnitType.Star);
}

public enum GridUnitType
{
    Pixel,
    Auto,
    Star
}
