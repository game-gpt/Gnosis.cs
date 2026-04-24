using System.Numerics;
using System.Runtime.InteropServices;

namespace Gnosis.Graphic.Foliage;

[StructLayout(LayoutKind.Sequential)]
public struct FoliageInstance
{
    public Vector3 Position;
    public float Scale;
    public Vector3 Rotation;
    public uint ColorPacked;
    public int TypeIndex;

    public FoliageInstance()
    {
        Position = Vector3.Zero;
        Scale = 1.0f;
        Rotation = Vector3.Zero;
        ColorPacked = 0xFFFFFFFF;
        TypeIndex = 0;
    }

    public FoliageInstance(Vector3 position, float scale, Vector3 rotation, uint colorPacked, int typeIndex)
    {
        Position = position;
        Scale = scale;
        Rotation = rotation;
        ColorPacked = colorPacked;
        TypeIndex = typeIndex;
    }

    public static uint PackColor(float r, float g, float b, float a)
    {
        var ri = (byte)(Math.Clamp(r, 0.0f, 1.0f) * 255);
        var gi = (byte)(Math.Clamp(g, 0.0f, 1.0f) * 255);
        var bi = (byte)(Math.Clamp(b, 0.0f, 1.0f) * 255);
        var ai = (byte)(Math.Clamp(a, 0.0f, 1.0f) * 255);
        return (uint)(ai << 24 | bi << 16 | gi << 8 | ri);
    }

    public static void UnpackColor(uint packed, out float r, out float g, out float b, out float a)
    {
        r = (packed & 0xFF) / 255.0f;
        g = ((packed >> 8) & 0xFF) / 255.0f;
        b = ((packed >> 16) & 0xFF) / 255.0f;
        a = ((packed >> 24) & 0xFF) / 255.0f;
    }
}
