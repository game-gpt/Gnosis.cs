namespace Gnosis.Rendering.Backends.Software;

public sealed class SoftwareRenderer
{
    #region Fields

    private readonly int _width;
    private readonly int _height;
    private readonly Texture _colorBuffer;
    private readonly float[] _depthBuffer;

    private IShaderModule? _currentModule;
    private DelegateMicroFunction? _vertexFunction;
    private DelegateMicroFunction? _fragmentFunction;
    private float[]? _vertexBuffer;
    private uint[]? _indexBuffer;
    private int _vertexStride;

    #endregion

    #region Constructors

    public SoftwareRenderer(int width, int height)
    {
        _width = width;
        _height = height;
        _colorBuffer = new Texture(width, height);
        _depthBuffer = new float[width * height];
    }

    #endregion

    #region Public Methods

    public void SetShaderModule(IShaderModule module)
    {
        _currentModule = module;
        _vertexFunction = null;
        _fragmentFunction = null;

        foreach (var func in module.Functions)
        {
            if (func is DelegateMicroFunction delegateFunc)
            {
                if (func.Kind == MicroFunctionKind.Vertex)
                {
                    _vertexFunction = delegateFunc;
                }
                else if (func.Kind == MicroFunctionKind.Fragment)
                {
                    _fragmentFunction = delegateFunc;
                }
            }
        }
    }

    public void SetVertexBuffer(float[] vertices, int stride)
    {
        _vertexBuffer = vertices;
        _vertexStride = stride;
    }

    public void SetIndexBuffer(uint[] indices)
    {
        _indexBuffer = indices;
    }

    public void Clear(float r, float g, float b, float a = 1.0f, float depth = 1.0f)
    {
        byte br = (byte)(r * 255);
        byte bg = (byte)(g * 255);
        byte bb = (byte)(b * 255);
        byte ba = (byte)(a * 255);

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                _colorBuffer.SetPixel(x, y, br, bg, bb, ba);
                _depthBuffer[y * _width + x] = depth;
            }
        }
    }

    public void DrawTriangles(int vertexCount, int firstVertex = 0)
    {
        if (_vertexBuffer == null || _vertexFunction == null)
        {
            return;
        }

        int triangleCount = (vertexCount - firstVertex) / 3;

        for (int t = 0; t < triangleCount; t++)
        {
            int i0 = firstVertex + t * 3;
            int i1 = i0 + 1;
            int i2 = i0 + 2;

            var v0 = ExecuteVertexShader(i0);
            var v1 = ExecuteVertexShader(i1);
            var v2 = ExecuteVertexShader(i2);

            RasterizeTriangle(v0, v1, v2);
        }
    }

    public void DrawIndexedTriangles(int indexCount, int firstIndex = 0)
    {
        if (_vertexBuffer == null || _indexBuffer == null || _vertexFunction == null)
        {
            return;
        }

        int triangleCount = (indexCount - firstIndex) / 3;

        for (int t = 0; t < triangleCount; t++)
        {
            int idx0 = firstIndex + t * 3;
            uint i0 = _indexBuffer[idx0];
            uint i1 = _indexBuffer[idx0 + 1];
            uint i2 = _indexBuffer[idx0 + 2];

            var v0 = ExecuteVertexShader((int)i0);
            var v1 = ExecuteVertexShader((int)i1);
            var v2 = ExecuteVertexShader((int)i2);

            RasterizeTriangle(v0, v1, v2);
        }
    }

    public Texture GetColorBuffer() => _colorBuffer;
    public byte[] GetFramebufferData() => _colorBuffer.Data;
    public int Width => _width;
    public int Height => _height;

    #endregion

    #region Private Methods

    private float[] ExecuteVertexShader(int vertexIndex)
    {
        int offset = vertexIndex * _vertexStride;
        var input = new float[_vertexStride];
        Array.Copy(_vertexBuffer!, input, _vertexStride);

        if (_vertexFunction?.Execute != null)
        {
            return _vertexFunction.Execute(input, vertexIndex);
        }

        return input;
    }

    private void RasterizeTriangle(float[] v0, float[] v1, float[] v2)
    {
        float x0 = v0[0], y0 = v0[1], z0 = v0[2];
        float x1 = v1[0], y1 = v1[1], z1 = v1[2];
        float x2 = v2[0], y2 = v2[1], z2 = v2[2];

        int minX = Math.Max(0, (int)Math.Floor(Math.Min(Math.Min(x0, x1), x2)));
        int maxX = Math.Min(_width - 1, (int)Math.Ceiling(Math.Max(Math.Max(x0, x1), x2)));
        int minY = Math.Max(0, (int)Math.Floor(Math.Min(Math.Min(y0, y1), y2)));
        int maxY = Math.Min(_height - 1, (int)Math.Ceiling(Math.Max(Math.Max(y0, y1), y2)));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                var (w0, w1, w2) = Barycentric(px, py, x0, y0, x1, y1, x2, y2);

                if (w0 >= 0 && w1 >= 0 && w2 >= 0)
                {
                    float z = w0 * z0 + w1 * z1 + w2 * z2;
                    int depthIdx = y * _width + x;

                    if (z < _depthBuffer[depthIdx])
                    {
                        _depthBuffer[depthIdx] = z;

                        var interpolated = InterpolateAttributes(w0, w1, w2, v0, v1, v2);
                        var color = ExecuteFragmentShader(interpolated);

                        _colorBuffer.SetPixel(x, y,
                            (byte)(color[0] * 255),
                            (byte)(color[1] * 255),
                            (byte)(color[2] * 255),
                            (byte)(color.Length > 3 ? color[3] * 255 : 255)
                        );
                    }
                }
            }
        }
    }

    private float[] InterpolateAttributes(float w0, float w1, float w2, float[] v0, float[] v1, float[] v2)
    {
        int len = Math.Min(Math.Min(v0.Length, v1.Length), v2.Length);
        var result = new float[len];

        for (int i = 0; i < len; i++)
        {
            result[i] = w0 * v0[i] + w1 * v1[i] + w2 * v2[i];
        }

        return result;
    }

    private float[] ExecuteFragmentShader(float[] interpolated)
    {
        if (_fragmentFunction?.Execute != null)
        {
            return _fragmentFunction.Execute(interpolated, 0);
        }

        return new float[] { 1, 1, 1, 1 };
    }

    private static (float, float, float) Barycentric(
        float px, float py,
        float x0, float y0,
        float x1, float y1,
        float x2, float y2)
    {
        float denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2);
        if (Math.Abs(denom) < 0.0001f)
        {
            return (0, 0, 0);
        }

        float bw0 = ((y1 - y2) * (px - x2) + (x2 - x1) * (py - y2)) / denom;
        float bw1 = ((y2 - y0) * (px - x2) + (x0 - x2) * (py - y2)) / denom;
        float bw2 = 1 - bw0 - bw1;

        return (bw0, bw1, bw2);
    }

    #endregion
}
