using System.Runtime.CompilerServices;

namespace Gnosis.Core.Math;

/// <summary>
/// Perlin 噪声生成器，支持 2D 和 3D
/// </summary>
public sealed class PerlinNoise
{
    #region 字段

    private readonly int[] _permutation;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用默认种子创建 Perlin 噪声生成器
    /// </summary>
    public PerlinNoise()
    {
        _permutation = new int[512];
        var rng = new Xoshiro256(42);
        Initialize(rng);
    }

    /// <summary>
    /// 使用指定种子创建 Perlin 噪声生成器
    /// </summary>
    public PerlinNoise(ulong seed)
    {
        _permutation = new int[512];
        var rng = new Xoshiro256(seed);
        Initialize(rng);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 计算 2D Perlin 噪声值，返回 [-1, 1]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Noise2D(float x, float y)
    {
        var xi = FloorToInt(x) & 255;
        var yi = FloorToInt(y) & 255;

        var xf = x - FloorToInt(x);
        var yf = y - FloorToInt(y);

        var u = Fade(xf);
        var v = Fade(yf);

        var aa = _permutation[_permutation[xi] + yi];
        var ab = _permutation[_permutation[xi] + yi + 1];
        var ba = _permutation[_permutation[xi + 1] + yi];
        var bb = _permutation[_permutation[xi + 1] + yi + 1];

        var x1 = MathHelper.Lerp(Grad2D(aa, xf, yf), Grad2D(ba, xf - 1, yf), u);
        var x2 = MathHelper.Lerp(Grad2D(ab, xf, yf - 1), Grad2D(bb, xf - 1, yf - 1), u);

        return MathHelper.Lerp(x1, x2, v);
    }

    /// <summary>
    /// 计算 3D Perlin 噪声值，返回 [-1, 1]
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Noise3D(float x, float y, float z)
    {
        var xi = FloorToInt(x) & 255;
        var yi = FloorToInt(y) & 255;
        var zi = FloorToInt(z) & 255;

        var xf = x - FloorToInt(x);
        var yf = y - FloorToInt(y);
        var zf = z - FloorToInt(z);

        var u = Fade(xf);
        var v = Fade(yf);
        var w = Fade(zf);

        var aaa = _permutation[_permutation[_permutation[xi] + yi] + zi];
        var aba = _permutation[_permutation[_permutation[xi] + yi + 1] + zi];
        var aab = _permutation[_permutation[_permutation[xi] + yi] + zi + 1];
        var abb = _permutation[_permutation[_permutation[xi] + yi + 1] + zi + 1];
        var baa = _permutation[_permutation[_permutation[xi + 1] + yi] + zi];
        var bba = _permutation[_permutation[_permutation[xi + 1] + yi + 1] + zi];
        var bab = _permutation[_permutation[_permutation[xi + 1] + yi] + zi + 1];
        var bbb = _permutation[_permutation[_permutation[xi + 1] + yi + 1] + zi + 1];

        var x1 = MathHelper.Lerp(Grad3D(aaa, xf, yf, zf), Grad3D(baa, xf - 1, yf, zf), u);
        var x2 = MathHelper.Lerp(Grad3D(aba, xf, yf - 1, zf), Grad3D(bba, xf - 1, yf - 1, zf), u);
        var y1 = MathHelper.Lerp(x1, x2, v);

        x1 = MathHelper.Lerp(Grad3D(aab, xf, yf, zf - 1), Grad3D(bab, xf - 1, yf, zf - 1), u);
        x2 = MathHelper.Lerp(Grad3D(abb, xf, yf - 1, zf - 1), Grad3D(bbb, xf - 1, yf - 1, zf - 1), u);
        var y2 = MathHelper.Lerp(x1, x2, v);

        return MathHelper.Lerp(y1, y2, w);
    }

    /// <summary>
    /// 计算分形布朗运动（FBM）2D 噪声
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Fbm2D(float x, float y, int octaves = 4, float lacunarity = 2f, float persistence = 0.5f)
    {
        var sum = 0f;
        var amplitude = 1f;
        var frequency = 1f;
        var maxAmplitude = 0f;

        for (var i = 0; i < octaves; i++)
        {
            sum += Noise2D(x * frequency, y * frequency) * amplitude;
            maxAmplitude += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return sum / maxAmplitude;
    }

    /// <summary>
    /// 计算分形布朗运动（FBM）3D 噪声
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Fbm3D(float x, float y, float z, int octaves = 4, float lacunarity = 2f, float persistence = 0.5f)
    {
        var sum = 0f;
        var amplitude = 1f;
        var frequency = 1f;
        var maxAmplitude = 0f;

        for (var i = 0; i < octaves; i++)
        {
            sum += Noise3D(x * frequency, y * frequency, z * frequency) * amplitude;
            maxAmplitude += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return sum / maxAmplitude;
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
            _permutation[i] = p[i];
            _permutation[256 + i] = p[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FloorToInt(float f)
    {
        return f >= 0f ? (int)f : (int)f - 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Grad2D(int hash, float x, float y)
    {
        var h = hash & 3;
        return ((h & 1) == 0 ? x : -x) + ((h & 2) == 0 ? y : -y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Grad3D(int hash, float x, float y, float z)
    {
        var h = hash & 15;
        var u = h < 8 ? x : y;
        var v = h < 4 ? y : h == 12 || h == 14 ? x : z;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    #endregion
}
