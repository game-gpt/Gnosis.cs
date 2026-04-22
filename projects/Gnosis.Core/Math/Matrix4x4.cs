using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// 4x4 浮点矩阵，行主序存储
/// </summary>
public readonly record struct Matrix4x4(
    float M11, float M12, float M13, float M14,
    float M21, float M22, float M23, float M24,
    float M31, float M32, float M33, float M34,
    float M41, float M42, float M43, float M44)
{
    #region 静态属性

    /// <summary>
    /// 单位矩阵
    /// </summary>
    public static Matrix4x4 Identity => new(
        1f, 0f, 0f, 0f,
        0f, 1f, 0f, 0f,
        0f, 0f, 1f, 0f,
        0f, 0f, 0f, 1f);

    /// <summary>
    /// 零矩阵
    /// </summary>
    public static Matrix4x4 Zero => new(
        0f, 0f, 0f, 0f,
        0f, 0f, 0f, 0f,
        0f, 0f, 0f, 0f,
        0f, 0f, 0f, 0f);

    #endregion

    #region 运算符

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 operator +(Matrix4x4 left, Matrix4x4 right)
    {
        return new Matrix4x4(
            left.M11 + right.M11, left.M12 + right.M12, left.M13 + right.M13, left.M14 + right.M14,
            left.M21 + right.M21, left.M22 + right.M22, left.M23 + right.M23, left.M24 + right.M24,
            left.M31 + right.M31, left.M32 + right.M32, left.M33 + right.M33, left.M34 + right.M34,
            left.M41 + right.M41, left.M42 + right.M42, left.M43 + right.M43, left.M44 + right.M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 operator -(Matrix4x4 left, Matrix4x4 right)
    {
        return new Matrix4x4(
            left.M11 - right.M11, left.M12 - right.M12, left.M13 - right.M13, left.M14 - right.M14,
            left.M21 - right.M21, left.M22 - right.M22, left.M23 - right.M23, left.M24 - right.M24,
            left.M31 - right.M31, left.M32 - right.M32, left.M33 - right.M33, left.M34 - right.M34,
            left.M41 - right.M41, left.M42 - right.M42, left.M43 - right.M43, left.M44 - right.M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 operator *(Matrix4x4 left, Matrix4x4 right)
    {
        return new Matrix4x4(
            left.M11 * right.M11 + left.M12 * right.M21 + left.M13 * right.M31 + left.M14 * right.M41,
            left.M11 * right.M12 + left.M12 * right.M22 + left.M13 * right.M32 + left.M14 * right.M42,
            left.M11 * right.M13 + left.M12 * right.M23 + left.M13 * right.M33 + left.M14 * right.M43,
            left.M11 * right.M14 + left.M12 * right.M24 + left.M13 * right.M34 + left.M14 * right.M44,

            left.M21 * right.M11 + left.M22 * right.M21 + left.M23 * right.M31 + left.M24 * right.M41,
            left.M21 * right.M12 + left.M22 * right.M22 + left.M23 * right.M32 + left.M24 * right.M42,
            left.M21 * right.M13 + left.M22 * right.M23 + left.M23 * right.M33 + left.M24 * right.M43,
            left.M21 * right.M14 + left.M22 * right.M24 + left.M23 * right.M34 + left.M24 * right.M44,

            left.M31 * right.M11 + left.M32 * right.M21 + left.M33 * right.M31 + left.M34 * right.M41,
            left.M31 * right.M12 + left.M32 * right.M22 + left.M33 * right.M32 + left.M34 * right.M42,
            left.M31 * right.M13 + left.M32 * right.M23 + left.M33 * right.M33 + left.M34 * right.M43,
            left.M31 * right.M14 + left.M32 * right.M24 + left.M33 * right.M34 + left.M34 * right.M44,

            left.M41 * right.M11 + left.M42 * right.M21 + left.M43 * right.M31 + left.M44 * right.M41,
            left.M41 * right.M12 + left.M42 * right.M22 + left.M43 * right.M32 + left.M44 * right.M42,
            left.M41 * right.M13 + left.M42 * right.M23 + left.M43 * right.M33 + left.M44 * right.M43,
            left.M41 * right.M14 + left.M42 * right.M24 + left.M43 * right.M34 + left.M44 * right.M44);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 operator *(Matrix4x4 matrix, float scalar)
    {
        return new Matrix4x4(
            matrix.M11 * scalar, matrix.M12 * scalar, matrix.M13 * scalar, matrix.M14 * scalar,
            matrix.M21 * scalar, matrix.M22 * scalar, matrix.M23 * scalar, matrix.M24 * scalar,
            matrix.M31 * scalar, matrix.M32 * scalar, matrix.M33 * scalar, matrix.M34 * scalar,
            matrix.M41 * scalar, matrix.M42 * scalar, matrix.M43 * scalar, matrix.M44 * scalar);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 operator *(float scalar, Matrix4x4 matrix)
    {
        return matrix * scalar;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 operator -(Matrix4x4 matrix)
    {
        return matrix * -1f;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 矩阵与向量相乘，将向量视为列向量 (x, y, z, 1)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 TransformPoint(Vector3 point)
    {
        return new Vector3(
            M11 * point.X + M12 * point.Y + M13 * point.Z + M14,
            M21 * point.X + M22 * point.Y + M23 * point.Z + M24,
            M31 * point.X + M32 * point.Y + M33 * point.Z + M34);
    }

    /// <summary>
    /// 矩阵与方向向量相乘，忽略平移分量
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 TransformDirection(Vector3 direction)
    {
        return new Vector3(
            M11 * direction.X + M12 * direction.Y + M13 * direction.Z,
            M21 * direction.X + M22 * direction.Y + M23 * direction.Z,
            M31 * direction.X + M32 * direction.Y + M33 * direction.Z);
    }

    /// <summary>
    /// 计算矩阵的转置
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4x4 Transpose()
    {
        return new Matrix4x4(
            M11, M21, M31, M41,
            M12, M22, M32, M42,
            M13, M23, M33, M43,
            M14, M24, M34, M44);
    }

    /// <summary>
    /// 计算矩阵的行列式
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Determinant()
    {
        var a = M11 * ((M22 * M33 * M44 + M23 * M34 * M42 + M24 * M32 * M43)
                     - (M24 * M33 * M42 + M23 * M32 * M44 + M22 * M34 * M43));
        var b = M12 * ((M21 * M33 * M44 + M23 * M34 * M41 + M24 * M31 * M43)
                     - (M24 * M33 * M41 + M23 * M31 * M44 + M21 * M34 * M43));
        var c = M13 * ((M21 * M32 * M44 + M22 * M34 * M41 + M24 * M31 * M42)
                     - (M24 * M32 * M41 + M22 * M31 * M44 + M21 * M34 * M42));
        var d = M14 * ((M21 * M32 * M43 + M22 * M33 * M41 + M23 * M31 * M42)
                     - (M23 * M32 * M41 + M22 * M31 * M43 + M21 * M33 * M42));
        return a - b + c - d;
    }

    /// <summary>
    /// 计算矩阵的逆矩阵
    /// </summary>
    public Matrix4x4 Invert()
    {
        var det = Determinant();
        if (MathF.Abs(det) < MathHelper.Epsilon)
        {
            return Identity;
        }

        var invDet = 1f / det;

        var r11 = (M22 * M33 * M44 + M23 * M34 * M42 + M24 * M32 * M43
                 - M24 * M33 * M42 - M23 * M32 * M44 - M22 * M34 * M43) * invDet;
        var r12 = (M14 * M33 * M42 + M13 * M32 * M44 + M12 * M34 * M43
                 - M12 * M33 * M44 - M13 * M34 * M42 - M14 * M32 * M43) * invDet;
        var r13 = (M12 * M23 * M44 + M13 * M24 * M42 + M14 * M22 * M43
                 - M14 * M23 * M42 - M13 * M22 * M44 - M12 * M24 * M43) * invDet;
        var r14 = (M14 * M23 * M32 + M13 * M22 * M34 + M12 * M24 * M33
                 - M12 * M23 * M34 - M13 * M24 * M32 - M14 * M22 * M33) * invDet;

        var r21 = (M24 * M33 * M41 + M23 * M31 * M44 + M21 * M34 * M43
                 - M21 * M33 * M44 - M23 * M34 * M41 - M24 * M31 * M43) * invDet;
        var r22 = (M11 * M33 * M44 + M13 * M34 * M41 + M14 * M31 * M43
                 - M14 * M33 * M41 - M13 * M31 * M44 - M11 * M34 * M43) * invDet;
        var r23 = (M14 * M23 * M41 + M11 * M24 * M43 + M13 * M21 * M44
                 - M11 * M23 * M44 - M13 * M24 * M41 - M14 * M21 * M43) * invDet;
        var r24 = (M11 * M23 * M34 + M13 * M24 * M31 + M14 * M21 * M33
                 - M14 * M23 * M31 - M13 * M21 * M34 - M11 * M24 * M33) * invDet;

        var r31 = (M21 * M32 * M44 + M22 * M34 * M41 + M24 * M31 * M42
                 - M24 * M32 * M41 - M22 * M31 * M44 - M21 * M34 * M42) * invDet;
        var r32 = (M14 * M32 * M41 + M12 * M31 * M44 + M11 * M34 * M42
                 - M11 * M32 * M44 - M12 * M34 * M41 - M14 * M31 * M42) * invDet;
        var r33 = (M11 * M22 * M44 + M12 * M24 * M41 + M14 * M21 * M42
                 - M14 * M22 * M41 - M12 * M21 * M44 - M11 * M24 * M42) * invDet;
        var r34 = (M12 * M21 * M34 + M14 * M22 * M31 + M11 * M24 * M32
                 - M11 * M22 * M34 - M14 * M21 * M32 - M12 * M24 * M31) * invDet;

        var r41 = (M23 * M32 * M41 + M22 * M31 * M43 + M21 * M33 * M42
                 - M21 * M32 * M43 - M23 * M31 * M42 - M22 * M33 * M41) * invDet;
        var r42 = (M13 * M32 * M41 + M12 * M33 * M41 + M11 * M32 * M43
                 - M11 * M33 * M42 - M13 * M31 * M42 - M12 * M31 * M43) * invDet;
        var r43 = (M13 * M21 * M42 + M11 * M23 * M41 + M12 * M21 * M43
                 - M12 * M23 * M41 - M13 * M21 * M42 - M11 * M22 * M43) * invDet;
        var r44 = (M12 * M23 * M31 + M11 * M22 * M33 + M13 * M21 * M32
                 - M13 * M22 * M31 - M12 * M21 * M33 - M11 * M23 * M32) * invDet;

        return new Matrix4x4(
            r11, r12, r13, r14,
            r21, r22, r23, r24,
            r31, r32, r33, r34,
            r41, r42, r43, r44);
    }

    #endregion

    #region 工厂方法

    /// <summary>
    /// 创建平移矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTranslation(Vector3 translation)
    {
        return CreateTranslation(translation.X, translation.Y, translation.Z);
    }

    /// <summary>
    /// 创建平移矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTranslation(float x, float y, float z)
    {
        return new Matrix4x4(
            1f, 0f, 0f, x,
            0f, 1f, 0f, y,
            0f, 0f, 1f, z,
            0f, 0f, 0f, 1f);
    }

    /// <summary>
    /// 创建缩放矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateScale(Vector3 scale)
    {
        return CreateScale(scale.X, scale.Y, scale.Z);
    }

    /// <summary>
    /// 创建缩放矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateScale(float x, float y, float z)
    {
        return new Matrix4x4(
            x, 0f, 0f, 0f,
            0f, y, 0f, 0f,
            0f, 0f, z, 0f,
            0f, 0f, 0f, 1f);
    }

    /// <summary>
    /// 创建均匀缩放矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateScale(float scale)
    {
        return CreateScale(scale, scale, scale);
    }

    /// <summary>
    /// 绕 X 轴旋转矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateRotationX(float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new Matrix4x4(
            1f, 0f, 0f, 0f,
            0f, cos, -sin, 0f,
            0f, sin, cos, 0f,
            0f, 0f, 0f, 1f);
    }

    /// <summary>
    /// 绕 Y 轴旋转矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateRotationY(float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new Matrix4x4(
            cos, 0f, sin, 0f,
            0f, 1f, 0f, 0f,
            -sin, 0f, cos, 0f,
            0f, 0f, 0f, 1f);
    }

    /// <summary>
    /// 绕 Z 轴旋转矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateRotationZ(float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new Matrix4x4(
            cos, -sin, 0f, 0f,
            sin, cos, 0f, 0f,
            0f, 0f, 1f, 0f,
            0f, 0f, 0f, 1f);
    }

    /// <summary>
    /// 从四元数创建旋转矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateFromQuaternion(Quaternion quaternion)
    {
        var xx = quaternion.X * quaternion.X;
        var yy = quaternion.Y * quaternion.Y;
        var zz = quaternion.Z * quaternion.Z;
        var xy = quaternion.X * quaternion.Y;
        var xz = quaternion.X * quaternion.Z;
        var yz = quaternion.Y * quaternion.Z;
        var wx = quaternion.W * quaternion.X;
        var wy = quaternion.W * quaternion.Y;
        var wz = quaternion.W * quaternion.Z;

        return new Matrix4x4(
            1f - 2f * (yy + zz), 2f * (xy - wz), 2f * (xz + wy), 0f,
            2f * (xy + wz), 1f - 2f * (xx + zz), 2f * (yz - wx), 0f,
            2f * (xz - wy), 2f * (yz + wx), 1f - 2f * (xx + yy), 0f,
            0f, 0f, 0f, 1f);
    }

    /// <summary>
    /// 创建透视投影矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreatePerspectiveFieldOfView(float fieldOfView, float aspectRatio, float nearPlaneDistance, float farPlaneDistance)
    {
        if (fieldOfView <= 0f || fieldOfView >= MathF.PI)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldOfView), "视野角度必须在 0 到 π 之间");
        }

        if (nearPlaneDistance <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance), "近裁面距离必须大于 0");
        }

        if (farPlaneDistance <= nearPlaneDistance)
        {
            throw new ArgumentOutOfRangeException(nameof(farPlaneDistance), "远裁面距离必须大于近裁面距离");
        }

        if (aspectRatio <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(aspectRatio), "宽高比必须大于 0");
        }

        var yScale = 1f / MathF.Tan(fieldOfView * 0.5f);
        var xScale = yScale / aspectRatio;
        var depthRange = farPlaneDistance - nearPlaneDistance;

        return new Matrix4x4(
            xScale, 0f, 0f, 0f,
            0f, yScale, 0f, 0f,
            0f, 0f, farPlaneDistance / depthRange, -nearPlaneDistance * farPlaneDistance / depthRange,
            0f, 0f, 1f, 0f);
    }

    /// <summary>
    /// 创建正交投影矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateOrthographic(float width, float height, float nearPlaneDistance, float farPlaneDistance)
    {
        if (width <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "宽度必须大于 0");
        }

        if (height <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "高度必须大于 0");
        }

        var depthRange = farPlaneDistance - nearPlaneDistance;

        return new Matrix4x4(
            2f / width, 0f, 0f, 0f,
            0f, 2f / height, 0f, 0f,
            0f, 0f, 1f / depthRange, -nearPlaneDistance / depthRange,
            0f, 0f, 0f, 1f);
    }

    /// <summary>
    /// 创建观察矩阵
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
    {
        var zAxis = (cameraPosition - cameraTarget).Normalize();
        var xAxis = Vector3.Cross(cameraUpVector, zAxis).Normalize();
        var yAxis = Vector3.Cross(zAxis, xAxis);

        return new Matrix4x4(
            xAxis.X, xAxis.Y, xAxis.Z, -Vector3.Dot(xAxis, cameraPosition),
            yAxis.X, yAxis.Y, yAxis.Z, -Vector3.Dot(yAxis, cameraPosition),
            zAxis.X, zAxis.Y, zAxis.Z, -Vector3.Dot(zAxis, cameraPosition),
            0f, 0f, 0f, 1f);
    }

    #endregion
}
