using System.Numerics;

namespace Gnosis.ECS.Simd;

/// <summary>
/// SIMD 批量操作辅助类，利用硬件向量指令加速组件数据的批量处理。
/// 适用于 Position/Velocity 等基于 float 的组件在 Chunk 内的批量运算。
/// </summary>
public static class SimdBatch
{
    #region 单精度浮点批量运算

    /// <summary>
    /// 批量加法：result[i] = a[i] + b[i]
    /// </summary>
    public static void Add(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> result)
    {
        var count = Math.Min(Math.Min(a.Length, b.Length), result.Length);
        var vectorSize = Vector<float>.Count;
        var i = 0;

        for (; i <= count - vectorSize; i += vectorSize)
        {
            var va = new Vector<float>(a.Slice(i, vectorSize));
            var vb = new Vector<float>(b.Slice(i, vectorSize));
            (va + vb).CopyTo(result.Slice(i, vectorSize));
        }

        for (; i < count; i++)
        {
            result[i] = a[i] + b[i];
        }
    }

    /// <summary>
    /// 批量乘加：result[i] = a[i] + b[i] * scalar
    /// 典型用途：position[i] += velocity[i] * deltaTime
    /// </summary>
    public static void MultiplyAdd(ReadOnlySpan<float> a, ReadOnlySpan<float> b, float scalar, Span<float> result)
    {
        var count = Math.Min(Math.Min(a.Length, b.Length), result.Length);
        var vectorSize = Vector<float>.Count;
        var vScalar = new Vector<float>(scalar);
        var i = 0;

        for (; i <= count - vectorSize; i += vectorSize)
        {
            var va = new Vector<float>(a.Slice(i, vectorSize));
            var vb = new Vector<float>(b.Slice(i, vectorSize));
            (va + vb * vScalar).CopyTo(result.Slice(i, vectorSize));
        }

        for (; i < count; i++)
        {
            result[i] = a[i] + b[i] * scalar;
        }
    }

    /// <summary>
    /// 批量缩放：result[i] = a[i] * scalar
    /// </summary>
    public static void Multiply(ReadOnlySpan<float> a, float scalar, Span<float> result)
    {
        var count = Math.Min(a.Length, result.Length);
        var vectorSize = Vector<float>.Count;
        var vScalar = new Vector<float>(scalar);
        var i = 0;

        for (; i <= count - vectorSize; i += vectorSize)
        {
            var va = new Vector<float>(a.Slice(i, vectorSize));
            (va * vScalar).CopyTo(result.Slice(i, vectorSize));
        }

        for (; i < count; i++)
        {
            result[i] = a[i] * scalar;
        }
    }

    /// <summary>
    /// 批量线性插值：result[i] = a[i] + (b[i] - a[i]) * t
    /// </summary>
    public static void Lerp(ReadOnlySpan<float> a, ReadOnlySpan<float> b, float t, Span<float> result)
    {
        var count = Math.Min(Math.Min(a.Length, b.Length), result.Length);
        var vectorSize = Vector<float>.Count;
        var vt = new Vector<float>(t);
        var i = 0;

        for (; i <= count - vectorSize; i += vectorSize)
        {
            var va = new Vector<float>(a.Slice(i, vectorSize));
            var vb = new Vector<float>(b.Slice(i, vectorSize));
            (va + (vb - va) * vt).CopyTo(result.Slice(i, vectorSize));
        }

        for (; i < count; i++)
        {
            result[i] = a[i] + (b[i] - a[i]) * t;
        }
    }

    /// <summary>
    /// 批量 clamp：result[i] = Math.Clamp(value[i], min[i], max[i])
    /// </summary>
    public static void Clamp(ReadOnlySpan<float> value, ReadOnlySpan<float> min, ReadOnlySpan<float> max, Span<float> result)
    {
        var count = Math.Min(Math.Min(Math.Min(value.Length, min.Length), max.Length), result.Length);
        var vectorSize = Vector<float>.Count;
        var i = 0;

        for (; i <= count - vectorSize; i += vectorSize)
        {
            var v = new Vector<float>(value.Slice(i, vectorSize));
            var vMin = new Vector<float>(min.Slice(i, vectorSize));
            var vMax = new Vector<float>(max.Slice(i, vectorSize));
            Vector.Max(Vector.Min(v, vMax), vMin).CopyTo(result.Slice(i, vectorSize));
        }

        for (; i < count; i++)
        {
            result[i] = Math.Clamp(value[i], min[i], max[i]);
        }
    }

    /// <summary>
    /// 批量标量 clamp：result[i] = Math.Clamp(value[i], minVal, maxVal)
    /// </summary>
    public static void Clamp(ReadOnlySpan<float> value, float minVal, float maxVal, Span<float> result)
    {
        var count = Math.Min(value.Length, result.Length);
        var vectorSize = Vector<float>.Count;
        var vMin = new Vector<float>(minVal);
        var vMax = new Vector<float>(maxVal);
        var i = 0;

        for (; i <= count - vectorSize; i += vectorSize)
        {
            var v = new Vector<float>(value.Slice(i, vectorSize));
            Vector.Max(Vector.Min(v, vMax), vMin).CopyTo(result.Slice(i, vectorSize));
        }

        for (; i < count; i++)
        {
            result[i] = Math.Clamp(value[i], minVal, maxVal);
        }
    }

    #endregion

    #region Chunk 级批量运算

    /// <summary>
    /// 对 Chunk 内的 float 组件数组执行乘加操作。
    /// 典型用途：Position.X += Velocity.X * deltaTime
    /// </summary>
    public static void ChunkMultiplyAdd(float[] target, float[] operand, float scalar, int count)
    {
        var vectorSize = Vector<float>.Count;
        var vScalar = new Vector<float>(scalar);
        var i = 0;

        for (; i <= count - vectorSize; i += vectorSize)
        {
            var vTarget = new Vector<float>(target, i);
            var vOperand = new Vector<float>(operand, i);
            (vTarget + vOperand * vScalar).CopyTo(target, i);
        }

        for (; i < count; i++)
        {
            target[i] += operand[i] * scalar;
        }
    }

    /// <summary>
    /// 对 Chunk 内的 float 组件数组执行缩放操作
    /// </summary>
    public static void ChunkMultiply(float[] target, float scalar, int count)
    {
        var vectorSize = Vector<float>.Count;
        var vScalar = new Vector<float>(scalar);
        var i = 0;

        for (; i <= count - vectorSize; i += vectorSize)
        {
            var vTarget = new Vector<float>(target, i);
            (vTarget * vScalar).CopyTo(target, i);
        }

        for (; i < count; i++)
        {
            target[i] *= scalar;
        }
    }

    /// <summary>
    /// 对 Chunk 内的 float 组件数组执行加法操作
    /// </summary>
    public static void ChunkAdd(float[] target, float[] operand, int count)
    {
        var vectorSize = Vector<float>.Count;
        var i = 0;

        for (; i <= count - vectorSize; i += vectorSize)
        {
            var vTarget = new Vector<float>(target, i);
            var vOperand = new Vector<float>(operand, i);
            (vTarget + vOperand).CopyTo(target, i);
        }

        for (; i < count; i++)
        {
            target[i] += operand[i];
        }
    }

    #endregion

    #region 诊断

    /// <summary>
    /// 当前硬件是否支持 SIMD 加速
    /// </summary>
    public static bool IsSimdSupported => Vector.IsHardwareAccelerated;

    /// <summary>
    /// 当前硬件的 float 向量宽度
    /// </summary>
    public static int VectorFloatCount => Vector<float>.Count;

    #endregion
}
