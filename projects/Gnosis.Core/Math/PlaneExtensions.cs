using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// Plane 扩展方法，补充 System.Numerics.Plane 缺失的功能
/// </summary>
public static class PlaneExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceToPoint(this Plane plane, Vector3 point)
    {
        return Vector3.Dot(plane.Normal, point) + plane.D;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetSide(this Plane plane, Vector3 point)
    {
        return DistanceToPoint(plane, point);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Plane CreateFromPointAndNormal(Vector3 normal, Vector3 point)
    {
        var d = -Vector3.Dot(normal, point);
        return new Plane(normal, d);
    }
}
