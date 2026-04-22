using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// 四维浮点向量
/// </summary>
public readonly record struct Vector4(float X, float Y, float Z, float W)
{
    #region 静态属性

    /// <summary>
    /// 零向量 (0, 0, 0, 0)
    /// </summary>
    public static Vector4 Zero => new(0f, 0f, 0f, 0f);

    /// <summary>
    /// 全一向量 (1, 1, 1, 1)
    /// </summary>
    public static Vector4 One => new(1f, 1f, 1f, 1f);

    /// <summary>
    /// X 轴单位向量 (1, 0, 0, 0)
    /// </summary>
    public static Vector4 UnitX => new(1f, 0f, 0f, 0f);

    /// <summary>
    /// Y 轴单位向量 (0, 1, 0, 0)
    /// </summary>
    public static Vector4 UnitY => new(0f, 1f, 0f, 0f);

    /// <summary>
    /// Z 轴单位向量 (0, 0, 1, 0)
    /// </summary>
    public static Vector4 UnitZ => new(0f, 0f, 1f, 0f);

    /// <summary>
    /// W 轴单位向量 (0, 0, 0, 1)
    /// </summary>
    public static Vector4 UnitW => new(0f, 0f, 0f, 1f);

    #endregion

    #region 运算符

    /// <summary>
    /// 向量加法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator +(Vector4 left, Vector4 right)
    {
        return new Vector4(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
    }

    /// <summary>
    /// 向量减法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator -(Vector4 left, Vector4 right)
    {
        return new Vector4(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
    }

    /// <summary>
    /// 向量与标量乘法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator *(Vector4 vector, float scalar)
    {
        return new Vector4(vector.X * scalar, vector.Y * scalar, vector.Z * scalar, vector.W * scalar);
    }

    /// <summary>
    /// 标量与向量乘法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator *(float scalar, Vector4 vector)
    {
        return new Vector4(vector.X * scalar, vector.Y * scalar, vector.Z * scalar, vector.W * scalar);
    }

    /// <summary>
    /// 向量逐分量乘法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator *(Vector4 left, Vector4 right)
    {
        return new Vector4(left.X * right.X, left.Y * right.Y, left.Z * right.Z, left.W * right.W);
    }

    /// <summary>
    /// 向量与标量除法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator /(Vector4 vector, float scalar)
    {
        var inv = 1f / scalar;
        return new Vector4(vector.X * inv, vector.Y * inv, vector.Z * inv, vector.W * inv);
    }

    /// <summary>
    /// 向量逐分量除法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator /(Vector4 left, Vector4 right)
    {
        return new Vector4(left.X / right.X, left.Y / right.Y, left.Z / right.Z, left.W / right.W);
    }

    /// <summary>
    /// 向量取反
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 operator -(Vector4 vector)
    {
        return new Vector4(-vector.X, -vector.Y, -vector.Z, -vector.W);
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
    public static float Dot(Vector4 a, Vector4 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
    }

    /// <summary>
    /// 计算向量长度
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Length()
    {
        return MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);
    }

    /// <summary>
    /// 计算向量长度平方
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared()
    {
        return X * X + Y * Y + Z * Z + W * W;
    }

    /// <summary>
    /// 将当前向量归一化
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector4 Normalize()
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
    public static Vector4 Normalized(Vector4 vector)
    {
        return vector.Normalize();
    }

    /// <summary>
    /// 线性插值
    /// </summary>
    /// <param name="a">起始向量</param>
    /// <param name="b">结束向量</param>
    /// <param name="t">插值因子</param>
    /// <returns>插值结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 Lerp(Vector4 a, Vector4 b, float t)
    {
        return new Vector4(
            MathHelper.Lerp(a.X, b.X, t),
            MathHelper.Lerp(a.Y, b.Y, t),
            MathHelper.Lerp(a.Z, b.Z, t),
            MathHelper.Lerp(a.W, b.W, t)
        );
    }

    #endregion
}
