using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// 射线，由起点和方向定义
/// </summary>
public readonly record struct Ray(Vector3 Origin, Vector3 Direction)
{
    #region 公开方法

    /// <summary>
    /// 获取射线上指定距离处的点
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 GetPoint(float distance)
    {
        return Origin + Direction * distance;
    }

    /// <summary>
    /// 计算射线与平面的交点距离，无交点返回 null
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float? Intersects(Plane plane)
    {
        var denom = Vector3.Dot(plane.Normal, Direction);
        if (MathF.Abs(denom) < MathHelper.Epsilon)
        {
            return null;
        }

        var t = (plane.D - Vector3.Dot(plane.Normal, Origin)) / denom;
        return t >= 0f ? t : null;
    }

    /// <summary>
    /// 计算射线与包围球的交点距离，无交点返回 null
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float? Intersects(BoundingSphere sphere)
    {
        var diff = Origin - sphere.Center;
        var b = Vector3.Dot(diff, Direction);
        var c = Vector3.Dot(diff, diff) - sphere.Radius * sphere.Radius;

        if (c > 0f && b > 0f)
        {
            return null;
        }

        var discriminant = b * b - c;
        if (discriminant < 0f)
        {
            return null;
        }

        var sqrtDiscriminant = MathF.Sqrt(discriminant);
        var t = -b - sqrtDiscriminant;

        if (t < 0f)
        {
            t = -b + sqrtDiscriminant;
        }

        return t >= 0f ? t : null;
    }

    /// <summary>
    /// 计算射线与包围盒的交点距离，无交点返回 null
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float? Intersects(BoundingBox box)
    {
        var tMin = 0f;
        var tMax = float.MaxValue;

        for (var i = 0; i < 3; i++)
        {
            var origin = i == 0 ? Origin.X : i == 1 ? Origin.Y : Origin.Z;
            var direction = i == 0 ? Direction.X : i == 1 ? Direction.Y : Direction.Z;
            var min = i == 0 ? box.Min.X : i == 1 ? box.Min.Y : box.Min.Z;
            var max = i == 0 ? box.Max.X : i == 1 ? box.Max.Y : box.Max.Z;

            if (MathF.Abs(direction) < MathHelper.Epsilon)
            {
                if (origin < min || origin > max)
                {
                    return null;
                }
            }
            else
            {
                var invDirection = 1f / direction;
                var t1 = (min - origin) * invDirection;
                var t2 = (max - origin) * invDirection;

                if (t1 > t2)
                {
                    (t1, t2) = (t2, t1);
                }

                tMin = MathF.Max(tMin, t1);
                tMax = MathF.Min(tMax, t2);

                if (tMin > tMax)
                {
                    return null;
                }
            }
        }

        return tMin >= 0f ? tMin : tMax >= 0f ? tMax : null;
    }

    #endregion
}
