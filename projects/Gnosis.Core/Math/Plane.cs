using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// 平面，由法向量和距离定义
/// </summary>
public readonly record struct Plane(Vector3 Normal, float D)
{
    #region 构造函数

    /// <summary>
    /// 从法向量和距离创建平面
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Plane(float nx, float ny, float nz, float d)
        : this(new Vector3(nx, ny, nz), d)
    {
    }

    /// <summary>
    /// 从法向量和平面上的点创建平面
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Plane(Vector3 normal, Vector3 point)
        : this(normal, Vector3.Dot(normal, point))
    {
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 计算点到平面的有符号距离
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DistanceToPoint(Vector3 point)
    {
        return Vector3.Dot(Normal, point) - D;
    }

    /// <summary>
    /// 判断点在平面的哪一侧
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetSide(Vector3 point)
    {
        return DistanceToPoint(point);
    }

    #endregion
}
