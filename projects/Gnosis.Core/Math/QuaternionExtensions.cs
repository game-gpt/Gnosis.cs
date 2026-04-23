using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// Quaternion 扩展方法，补充 System.Numerics.Quaternion 缺失的功能
/// </summary>
public static class QuaternionExtensions
{
    #region 实例扩展方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Normalize(this Quaternion quaternion)
    {
        return Quaternion.Normalize(quaternion);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Conjugate(this Quaternion quaternion)
    {
        return Quaternion.Conjugate(quaternion);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Inverse(this Quaternion quaternion)
    {
        return Quaternion.Inverse(quaternion);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Rotate(this Quaternion quaternion, Vector3 vector)
    {
        var q = quaternion * new Quaternion(vector.X, vector.Y, vector.Z, 0f) * Quaternion.Conjugate(quaternion);
        return new Vector3(q.X, q.Y, q.Z);
    }

    #endregion

    #region 静态扩展方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Lerp(Quaternion a, Quaternion b, float t)
    {
        return new Quaternion(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t,
            a.W + (b.W - a.W) * t);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion CreateFromEulerAngles(float pitch, float yaw, float roll)
    {
        return Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion FromToRotation(Vector3 from, Vector3 to)
    {
        var dot = Vector3.Dot(from, to);

        if (dot > 0.999999f)
        {
            return Quaternion.Identity;
        }

        if (dot < -0.999999f)
        {
            var axis = Vector3.Cross(Vector3.UnitX, from);
            if (axis.LengthSquared() < MathHelper.Epsilon)
            {
                axis = Vector3.Cross(Vector3.UnitY, from);
            }

            axis = Vector3.Normalize(axis);
            return Quaternion.CreateFromAxisAngle(axis, MathF.PI);
        }

        var crossAxis = Vector3.Cross(from, to);
        var w = 1f + dot;
        return Quaternion.Normalize(new Quaternion(crossAxis.X, crossAxis.Y, crossAxis.Z, w));
    }

    #endregion
}
