using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// Matrix4x4 扩展方法，补充 System.Numerics.Matrix4x4 缺失的功能
/// </summary>
public static class Matrix4x4Extensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 TransformPoint(this Matrix4x4 matrix, Vector3 point)
    {
        var x = matrix.M11 * point.X + matrix.M12 * point.Y + matrix.M13 * point.Z + matrix.M14;
        var y = matrix.M21 * point.X + matrix.M22 * point.Y + matrix.M23 * point.Z + matrix.M24;
        var z = matrix.M31 * point.X + matrix.M32 * point.Y + matrix.M33 * point.Z + matrix.M34;
        return new Vector3(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 TransformDirection(this Matrix4x4 matrix, Vector3 direction)
    {
        var x = matrix.M11 * direction.X + matrix.M12 * direction.Y + matrix.M13 * direction.Z;
        var y = matrix.M21 * direction.X + matrix.M22 * direction.Y + matrix.M23 * direction.Z;
        var z = matrix.M31 * direction.X + matrix.M32 * direction.Y + matrix.M33 * direction.Z;
        return new Vector3(x, y, z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 Invert(this Matrix4x4 matrix)
    {
        return Matrix4x4.Invert(matrix, out var result) ? result : Matrix4x4.Identity;
    }
}
