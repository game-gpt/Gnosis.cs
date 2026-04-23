using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// Vector2 扩展方法
/// </summary>
public static class Vector2Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Perpendicular(this Vector2 vector)
    {
        return new Vector2(vector.Y, -vector.X);
    }
}
