using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// 数学常量与工具方法
/// </summary>
public static class MathHelper
{
    /// <summary>
    /// 圆周率 π
    /// </summary>
    public const float Pi = MathF.PI;

    /// <summary>
    /// π / 2
    /// </summary>
    public const float PiOver2 = MathF.PI / 2f;

    /// <summary>
    /// π / 4
    /// </summary>
    public const float PiOver4 = MathF.PI / 4f;

    /// <summary>
    /// 2π
    /// </summary>
    public const float TwoPi = MathF.PI * 2f;

    /// <summary>
    /// 角度转弧度
    /// </summary>
    public const float Deg2Rad = MathF.PI / 180f;

    /// <summary>
    /// 弧度转角度
    /// </summary>
    public const float Rad2Deg = 180f / MathF.PI;

    /// <summary>
    /// 浮点比较容差
    /// </summary>
    public const float Epsilon = 1e-6f;

    /// <summary>
    /// 将值限制在 [min, max] 范围内
    /// </summary>
    /// <param name="value">输入值</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <returns>限制后的值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float value, float min, float max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    /// <summary>
    /// 将值限制在 [min, max] 范围内（整数版本）
    /// </summary>
    /// <param name="value">输入值</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <returns>限制后的值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    /// <summary>
    /// 线性插值，t 被限制在 [0, 1]
    /// </summary>
    /// <param name="a">起始值</param>
    /// <param name="b">结束值</param>
    /// <param name="t">插值因子</param>
    /// <returns>插值结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Clamp(t, 0f, 1f);
    }

    /// <summary>
    /// 线性插值，t 不受限制
    /// </summary>
    /// <param name="a">起始值</param>
    /// <param name="b">结束值</param>
    /// <param name="t">插值因子</param>
    /// <returns>插值结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float LerpUnclamped(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    /// <summary>
    /// 平滑阶梯插值，在 [0, 1] 之间使用 Hermite 多项式平滑过渡
    /// </summary>
    /// <param name="a">起始值</param>
    /// <param name="b">结束值</param>
    /// <param name="t">插值因子</param>
    /// <returns>平滑插值结果</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SmoothStep(float a, float b, float t)
    {
        var clamped = Clamp(t, 0f, 1f);
        var s = clamped * clamped * (3f - 2f * clamped);
        return a + (b - a) * s;
    }

    /// <summary>
    /// 将当前值向目标值移动指定最大距离
    /// </summary>
    /// <param name="current">当前值</param>
    /// <param name="target">目标值</param>
    /// <param name="maxDelta">最大移动距离</param>
    /// <returns>移动后的值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
        {
            return target;
        }

        return current + MathF.Sign(target - current) * maxDelta;
    }

    /// <summary>
    /// 将值循环限制在 [0, length] 范围内
    /// </summary>
    /// <param name="t">输入值</param>
    /// <param name="length">循环长度</param>
    /// <returns>循环后的值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Repeat(float t, float length)
    {
        return Clamp(t - MathF.Floor(t / length) * length, 0f, length);
    }

    /// <summary>
    /// 将值在 [0, length] 范围内来回反射
    /// </summary>
    /// <param name="t">输入值</param>
    /// <param name="length">反射长度</param>
    /// <returns>反射后的值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float PingPong(float t, float length)
    {
        t = Repeat(t, length * 2f);
        return length - MathF.Abs(t - length);
    }

    /// <summary>
    /// 判断两个浮点数是否近似相等
    /// </summary>
    /// <param name="a">第一个值</param>
    /// <param name="b">第二个值</param>
    /// <returns>是否近似相等</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Approximately(float a, float b)
    {
        return MathF.Abs(b - a) < MathF.Max(1e-6f * MathF.Max(MathF.Abs(a), MathF.Abs(b)), Epsilon * 8f);
    }
}
