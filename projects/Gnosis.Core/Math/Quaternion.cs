using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// 四元数，用于表示三维旋转
/// </summary>
public readonly record struct Quaternion(float X, float Y, float Z, float W)
{
    #region 静态属性

    /// <summary>
    /// 单位四元数（无旋转）
    /// </summary>
    public static Quaternion Identity => new(0f, 0f, 0f, 1f);

    #endregion

    #region 运算符

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator +(Quaternion left, Quaternion right)
    {
        return new Quaternion(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.W + right.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator -(Quaternion left, Quaternion right)
    {
        return new Quaternion(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.W - right.W);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator *(Quaternion left, Quaternion right)
    {
        return new Quaternion(
            left.W * right.X + left.X * right.W + left.Y * right.Z - left.Z * right.Y,
            left.W * right.Y + left.Y * right.W + left.Z * right.X - left.X * right.Z,
            left.W * right.Z + left.Z * right.W + left.X * right.Y - left.Y * right.X,
            left.W * right.W - left.X * right.X - left.Y * right.Y - left.Z * right.Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator *(Quaternion quaternion, float scalar)
    {
        return new Quaternion(quaternion.X * scalar, quaternion.Y * scalar, quaternion.Z * scalar, quaternion.W * scalar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator *(float scalar, Quaternion quaternion)
    {
        return quaternion * scalar;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator -(Quaternion quaternion)
    {
        return new Quaternion(-quaternion.X, -quaternion.Y, -quaternion.Z, -quaternion.W);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 计算四元数长度
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Length()
    {
        return MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);
    }

    /// <summary>
    /// 计算四元数长度平方
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared()
    {
        return X * X + Y * Y + Z * Z + W * W;
    }

    /// <summary>
    /// 归一化四元数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion Normalize()
    {
        var len = Length();
        if (len < MathHelper.Epsilon)
        {
            return Identity;
        }

        var inv = 1f / len;
        return this * inv;
    }

    /// <summary>
    /// 计算共轭四元数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion Conjugate()
    {
        return new Quaternion(-X, -Y, -Z, W);
    }

    /// <summary>
    /// 计算逆四元数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion Inverse()
    {
        var lenSq = LengthSquared();
        if (lenSq < MathHelper.Epsilon)
        {
            return Identity;
        }

        var invLenSq = 1f / lenSq;
        return new Quaternion(-X * invLenSq, -Y * invLenSq, -Z * invLenSq, W * invLenSq);
    }

    /// <summary>
    /// 使用四元数旋转向量
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Rotate(Vector3 vector)
    {
        var q = this * new Quaternion(vector.X, vector.Y, vector.Z, 0f) * Conjugate();
        return new Vector3(q.X, q.Y, q.Z);
    }

    /// <summary>
    /// 计算两个四元数的点积
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Quaternion a, Quaternion b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
    }

    /// <summary>
    /// 球面线性插值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
    {
        var dot = Dot(a, b);

        if (dot < 0f)
        {
            b = -b;
            dot = -dot;
        }

        if (dot > 0.9995f)
        {
            return Lerp(a, b, t).Normalize();
        }

        var theta0 = MathF.Acos(dot);
        var theta = theta0 * t;
        var sinTheta = MathF.Sin(theta);
        var sinTheta0 = MathF.Sin(theta0);

        var s0 = MathF.Cos(theta) - dot * sinTheta / sinTheta0;
        var s1 = sinTheta / sinTheta0;

        return new Quaternion(
            s0 * a.X + s1 * b.X,
            s0 * a.Y + s1 * b.Y,
            s0 * a.Z + s1 * b.Z,
            s0 * a.W + s1 * b.W).Normalize();
    }

    /// <summary>
    /// 线性插值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Lerp(Quaternion a, Quaternion b, float t)
    {
        return new Quaternion(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.Z + (b.Z - a.Z) * t,
            a.W + (b.W - a.W) * t);
    }

    #endregion

    #region 工厂方法

    /// <summary>
    /// 从欧拉角（弧度）创建四元数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion CreateFromEulerAngles(float pitch, float yaw, float roll)
    {
        var halfPitch = pitch * 0.5f;
        var halfYaw = yaw * 0.5f;
        var halfRoll = roll * 0.5f;

        var sinPitch = MathF.Sin(halfPitch);
        var cosPitch = MathF.Cos(halfPitch);
        var sinYaw = MathF.Sin(halfYaw);
        var cosYaw = MathF.Cos(halfYaw);
        var sinRoll = MathF.Sin(halfRoll);
        var cosRoll = MathF.Cos(halfRoll);

        return new Quaternion(
            cosYaw * sinPitch * cosRoll + sinYaw * cosPitch * sinRoll,
            sinYaw * cosPitch * cosRoll - cosYaw * sinPitch * sinRoll,
            cosYaw * cosPitch * sinRoll - sinYaw * sinPitch * cosRoll,
            cosYaw * cosPitch * cosRoll + sinYaw * sinPitch * sinRoll);
    }

    /// <summary>
    /// 从旋转轴和角度（弧度）创建四元数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
    {
        var normalizedAxis = axis.Normalize();
        var halfAngle = angle * 0.5f;
        var sin = MathF.Sin(halfAngle);
        return new Quaternion(
            normalizedAxis.X * sin,
            normalizedAxis.Y * sin,
            normalizedAxis.Z * sin,
            MathF.Cos(halfAngle));
    }

    /// <summary>
    /// 从旋转矩阵创建四元数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion CreateFromRotationMatrix(Matrix4x4 matrix)
    {
        var trace = matrix.M11 + matrix.M22 + matrix.M33;

        if (trace > 0f)
        {
            var s = MathF.Sqrt(trace + 1f) * 2f;
            var invS = 1f / s;
            return new Quaternion(
                (matrix.M23 - matrix.M32) * invS,
                (matrix.M31 - matrix.M13) * invS,
                (matrix.M12 - matrix.M21) * invS,
                0.25f * s);
        }

        if (matrix.M11 > matrix.M22 && matrix.M11 > matrix.M33)
        {
            var s = MathF.Sqrt(1f + matrix.M11 - matrix.M22 - matrix.M33) * 2f;
            var invS = 1f / s;
            return new Quaternion(
                0.25f * s,
                (matrix.M12 + matrix.M21) * invS,
                (matrix.M13 + matrix.M31) * invS,
                (matrix.M23 - matrix.M32) * invS);
        }

        if (matrix.M22 > matrix.M33)
        {
            var s = MathF.Sqrt(1f + matrix.M22 - matrix.M11 - matrix.M33) * 2f;
            var invS = 1f / s;
            return new Quaternion(
                (matrix.M12 + matrix.M21) * invS,
                0.25f * s,
                (matrix.M23 + matrix.M32) * invS,
                (matrix.M31 - matrix.M13) * invS);
        }
        else
        {
            var s = MathF.Sqrt(1f + matrix.M33 - matrix.M11 - matrix.M22) * 2f;
            var invS = 1f / s;
            return new Quaternion(
                (matrix.M13 + matrix.M31) * invS,
                (matrix.M23 + matrix.M32) * invS,
                0.25f * s,
                (matrix.M12 - matrix.M21) * invS);
        }
    }

    /// <summary>
    /// 从一个方向到另一个方向创建旋转四元数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion FromToRotation(Vector3 from, Vector3 to)
    {
        var dot = Vector3.Dot(from, to);

        if (dot > 0.999999f)
        {
            return Identity;
        }

        if (dot < -0.999999f)
        {
            var axis = Vector3.Cross(Vector3.Right, from);
            if (axis.LengthSquared() < MathHelper.Epsilon)
            {
                axis = Vector3.Cross(Vector3.Up, from);
            }

            return CreateFromAxisAngle(axis.Normalize(), MathF.PI);
        }

        var crossAxis = Vector3.Cross(from, to);
        var w = 1f + dot;
        return new Quaternion(crossAxis.X, crossAxis.Y, crossAxis.Z, w).Normalize();
    }

    #endregion
}
