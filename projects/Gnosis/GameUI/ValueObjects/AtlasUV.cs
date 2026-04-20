namespace Gnosis.GameUI.ValueObjects;

public readonly struct AtlasUV
{
    public readonly float U;
    public readonly float V;
    public readonly float Width;
    public readonly float Height;

    public static AtlasUV Full { get; } = new(0, 0, 1, 1);
    public static AtlasUV Zero { get; } = new(0, 0, 0, 0);

    public AtlasUV(float u, float v, float width, float height)
    {
        U = u;
        V = v;
        Width = width;
        Height = height;
    }
}
