using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// 包围球
/// </summary>
public readonly record struct BoundingSphere(Vector3 Center, float Radius)
{
    #region 公开方法

    /// <summary>
    /// 检测点是否在包围球内
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Vector3 point)
    {
        return Vector3.DistanceSquared(Center, point) <= Radius * Radius;
    }

    /// <summary>
    /// 检测另一个包围球是否完全在此包围球内
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(BoundingSphere other)
    {
        var distance = Vector3.Distance(Center, other.Center);
        return distance + other.Radius <= Radius;
    }

    /// <summary>
    /// 检测两个包围球是否相交
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(BoundingSphere other)
    {
        var distanceSq = Vector3.DistanceSquared(Center, other.Center);
        var radiusSum = Radius + other.Radius;
        return distanceSq <= radiusSum * radiusSum;
    }

    /// <summary>
    /// 检测与包围盒是否相交
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(BoundingBox box)
    {
        var clamped = new Vector3(
            MathHelper.Clamp(Center.X, box.Min.X, box.Max.X),
            MathHelper.Clamp(Center.Y, box.Min.Y, box.Max.Y),
            MathHelper.Clamp(Center.Z, box.Min.Z, box.Max.Z));
        return Vector3.DistanceSquared(Center, clamped) <= Radius * Radius;
    }

    /// <summary>
    /// 合并两个包围球
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BoundingSphere Merge(BoundingSphere a, BoundingSphere b)
    {
        var center = Vector3.Lerp(a.Center, b.Center, 0.5f);
        var radiusA = Vector3.Distance(center, a.Center) + a.Radius;
        var radiusB = Vector3.Distance(center, b.Center) + b.Radius;
        return new BoundingSphere(center, MathF.Max(radiusA, radiusB));
    }

    /// <summary>
    /// 从一组点创建包围球
    /// </summary>
    public static BoundingSphere CreateFromPoints(ReadOnlySpan<Vector3> points)
    {
        if (points.IsEmpty)
        {
            return new BoundingSphere(Vector3.Zero, 0f);
        }

        var box = BoundingBox.CreateFromPoints(points);
        var center = box.Center;
        var maxDistSq = 0f;

        for (var i = 0; i < points.Length; i++)
        {
            var distSq = Vector3.DistanceSquared(center, points[i]);
            if (distSq > maxDistSq)
            {
                maxDistSq = distSq;
            }
        }

        return new BoundingSphere(center, MathF.Sqrt(maxDistSq));
    }

    #endregion
}
