using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// Simplex 噪声生成器，支持 2D 和 3D
/// </summary>
public sealed class SimplexNoise
{
    #region 常量

    private static readonly float F2 = 0.5f * (MathF.Sqrt(3f) - 1f);
    private static readonly float G2 = (3f - MathF.Sqrt(3f)) / 6f;
    private const float F3 = 1f / 3f;
    private const float G3 = 1f / 6f;

    #endregion

    #region 字段

    private readonly int[] _perm;
    private readonly int[] _permMod12;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用默认种子创建 Simplex 噪声生成器
    /// </summary>
    public SimplexNoise()
    {
        _perm = new int[512];
        _permMod12 = new int[512];
        var rng = new Xoshiro256(42);
        Initialize(rng);
    }

    /// <summary>
    /// 使用指定种子创建 Simplex 噪声生成器
    /// </summary>
    public SimplexNoise(ulong seed)
    {
        _perm = new int[512];
        _permMod12 = new int[512];
        var rng = new Xoshiro256(seed);
        Initialize(rng);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 计算 2D Simplex 噪声值，返回 [-1, 1]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Noise2D(float x, float y)
    {
        var s = (x + y) * F2;
        var i = FloorToInt(x + s);
        var j = FloorToInt(y + s);

        var t = (i + j) * G2;
        var x0 = x - (i - t);
        var y0 = y - (j - t);

        int i1;
        int j1;

        if (x0 > y0)
        {
            i1 = 1;
            j1 = 0;
        }
        else
        {
            i1 = 0;
            j1 = 1;
        }

        var x1 = x0 - i1 + G2;
        var y1 = y0 - j1 + G2;
        var x2 = x0 - 1f + 2f * G2;
        var y2 = y0 - 1f + 2f * G2;

        var ii = i & 255;
        var jj = j & 255;

        var n0 = 0f;
        var n1 = 0f;
        var n2 = 0f;

        var t0 = 0.5f - x0 * x0 - y0 * y0;
        if (t0 >= 0f)
        {
            var gi0 = _permMod12[ii + _perm[jj]];
            t0 *= t0;
            n0 = t0 * t0 * Dot2D(gi0, x0, y0);
        }

        var t1 = 0.5f - x1 * x1 - y1 * y1;
        if (t1 >= 0f)
        {
            var gi1 = _permMod12[ii + i1 + _perm[jj + j1]];
            t1 *= t1;
            n1 = t1 * t1 * Dot2D(gi1, x1, y1);
        }

        var t2 = 0.5f - x2 * x2 - y2 * y2;
        if (t2 >= 0f)
        {
            var gi2 = _permMod12[ii + 1 + _perm[jj + 1]];
            t2 *= t2;
            n2 = t2 * t2 * Dot2D(gi2, x2, y2);
        }

        return 70f * (n0 + n1 + n2);
    }

    /// <summary>
    /// 计算 3D Simplex 噪声值，返回 [-1, 1]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Noise3D(float x, float y, float z)
    {
        var s = (x + y + z) * F3;
        var i = FloorToInt(x + s);
        var j = FloorToInt(y + s);
        var k = FloorToInt(z + s);

        var t = (i + j + k) * G3;
        var x0 = x - (i - t);
        var y0 = y - (j - t);
        var z0 = z - (k - t);

        int i1;
        int j1;
        int k1;
        int i2;
        int j2;
        int k2;

        if (x0 >= y0)
        {
            if (y0 >= z0)
            {
                i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0;
            }
            else if (x0 >= z0)
            {
                i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1;
            }
            else
            {
                i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1;
            }
        }
        else
        {
            if (y0 < z0)
            {
                i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1;
            }
            else if (x0 < z0)
            {
                i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1;
            }
            else
            {
                i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0;
            }
        }

        var x1 = x0 - i1 + G3;
        var y1 = y0 - j1 + G3;
        var z1 = z0 - k1 + G3;
        var x2 = x0 - i2 + 2f * G3;
        var y2 = y0 - j2 + 2f * G3;
        var z2 = z0 - k2 + 2f * G3;
        var x3 = x0 - 1f + 3f * G3;
        var y3 = y0 - 1f + 3f * G3;
        var z3 = z0 - 1f + 3f * G3;

        var ii = i & 255;
        var jj = j & 255;
        var kk = k & 255;

        var n0 = 0f;
        var n1 = 0f;
        var n2 = 0f;
        var n3 = 0f;

        var t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
        if (t0 >= 0f)
        {
            var gi0 = _permMod12[ii + _perm[jj + _perm[kk]]];
            t0 *= t0;
            n0 = t0 * t0 * Dot3D(gi0, x0, y0, z0);
        }

        var t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
        if (t1 >= 0f)
        {
            var gi1 = _permMod12[ii + i1 + _perm[jj + j1 + _perm[kk + k1]]];
            t1 *= t1;
            n1 = t1 * t1 * Dot3D(gi1, x1, y1, z1);
        }

        var t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
        if (t2 >= 0f)
        {
            var gi2 = _permMod12[ii + i2 + _perm[jj + j2 + _perm[kk + k2]]];
            t2 *= t2;
            n2 = t2 * t2 * Dot3D(gi2, x2, y2, z2);
        }

        var t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
        if (t3 >= 0f)
        {
            var gi3 = _permMod12[ii + 1 + _perm[jj + 1 + _perm[kk + 1]]];
            t3 *= t3;
            n3 = t3 * t3 * Dot3D(gi3, x3, y3, z3);
        }

        return 32f * (n0 + n1 + n2 + n3);
    }

    #endregion

    #region 私有方法

    private void Initialize(Xoshiro256 rng)
    {
        var p = new int[256];
        for (var i = 0; i < 256; i++)
        {
            p[i] = i;
        }

        for (var i = 255; i > 0; i--)
        {
            var j = rng.NextInt(0, i + 1);
            (p[i], p[j]) = (p[j], p[i]);
        }

        for (var i = 0; i < 256; i++)
        {
            _perm[i] = p[i];
            _perm[256 + i] = p[i];
            _permMod12[i] = p[i] % 12;
            _permMod12[256 + i] = p[i] % 12;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FloorToInt(float f)
    {
        return f >= 0f ? (int)f : (int)f - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Dot2D(int g, float x, float y)
    {
        return Gradients2D[g, 0] * x + Gradients2D[g, 1] * y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Dot3D(int g, float x, float y, float z)
    {
        return Gradients3D[g, 0] * x + Gradients3D[g, 1] * y + Gradients3D[g, 2] * z;
    }

    private static readonly float[,] Gradients2D = {
        { 1f, 1f }, { -1f, 1f }, { 1f, -1f }, { -1f, -1f },
        { 1f, 0f }, { -1f, 0f }, { 0f, 1f }, { 0f, -1f },
        { 1f, 1f }, { -1f, 1f }, { 1f, -1f }, { -1f, -1f }
    };

    private static readonly float[,] Gradients3D = {
        { 1f, 1f, 0f }, { -1f, 1f, 0f }, { 1f, -1f, 0f }, { -1f, -1f, 0f },
        { 1f, 0f, 1f }, { -1f, 0f, 1f }, { 1f, 0f, -1f }, { -1f, 0f, -1f },
        { 0f, 1f, 1f }, { 0f, -1f, 1f }, { 0f, 1f, -1f }, { 0f, -1f, -1f }
    };

    #endregion
}
