using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// Vector3 扩展方法，补充 System.Numerics.Vector3 缺失的功能
/// </summary>
public static class Vector3Extensions
{
    #region 方向常量

    public static Vector3 Up => new(0f, 1f, 0f);
    public static Vector3 Down => new(0f, -1f, 0f);
    public static Vector3 Left => new(-1f, 0f, 0f);
    public static Vector3 Right => new(1f, 0f, 0f);
    public static Vector3 Forward => new(0f, 0f, -1f);
    public static Vector3 Back => new(0f, 0f, 1f);

    #endregion

    #region 实例扩展方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Normalize(this Vector3 vector)
    {
        return Vector3.Normalize(vector);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LengthSquared(this Vector3 vector)
    {
        return vector.LengthSquared();
    }

    #endregion

    #region 静态扩展方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Slerp(Vector3 a, Vector3 b, float t)
    {
        var lenA = a.Length();
        var lenB = b.Length();

        if (lenA < MathHelper.Epsilon || lenB < MathHelper.Epsilon)
        {
            return Vector3.Lerp(a, b, t);
        }

        var normA = a / lenA;
        var normB = b / lenB;
        var dot = Math.Clamp(Vector3.Dot(normA, normB), -1f, 1f);
        var theta = MathF.Acos(dot);
        var sinTheta = MathF.Sin(theta);

        if (sinTheta < MathHelper.Epsilon)
        {
            return Vector3.Lerp(a, b, t);
        }

        var wa = MathF.Sin((1f - t) * theta) / sinTheta;
        var wb = MathF.Sin(t * theta) / sinTheta;
        return (wa * normA + wb * normB) * MathHelper.Lerp(lenA, lenB, t);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Project(Vector3 vector, Vector3 onNormal)
    {
        var dot = Vector3.Dot(onNormal, onNormal);
        if (dot < MathHelper.Epsilon)
        {
            return Vector3.Zero;
        }

        var d = Vector3.Dot(vector, onNormal);
        return onNormal * (d / dot);
    }

    #endregion
}
