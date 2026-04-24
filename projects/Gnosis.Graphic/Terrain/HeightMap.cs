using System.Numerics;

namespace Gnosis.Graphic.Terrain;

public sealed class HeightMap
{
    #region 字段

    private readonly float[] _heights;

    #endregion

    #region 属性

    public int Width { get; }
    public int Height { get; }
    public float MinHeight { get; private set; }
    public float MaxHeight { get; private set; }
    public float HeightRange => MaxHeight - MinHeight;

    #endregion

    #region 构造函数

    public HeightMap(int width, int height)
    {
        Width = width;
        Height = height;
        _heights = new float[width * height];
        MinHeight = 0.0f;
        MaxHeight = 0.0f;
    }

    public HeightMap(int width, int height, float[] heights)
    {
        if (heights.Length != width * height)
        {
            throw new ArgumentException($"高度图数据长度 {heights.Length} 与尺寸 {width}x{height} 不匹配", nameof(heights));
        }

        Width = width;
        Height = height;
        _heights = new float[heights.Length];
        Array.Copy(heights, _heights, heights.Length);
        RecalculateBounds();
    }

    #endregion

    #region 索引器

    public float this[int x, int y]
    {
        get
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return 0.0f;
            }

            return _heights[y * Width + x];
        }
        set
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
            {
                return;
            }

            _heights[y * Width + x] = value;
        }
    }

    #endregion

    #region 公开方法

    public float SampleHeight(float u, float v)
    {
        var x = u * (Width - 1);
        var y = v * (Height - 1);

        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = Math.Min(x0 + 1, Width - 1);
        var y1 = Math.Min(y0 + 1, Height - 1);

        x0 = Math.Clamp(x0, 0, Width - 1);
        y0 = Math.Clamp(y0, 0, Height - 1);

        var fx = x - x0;
        var fy = y - y0;

        var h00 = _heights[y0 * Width + x0];
        var h10 = _heights[y0 * Width + x1];
        var h01 = _heights[y1 * Width + x0];
        var h11 = _heights[y1 * Width + x1];

        var h0 = h00 * (1.0f - fx) + h10 * fx;
        var h1 = h01 * (1.0f - fx) + h11 * fx;

        return h0 * (1.0f - fy) + h1 * fy;
    }

    public Vector3 SampleNormal(float u, float v, float cellSize)
    {
        var eps = 1.0f / Math.Max(Width, Height);

        var hL = SampleHeight(Math.Clamp(u - eps, 0.0f, 1.0f), v);
        var hR = SampleHeight(Math.Clamp(u + eps, 0.0f, 1.0f), v);
        var hD = SampleHeight(u, Math.Clamp(v - eps, 0.0f, 1.0f));
        var hU = SampleHeight(u, Math.Clamp(v + eps, 0.0f, 1.0f));

        var normal = new Vector3((hL - hR) * cellSize, 2.0f * eps, (hD - hU) * cellSize);
        return Vector3.Normalize(normal);
    }

    public Vector3 GetWorldPosition(float u, float v, float terrainSize, float heightScale)
    {
        var x = (u - 0.5f) * terrainSize;
        var z = (v - 0.5f) * terrainSize;
        var y = SampleHeight(u, v) * heightScale;
        return new Vector3(x, y, z);
    }

    public static HeightMap FromRawData(byte[] data, int width, int height, float heightScale = 1.0f, float heightOffset = 0.0f)
    {
        if (data.Length != width * height * sizeof(float))
        {
            throw new ArgumentException($"原始数据长度 {data.Length} 与预期 {width * height * sizeof(float)} 不匹配", nameof(data));
        }

        var heights = new float[width * height];
        for (var i = 0; i < heights.Length; i++)
        {
            heights[i] = BitConverter.ToSingle(data, i * sizeof(float)) * heightScale + heightOffset;
        }

        return new HeightMap(width, height, heights);
    }

    public static HeightMap FromR16Data(byte[] data, int width, int height, float heightScale = 1.0f / 65535.0f, float heightOffset = 0.0f)
    {
        if (data.Length != width * height * sizeof(ushort))
        {
            throw new ArgumentException($"R16 数据长度 {data.Length} 与预期 {width * height * sizeof(ushort)} 不匹配", nameof(data));
        }

        var heights = new float[width * height];
        for (var i = 0; i < heights.Length; i++)
        {
            var raw = BitConverter.ToUInt16(data, i * sizeof(ushort));
            heights[i] = raw * heightScale + heightOffset;
        }

        return new HeightMap(width, height, heights);
    }

    public static HeightMap GenerateFlat(int width, int height, float flatHeight = 0.0f)
    {
        var heights = new float[width * height];
        Array.Fill(heights, flatHeight);
        return new HeightMap(width, height, heights);
    }

    public static HeightMap GeneratePerlinNoise(int width, int height, float scale, int octaves, float persistence, float lacunarity, int seed)
    {
        var heights = new float[width * height];
        var random = new Random(seed);

        var offsets = new Vector2[octaves];
        for (var i = 0; i < octaves; i++)
        {
            offsets[i] = new Vector2(random.NextSingle() * 1000.0f, random.NextSingle() * 1000.0f);
        }

        var minH = float.MaxValue;
        var maxH = float.MinValue;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var amplitude = 1.0f;
                var frequency = 1.0f;
                var noiseHeight = 0.0f;

                for (var o = 0; o < octaves; o++)
                {
                    var sampleX = x / (float)width * scale * frequency + offsets[o].X;
                    var sampleY = y / (float)height * scale * frequency + offsets[o].Y;

                    var noise = PerlinNoise2D(sampleX, sampleY);
                    noiseHeight += noise * amplitude;

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                heights[y * width + x] = noiseHeight;

                if (noiseHeight < minH)
                {
                    minH = noiseHeight;
                }

                if (noiseHeight > maxH)
                {
                    maxH = noiseHeight;
                }
            }
        }

        var range = maxH - minH;
        if (range > 0.0f)
        {
            for (var i = 0; i < heights.Length; i++)
            {
                heights[i] = (heights[i] - minH) / range;
            }
        }

        return new HeightMap(width, height, heights);
    }

    public void RecalculateBounds()
    {
        MinHeight = float.MaxValue;
        MaxHeight = float.MinValue;

        foreach (var h in _heights)
        {
            if (h < MinHeight)
            {
                MinHeight = h;
            }

            if (h > MaxHeight)
            {
                MaxHeight = h;
            }
        }
    }

    #endregion

    #region 私有方法

    private static float PerlinNoise2D(float x, float y)
    {
        var xi = (int)Math.Floor(x);
        var yi = (int)Math.Floor(y);
        var xf = x - xi;
        var yf = y - yi;

        var u = Fade(xf);
        var v = Fade(yf);

        var n00 = Hash2D(xi, yi);
        var n10 = Hash2D(xi + 1, yi);
        var n01 = Hash2D(xi, yi + 1);
        var n11 = Hash2D(xi + 1, yi + 1);

        var x0 = n00 * (1.0f - u) + n10 * u;
        var x1 = n01 * (1.0f - u) + n11 * u;

        return x0 * (1.0f - v) + x1 * v;
    }

    private static float Fade(float t)
    {
        return t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
    }

    private static float Hash2D(int x, int y)
    {
        var n = x * 374761393 + y * 668265263;
        n = (n ^ (n >> 13)) * 1274126177;
        return ((n ^ (n >> 16)) & 0xFFFF) / 65535.0f * 2.0f - 1.0f;
    }

    #endregion
}
