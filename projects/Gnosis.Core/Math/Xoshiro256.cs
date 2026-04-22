using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// Xoshiro256** 伪随机数生成器，高质量、快速、可重现
/// </summary>
public sealed class Xoshiro256
{
    #region 字段

    private ulong _s0;
    private ulong _s1;
    private ulong _s2;
    private ulong _s3;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用默认种子创建随机数生成器
    /// </summary>
    public Xoshiro256()
    {
        var seed = (ulong)DateTime.UtcNow.Ticks;
        Seed(seed);
    }

    /// <summary>
    /// 使用指定种子创建随机数生成器
    /// </summary>
    public Xoshiro256(ulong seed)
    {
        Seed(seed);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 设置随机数种子，使用 SplitMix64 算法展开种子
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Seed(ulong seed)
    {
        _s0 = SplitMix64(ref seed);
        _s1 = SplitMix64(ref seed);
        _s2 = SplitMix64(ref seed);
        _s3 = SplitMix64(ref seed);
    }

    /// <summary>
    /// 生成 [0, ulong.MaxValue] 范围的随机数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong NextUInt64()
    {
        var result = Rotl(_s1 * 5, 7) * 9;
        var t = _s1 << 17;

        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;

        _s2 ^= t;
        _s3 = Rotl(_s3, 45);

        return result;
    }

    /// <summary>
    /// 生成 [0, 1) 范围的随机浮点数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextFloat()
    {
        return (NextUInt64() >> 40) * (1f / (1u << 24));
    }

    /// <summary>
    /// 生成 [0, 1) 范围的随机双精度浮点数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double NextDouble()
    {
        return (NextUInt64() >> 11) * (1.0 / (1ul << 53));
    }

    /// <summary>
    /// 生成 [min, max) 范围的随机整数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextInt(int min, int max)
    {
        if (min >= max)
        {
            throw new ArgumentException("最小值必须小于最大值");
        }

        var range = (uint)(max - min);
        return min + (int)(NextUInt64() % range);
    }

    /// <summary>
    /// 生成 [min, max) 范围的随机浮点数
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float NextFloat(float min, float max)
    {
        return min + (max - min) * NextFloat();
    }

    /// <summary>
    /// 生成单位圆内的随机二维方向
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector2 NextVector2()
    {
        var angle = NextFloat() * MathHelper.TwoPi;
        return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
    }

    /// <summary>
    /// 生成单位球面上的随机三维方向
    /// </summary>
    public Vector3 NextVector3()
    {
        var z = NextFloat() * 2f - 1f;
        var r = MathF.Sqrt(1f - z * z);
        var angle = NextFloat() * MathHelper.TwoPi;
        return new Vector3(r * MathF.Cos(angle), r * MathF.Sin(angle), z);
    }

    #endregion

    #region 私有方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Rotl(ulong x, int k)
    {
        return (x << k) | (x >> (64 - k));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong SplitMix64(ref ulong state)
    {
        var z = state + 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    #endregion
}
