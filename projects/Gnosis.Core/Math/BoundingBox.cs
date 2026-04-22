using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// 轴对齐包围盒
/// </summary>
public readonly record struct BoundingBox(Vector3 Min, Vector3 Max)
{
    #region 静态属性

    public static BoundingBox Empty => new(Vector3.One * float.MaxValue, Vector3.One * float.MinValue);

    #endregion

    #region 公开方法

    /// <summary>
    /// 获取包围盒中心点
    /// </summary>
    public Vector3 Center => (Min + Max) * 0.5f;

    /// <summary>
    /// 获取包围盒尺寸
    /// </summary>
    public Vector3 Size => Max - Min;

    /// <summary>
    /// 获取包围盒半尺寸
    /// </summary>
    public Vector3 Extents => Size * 0.5f;

    /// <summary>
    /// 检测点是否在包围盒内
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Vector3 point)
    {
        return point.X >= Min.X && point.X <= Max.X &&
               point.Y >= Min.Y && point.Y <= Max.Y &&
               point.Z >= Min.Z && point.Z <= Max.Z;
    }

    /// <summary>
    /// 检测另一个包围盒是否完全在此包围盒内
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(BoundingBox other)
    {
        return Min.X <= other.Min.X && Max.X >= other.Max.X &&
               Min.Y <= other.Min.Y && Max.Y >= other.Max.Y &&
               Min.Z <= other.Min.Z && Max.Z >= other.Max.Z;
    }

    /// <summary>
    /// 检测两个包围盒是否相交
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(BoundingBox other)
    {
        return Min.X <= other.Max.X && Max.X >= other.Min.X &&
               Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
               Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
    }

    /// <summary>
    /// 合并两个包围盒
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BoundingBox Merge(BoundingBox a, BoundingBox b)
    {
        return new BoundingBox(
            new Vector3(MathF.Min(a.Min.X, b.Min.X), MathF.Min(a.Min.Y, b.Min.Y), MathF.Min(a.Min.Z, b.Min.Z)),
            new Vector3(MathF.Max(a.Max.X, b.Max.X), MathF.Max(a.Max.Y, b.Max.Y), MathF.Max(a.Max.Z, b.Max.Z)));
    }

    /// <summary>
    /// 从一组点创建包围盒
    /// </summary>
    public static BoundingBox CreateFromPoints(ReadOnlySpan<Vector3> points)
    {
        if (points.IsEmpty)
        {
            return Empty;
        }

        var min = points[0];
        var max = points[0];

        for (var i = 1; i < points.Length; i++)
        {
            var p = points[i];
            min = new Vector3(MathF.Min(min.X, p.X), MathF.Min(min.Y, p.Y), MathF.Min(min.Z, p.Z));
            max = new Vector3(MathF.Max(max.X, p.X), MathF.Max(max.Y, p.Y), MathF.Max(max.Z, p.Z));
        }

        return new BoundingBox(min, max);
    }

    #endregion
}
