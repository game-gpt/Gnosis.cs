using System.Numerics;
using System.Runtime.InteropServices;

namespace Gnosis.Graphic.Sprite2D;

[StructLayout(LayoutKind.Sequential)]
public struct Vertex2D
{
    public Vector2 Position;
    public Vector2 Uv;
    public Vector4 Tint;

    public Vertex2D(Vector2 position, Vector2 uv, Vector4 tint)
    {
        Position = position;
        Uv = uv;
        Tint = tint;
    }
}
