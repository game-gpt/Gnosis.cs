using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// 二维浮点向量
/// </summary>
public readonly record struct Vector2(float X, float Y)
{
    #region 静态属性

    /// <summary>
    /// 零向量 (0, 0)
    /// </summary>
    public static Vector2 Zero => new(0f, 0f);

    /// <summary>
    /// 全一向量 (1, 1)
    /// </summary>
    public static Vector2 One => new(1f, 1f);

    /// <summary>
    /// X 轴单位向量 (1, 0)
    /// </summary>
    public static Vector2 UnitX => new(1f, 0f);

    /// <summary>
    /// Y 轴单位向量 (0, 1)
    /// </summary>
    public static Vector2 UnitY => new(0f, 1f);

    #endregion

    #region 运算符

    /// <summary>
    /// 向量加法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator +(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X + right.X, left.Y + right.Y);
    }

    /// <summary>
    /// 向量减法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator -(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X - right.X, left.Y - right.Y);
    }

    /// <summary>
    /// 向量与标量乘法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator *(Vector2 vector, float scalar)
    {
        return new Vector2(vector.X * scalar, vector.Y * scalar);
    }

    /// <summary>
    /// 标量与向量乘法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator *(float scalar, Vector2 vector)
    {
        return new Vector2(vector.X * scalar, vector.Y * scalar);
    }

    /// <summary>
    /// 向量逐分量乘法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator *(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X * right.X, left.Y * right.Y);
    }

    /// <summary>
    /// 向量与标量除法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator /(Vector2 vector, float scalar)
    {
        var inv = 1f / scalar;
        return new Vector2(vector.X * inv, vector.Y * inv);
    }

    /// <summary>
    /// 向量逐分量除法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator /(Vector2 left, Vector2 right)
    {
        return new Vector2(left.X / right.X, left.Y / right.Y);
    }

    /// <summary>
    /// 向量取反
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator -(Vector2 vector)
    {
        return new Vector2(-vector.X, -vector.Y);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 计算点积
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>点积值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector2 a, Vector2 b)
    {
        return a.X * b.X + a.Y * b.Y;
    }

    /// <summary>
    /// 计算向量长度
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Length()
    {
        return MathF.Sqrt(X * X + Y * Y);
    }

    /// <summary>
    /// 计算向量长度平方
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared()
    {
        return X * X + Y * Y;
    }

    /// <summary>
    /// 将当前向量归一化（修改自身，对 readonly struct 返回新值）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 Normalize()
    {
        var len = Length();
        if (len < MathHelper.Epsilon)
        {
            return Zero;
        }

        return this / len;
    }

    /// <summary>
    /// 返回归一化后的向量
    /// </summary>
    /// <param name="vector">输入向量</param>
    /// <returns>归一化向量</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Normalized(Vector2 vector)
    {
        return vector.Normalize();
    }

    /// <summary>
    /// 计算两个向量之间的距离
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>距离值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(Vector2 a, Vector2 b)
    {
        return (a - b).Length();
    }

    /// <summary>
    /// 计算两个向量之间距离的平方
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>距离平方值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceSquared(Vector2 a, Vector2 b)
    {
        return (a - b).LengthSquared();
    }

    /// <summary>
    /// 线性插值
    /// </summary>
    /// <param name="a">起始向量</param>
    /// <param name="b">结束向量</param>
    /// <param name="t">插值因子</param>
    /// <returns>插值结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
    {
        return new Vector2(
            MathHelper.Lerp(a.X, b.X, t),
            MathHelper.Lerp(a.Y, b.Y, t)
        );
    }

    /// <summary>
    /// 计算反射向量
    /// </summary>
    /// <param name="vector">入射向量</param>
    /// <param name="normal">法线向量</param>
    /// <returns>反射向量</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Reflect(Vector2 vector, Vector2 normal)
    {
        var dot = Dot(vector, normal);
        return vector - 2f * dot * normal;
    }

    /// <summary>
    /// 计算垂直向量（顺时针旋转 90 度）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 Perpendicular()
    {
        return new Vector2(Y, -X);
    }

    #endregion
}
