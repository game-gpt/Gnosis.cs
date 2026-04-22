using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// 视锥体，由 6 个平面定义
/// </summary>
public sealed class Frustum
{
    #region 常量

    private const int PlaneCount = 6;

    #endregion

    #region 字段

    private readonly Plane[] _planes;

    #endregion

    #region 属性

    /// <summary>
    /// 近裁面
    /// </summary>
    public Plane Near => _planes[0];

    /// <summary>
    /// 远裁面
    /// </summary>
    public Plane Far => _planes[1];

    /// <summary>
    /// 左裁面
    /// </summary>
    public Plane Left => _planes[2];

    /// <summary>
    /// 右裁面
    /// </summary>
    public Plane Right => _planes[3];

    /// <summary>
    /// 上裁面
    /// </summary>
    public Plane Top => _planes[4];

    /// <summary>
    /// 下裁面
    /// </summary>
    public Plane Bottom => _planes[5];

    #endregion

    #region 构造函数

    public Frustum()
    {
        _planes = new Plane[PlaneCount];
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 从视图投影矩阵更新视锥体平面
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(Matrix4x4 viewProjection)
    {
        _planes[0] = ExtractPlane(viewProjection.M14 + viewProjection.M13, viewProjection.M24 + viewProjection.M23, viewProjection.M34 + viewProjection.M33, viewProjection.M44 + viewProjection.M43);
        _planes[1] = ExtractPlane(viewProjection.M14 - viewProjection.M13, viewProjection.M24 - viewProjection.M23, viewProjection.M34 - viewProjection.M33, viewProjection.M44 - viewProjection.M43);
        _planes[2] = ExtractPlane(viewProjection.M14 + viewProjection.M11, viewProjection.M24 + viewProjection.M21, viewProjection.M34 + viewProjection.M31, viewProjection.M44 + viewProjection.M41);
        _planes[3] = ExtractPlane(viewProjection.M14 - viewProjection.M11, viewProjection.M24 - viewProjection.M21, viewProjection.M34 - viewProjection.M31, viewProjection.M44 - viewProjection.M41);
        _planes[4] = ExtractPlane(viewProjection.M14 - viewProjection.M12, viewProjection.M24 - viewProjection.M22, viewProjection.M34 - viewProjection.M32, viewProjection.M44 - viewProjection.M42);
        _planes[5] = ExtractPlane(viewProjection.M14 + viewProjection.M12, viewProjection.M24 + viewProjection.M22, viewProjection.M34 + viewProjection.M32, viewProjection.M44 + viewProjection.M42);
    }

    /// <summary>
    /// 检测点是否在视锥体内
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Vector3 point)
    {
        for (var i = 0; i < PlaneCount; i++)
        {
            if (_planes[i].DistanceToPoint(point) < 0f)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检测包围球是否在视锥体内
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(BoundingSphere sphere)
    {
        for (var i = 0; i < PlaneCount; i++)
        {
            var distance = _planes[i].DistanceToPoint(sphere.Center);
            if (distance < -sphere.Radius)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检测包围盒是否在视锥体内
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Intersects(BoundingBox box)
    {
        for (var i = 0; i < PlaneCount; i++)
        {
            var normal = _planes[i].Normal;
            var p = new Vector3(
                normal.X >= 0f ? box.Min.X : box.Max.X,
                normal.Y >= 0f ? box.Min.Y : box.Max.Y,
                normal.Z >= 0f ? box.Min.Z : box.Max.Z);

            if (_planes[i].DistanceToPoint(p) < 0f)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    #region 私有方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Plane ExtractPlane(float a, float b, float c, float d)
    {
        var length = MathF.Sqrt(a * a + b * b + c * c);
        if (length < MathHelper.Epsilon)
        {
            return new Plane(Vector3.Zero, 0f);
        }

        var inv = 1f / length;
        return new Plane(new Vector3(a * inv, b * inv, c * inv), d * inv);
    }

    #endregion
}
