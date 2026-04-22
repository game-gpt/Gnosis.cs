using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// 三维浮点向量
/// </summary>
public readonly record struct Vector3(float X, float Y, float Z)
{
    #region 静态属性

    /// <summary>
    /// 零向量 (0, 0, 0)
    /// </summary>
    public static Vector3 Zero => new(0f, 0f, 0f);

    /// <summary>
    /// 全一向量 (1, 1, 1)
    /// </summary>
    public static Vector3 One => new(1f, 1f, 1f);

    /// <summary>
    /// X 轴单位向量 (1, 0, 0)
    /// </summary>
    public static Vector3 UnitX => new(1f, 0f, 0f);

    /// <summary>
    /// Y 轴单位向量 (0, 1, 0)
    /// </summary>
    public static Vector3 UnitY => new(0f, 1f, 0f);

    /// <summary>
    /// Z 轴单位向量 (0, 0, 1)
    /// </summary>
    public static Vector3 UnitZ => new(0f, 0f, 1f);

    /// <summary>
    /// 上方向向量 (0, 1, 0)
    /// </summary>
    public static Vector3 Up => new(0f, 1f, 0f);

    /// <summary>
    /// 下方向向量 (0, -1, 0)
    /// </summary>
    public static Vector3 Down => new(0f, -1f, 0f);

    /// <summary>
    /// 左方向向量 (-1, 0, 0)
    /// </summary>
    public static Vector3 Left => new(-1f, 0f, 0f);

    /// <summary>
    /// 右方向向量 (1, 0, 0)
    /// </summary>
    public static Vector3 Right => new(1f, 0f, 0f);

    /// <summary>
    /// 前方向向量 (0, 0, -1)
    /// </summary>
    public static Vector3 Forward => new(0f, 0f, -1f);

    /// <summary>
    /// 后方向向量 (0, 0, 1)
    /// </summary>
    public static Vector3 Back => new(0f, 0f, 1f);

    #endregion

    #region 运算符

    /// <summary>
    /// 向量加法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator +(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    /// <summary>
    /// 向量减法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator -(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    /// <summary>
    /// 向量与标量乘法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(Vector3 vector, float scalar)
    {
        return new Vector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
    }

    /// <summary>
    /// 标量与向量乘法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(float scalar, Vector3 vector)
    {
        return new Vector3(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);
    }

    /// <summary>
    /// 向量逐分量乘法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
    }

    /// <summary>
    /// 向量与标量除法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator /(Vector3 vector, float scalar)
    {
        var inv = 1f / scalar;
        return new Vector3(vector.X * inv, vector.Y * inv, vector.Z * inv);
    }

    /// <summary>
    /// 向量逐分量除法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator /(Vector3 left, Vector3 right)
    {
        return new Vector3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
    }

    /// <summary>
    /// 向量取反
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator -(Vector3 vector)
    {
        return new Vector3(-vector.X, -vector.Y, -vector.Z);
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
    public static float Dot(Vector3 a, Vector3 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    /// <summary>
    /// 计算叉积
    /// </summary>
    /// <param name="a">第一个向量</param>
    /// <param name="b">第二个向量</param>
    /// <returns>叉积向量</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Cross(Vector3 a, Vector3 b)
    {
        return new Vector3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
    }

    /// <summary>
    /// 计算向量长度
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Length()
    {
        return MathF.Sqrt(X * X + Y * Y + Z * Z);
    }

    /// <summary>
    /// 计算向量长度平方
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared()
    {
        return X * X + Y * Y + Z * Z;
    }

    /// <summary>
    /// 将当前向量归一化
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Normalize()
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
    public static Vector3 Normalized(Vector3 vector)
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
    public static float Distance(Vector3 a, Vector3 b)
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
    public static float DistanceSquared(Vector3 a, Vector3 b)
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
    public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        return new Vector3(
            MathHelper.Lerp(a.X, b.X, t),
            MathHelper.Lerp(a.Y, b.Y, t),
            MathHelper.Lerp(a.Z, b.Z, t)
        );
    }

    /// <summary>
    /// 球面线性插值
    /// </summary>
    /// <param name="a">起始向量</param>
    /// <param name="b">结束向量</param>
    /// <param name="t">插值因子</param>
    /// <returns>插值结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Slerp(Vector3 a, Vector3 b, float t)
    {
        var lenA = a.Length();
        var lenB = b.Length();

        if (lenA < MathHelper.Epsilon || lenB < MathHelper.Epsilon)
        {
            return Lerp(a, b, t);
        }

        var normA = a / lenA;
        var normB = b / lenB;
        var dot = MathHelper.Clamp(Dot(normA, normB), -1f, 1f);
        var theta = MathF.Acos(dot);
        var sinTheta = MathF.Sin(theta);

        if (sinTheta < MathHelper.Epsilon)
        {
            return Lerp(a, b, t);
        }

        var wa = MathF.Sin((1f - t) * theta) / sinTheta;
        var wb = MathF.Sin(t * theta) / sinTheta;
        return (wa * normA + wb * normB) * MathHelper.Lerp(lenA, lenB, t);
    }

    /// <summary>
    /// 计算反射向量
    /// </summary>
    /// <param name="vector">入射向量</param>
    /// <param name="normal">法线向量</param>
    /// <returns>反射向量</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Reflect(Vector3 vector, Vector3 normal)
    {
        var dot = Dot(vector, normal);
        return vector - 2f * dot * normal;
    }

    /// <summary>
    /// 将向量投影到另一个向量上
    /// </summary>
    /// <param name="vector">要投影的向量</param>
    /// <param name="onNormal">投影目标向量</param>
    /// <returns>投影结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Project(Vector3 vector, Vector3 onNormal)
    {
        var dot = Dot(onNormal, onNormal);
        if (dot < MathHelper.Epsilon)
        {
            return Zero;
        }

        var d = Dot(vector, onNormal);
        return new Vector3(
            onNormal.X * d / dot,
            onNormal.Y * d / dot,
            onNormal.Z * d / dot
        );
    }

    #endregion
}
