namespace Gnosis.Rendering.Renderer;

public sealed class Texture
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Data { get; }

    public Texture(int width, int height)
    {
        Width = width;
        Height = height;
        Data = new byte[width * height * 4];
    }

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 255)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        var idx = (y * Width + x) * 4;
        Data[idx] = r;
        Data[idx + 1] = g;
        Data[idx + 2] = b;
        Data[idx + 3] = a;
    }

    public (byte r, byte g, byte b, byte a) GetPixel(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return (0, 0, 0, 0);

        var idx = (y * Width + x) * 4;
        return (Data[idx], Data[idx + 1], Data[idx + 2], Data[idx + 3]);
    }
}
