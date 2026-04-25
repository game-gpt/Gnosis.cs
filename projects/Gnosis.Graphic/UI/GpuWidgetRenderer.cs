using System.Numerics;
using System.Runtime.InteropServices;

namespace Gnosis.Graphic.UI;

[StructLayout(LayoutKind.Sequential)]
public struct UIVertex
{
    public Vector3 Position;
    public Vector4 Color;
    public Vector2 Uv;
    public Vector2 RectSize;
    public Vector4 CornerRadii;
    public float DrawType;
    public float BorderWidth;
    public Vector4 BorderColor;

    public UIVertex(
        Vector3 position, Vector4 color, Vector2 uv,
        Vector2 rectSize, Vector4 cornerRadii,
        float drawType, float borderWidth, Vector4 borderColor)
    {
        Position = position;
        Color = color;
        Uv = uv;
        RectSize = rectSize;
        CornerRadii = cornerRadii;
        DrawType = drawType;
        BorderWidth = borderWidth;
        BorderColor = borderColor;
    }
}

public enum UIDrawType
{
    SdfText = 0,
    RoundedRect = 1,
    RoundedRectBorder = 2,
    Line = 3
}

public sealed class GpuWidgetRenderer : IDisposable
{
    #region 常量

    private const int InitialCapacity = 1024;
    private const int VerticesPerQuad = 4;
    private const int IndicesPerQuad = 6;

    #endregion

    #region 内部状态

    private readonly int _width;
    private readonly int _height;
    private readonly List<UIVertex> _vertices;
    private readonly List<uint> _indices;
    private readonly Stack<ClipRect> _clipStack;
    private bool _inDraw;

    #endregion

    #region 属性

    public int Width => _width;
    public int Height => _height;
    public int QuadCount => _vertices.Count / VerticesPerQuad;
    public IReadOnlyList<UIVertex> Vertices => _vertices;
    public IReadOnlyList<uint> Indices => _indices;

    #endregion

    #region 构造函数

    public GpuWidgetRenderer(int width, int height)
    {
        _width = width;
        _height = height;
        _vertices = new List<UIVertex>(InitialCapacity * VerticesPerQuad);
        _indices = new List<uint>(InitialCapacity * IndicesPerQuad);
        _clipStack = new Stack<ClipRect>();
        _inDraw = false;
    }

    #endregion

    #region 绘制方法

    public void Begin()
    {
        _vertices.Clear();
        _indices.Clear();
        _clipStack.Clear();
        _inDraw = true;
    }

    public void End()
    {
        _inDraw = false;
    }

    public void Clear()
    {
        _vertices.Clear();
        _indices.Clear();
    }

    public void DrawRect(float x, float y, float width, float height, float r, float g, float b, float a = 1.0f)
    {
        EmitRoundedRectQuad(
            x, y, width, height,
            new Vector4(r, g, b, a),
            Vector4.Zero,
            0.0f,
            new Vector4(0, 0, 0, 0));
    }

    public void DrawRoundedRect(float x, float y, float width, float height,
        float radius, float r, float g, float b, float a = 1.0f)
    {
        EmitRoundedRectQuad(
            x, y, width, height,
            new Vector4(r, g, b, a),
            new Vector4(radius, radius, radius, radius),
            0.0f,
            new Vector4(0, 0, 0, 0));
    }

    public void DrawRoundedRectBorder(float x, float y, float width, float height,
        float radius, float borderWidth,
        float fillR, float fillG, float fillB, float fillA,
        float borderR, float borderG, float borderB, float borderA)
    {
        EmitRoundedRectQuad(
            x, y, width, height,
            new Vector4(fillR, fillG, fillB, fillA),
            new Vector4(radius, radius, radius, radius),
            borderWidth,
            new Vector4(borderR, borderG, borderB, borderA));
    }

    public void DrawText(string text, float x, float y, float size, float r, float g, float b)
    {
        EmitSdfTextQuad(x, y, size, size, new Vector4(r, g, b, 1.0f));
    }

    public void DrawSdfGlyph(float x, float y, float width, float height,
        Vector2 uvOffset, Vector2 uvSize,
        float r, float g, float b, float a)
    {
        EmitSdfGlyphQuad(x, y, width, height, uvOffset, uvSize, new Vector4(r, g, b, a));
    }

    public void DrawLine(float x1, float y1, float x2, float y2, float r, float g, float b, float a = 1.0f, float thickness = 1.0f)
    {
        var isHorizontal = MathF.Abs(y2 - y1) < 0.001f;
        var isVertical = MathF.Abs(x2 - x1) < 0.001f;

        if (isHorizontal)
        {
            var lx = Math.Min(x1, x2);
            var ly = y1 - thickness * 0.5f;
            var lw = Math.Abs(x2 - x1);
            var lh = thickness;
            EmitLineQuad(lx, ly, lw, lh, new Vector4(r, g, b, a));
        }
        else if (isVertical)
        {
            var lx = x1 - thickness * 0.5f;
            var ly = Math.Min(y1, y2);
            var lw = thickness;
            var lh = Math.Abs(y2 - y1);
            EmitLineQuad(lx, ly, lw, lh, new Vector4(r, g, b, a));
        }
        else
        {
            var dx = x2 - x1;
            var dy = y2 - y1;
            var len = MathF.Sqrt(dx * dx + dy * dy);
            var nx = -dy / len * thickness * 0.5f;
            var ny = dx / len * thickness * 0.5f;

            var v0 = new Vector3(NdcX(x1 + nx), NdcY(y1 + ny), 0.0f);
            var v1 = new Vector3(NdcX(x1 - nx), NdcY(y1 - ny), 0.0f);
            var v2 = new Vector3(NdcX(x2 - nx), NdcY(y2 - ny), 0.0f);
            var v3 = new Vector3(NdcX(x2 + nx), NdcY(y2 + ny), 0.0f);

            var color = new Vector4(r, g, b, a);
            var rectSize = new Vector2(len, thickness);
            var vertexBase = (uint)_vertices.Count;

            _vertices.Add(new UIVertex(v0, color, new Vector2(0, 0), rectSize, Vector4.Zero, (float)UIDrawType.Line, 0, Vector4.Zero));
            _vertices.Add(new UIVertex(v1, color, new Vector2(0, 1), rectSize, Vector4.Zero, (float)UIDrawType.Line, 0, Vector4.Zero));
            _vertices.Add(new UIVertex(v2, color, new Vector2(1, 1), rectSize, Vector4.Zero, (float)UIDrawType.Line, 0, Vector4.Zero));
            _vertices.Add(new UIVertex(v3, color, new Vector2(1, 0), rectSize, Vector4.Zero, (float)UIDrawType.Line, 0, Vector4.Zero));

            EmitQuadIndices(vertexBase);
        }
    }

    public void PushClip(float x, float y, float width, float height)
    {
        _clipStack.Push(new ClipRect(x, y, width, height));
    }

    public void PopClip()
    {
        if (_clipStack.Count > 0)
        {
            _clipStack.Pop();
        }
    }

    #endregion

    #region GPU 数据导出

    public (UIVertex[] vertices, uint[] indices) GetGpuData()
    {
        return (_vertices.ToArray(), _indices.ToArray());
    }

    public int GetVertexDataSize()
    {
        return _vertices.Count * Marshal.SizeOf<UIVertex>();
    }

    public int GetIndexDataSize()
    {
        return _indices.Count * sizeof(uint);
    }

    #endregion

    #region 私有方法

    private void EmitRoundedRectQuad(
        float x, float y, float width, float height,
        Vector4 color, Vector4 cornerRadii,
        float borderWidth, Vector4 borderColor)
    {
        var drawType = borderWidth > 0 ? UIDrawType.RoundedRectBorder : UIDrawType.RoundedRect;

        var ndcX = NdcX(x);
        var ndcY = NdcY(y);
        var ndcW = NdcW(width);
        var ndcH = NdcH(height);

        var v0 = new Vector3(ndcX, ndcY, 0.0f);
        var v1 = new Vector3(ndcX, ndcY + ndcH, 0.0f);
        var v2 = new Vector3(ndcX + ndcW, ndcY + ndcH, 0.0f);
        var v3 = new Vector3(ndcX + ndcW, ndcY, 0.0f);

        var rectSize = new Vector2(width, height);
        var vertexBase = (uint)_vertices.Count;

        _vertices.Add(new UIVertex(v0, color, new Vector2(0, 0), rectSize, cornerRadii, (float)drawType, borderWidth, borderColor));
        _vertices.Add(new UIVertex(v1, color, new Vector2(0, 1), rectSize, cornerRadii, (float)drawType, borderWidth, borderColor));
        _vertices.Add(new UIVertex(v2, color, new Vector2(1, 1), rectSize, cornerRadii, (float)drawType, borderWidth, borderColor));
        _vertices.Add(new UIVertex(v3, color, new Vector2(1, 0), rectSize, cornerRadii, (float)drawType, borderWidth, borderColor));

        EmitQuadIndices(vertexBase);
    }

    private void EmitSdfTextQuad(float x, float y, float width, float height, Vector4 color)
    {
        var ndcX = NdcX(x);
        var ndcY = NdcY(y);
        var ndcW = NdcW(width);
        var ndcH = NdcH(height);

        var v0 = new Vector3(ndcX, ndcY, 0.0f);
        var v1 = new Vector3(ndcX, ndcY + ndcH, 0.0f);
        var v2 = new Vector3(ndcX + ndcW, ndcY + ndcH, 0.0f);
        var v3 = new Vector3(ndcX + ndcW, ndcY, 0.0f);

        var rectSize = new Vector2(width, height);
        var vertexBase = (uint)_vertices.Count;

        _vertices.Add(new UIVertex(v0, color, new Vector2(0, 0), rectSize, Vector4.Zero, (float)UIDrawType.SdfText, 0, Vector4.Zero));
        _vertices.Add(new UIVertex(v1, color, new Vector2(0, 1), rectSize, Vector4.Zero, (float)UIDrawType.SdfText, 0, Vector4.Zero));
        _vertices.Add(new UIVertex(v2, color, new Vector2(1, 1), rectSize, Vector4.Zero, (float)UIDrawType.SdfText, 0, Vector4.Zero));
        _vertices.Add(new UIVertex(v3, color, new Vector2(1, 0), rectSize, Vector4.Zero, (float)UIDrawType.SdfText, 0, Vector4.Zero));

        EmitQuadIndices(vertexBase);
    }

    private void EmitSdfGlyphQuad(float x, float y, float width, float height,
        Vector2 uvOffset, Vector2 uvSize, Vector4 color)
    {
        var ndcX = NdcX(x);
        var ndcY = NdcY(y);
        var ndcW = NdcW(width);
        var ndcH = NdcH(height);

        var v0 = new Vector3(ndcX, ndcY, 0.0f);
        var v1 = new Vector3(ndcX, ndcY + ndcH, 0.0f);
        var v2 = new Vector3(ndcX + ndcW, ndcY + ndcH, 0.0f);
        var v3 = new Vector3(ndcX + ndcW, ndcY, 0.0f);

        var rectSize = new Vector2(width, height);
        var vertexBase = (uint)_vertices.Count;

        _vertices.Add(new UIVertex(v0, color, new Vector2(uvOffset.X, uvOffset.Y), rectSize, Vector4.Zero, (float)UIDrawType.SdfText, 0, Vector4.Zero));
        _vertices.Add(new UIVertex(v1, color, new Vector2(uvOffset.X, uvOffset.Y + uvSize.Y), rectSize, Vector4.Zero, (float)UIDrawType.SdfText, 0, Vector4.Zero));
        _vertices.Add(new UIVertex(v2, color, new Vector2(uvOffset.X + uvSize.X, uvOffset.Y + uvSize.Y), rectSize, Vector4.Zero, (float)UIDrawType.SdfText, 0, Vector4.Zero));
        _vertices.Add(new UIVertex(v3, color, new Vector2(uvOffset.X + uvSize.X, uvOffset.Y), rectSize, Vector4.Zero, (float)UIDrawType.SdfText, 0, Vector4.Zero));

        EmitQuadIndices(vertexBase);
    }

    private void EmitLineQuad(float x, float y, float width, float height, Vector4 color)
    {
        var ndcX = NdcX(x);
        var ndcY = NdcY(y);
        var ndcW = NdcW(width);
        var ndcH = NdcH(height);

        var v0 = new Vector3(ndcX, ndcY, 0.0f);
        var v1 = new Vector3(ndcX, ndcY + ndcH, 0.0f);
        var v2 = new Vector3(ndcX + ndcW, ndcY + ndcH, 0.0f);
        var v3 = new Vector3(ndcX + ndcW, ndcY, 0.0f);

        var rectSize = new Vector2(width, height);
        var vertexBase = (uint)_vertices.Count;

        _vertices.Add(new UIVertex(v0, color, new Vector2(0, 0), rectSize, Vector4.Zero, (float)UIDrawType.Line, 0, Vector4.Zero));
        _vertices.Add(new UIVertex(v1, color, new Vector2(0, 1), rectSize, Vector4.Zero, (float)UIDrawType.Line, 0, Vector4.Zero));
        _vertices.Add(new UIVertex(v2, color, new Vector2(1, 1), rectSize, Vector4.Zero, (float)UIDrawType.Line, 0, Vector4.Zero));
        _vertices.Add(new UIVertex(v3, color, new Vector2(1, 0), rectSize, Vector4.Zero, (float)UIDrawType.Line, 0, Vector4.Zero));

        EmitQuadIndices(vertexBase);
    }

    private void EmitQuadIndices(uint vertexBase)
    {
        _indices.Add(vertexBase);
        _indices.Add(vertexBase + 1);
        _indices.Add(vertexBase + 2);
        _indices.Add(vertexBase);
        _indices.Add(vertexBase + 2);
        _indices.Add(vertexBase + 3);
    }

    private float NdcX(float pixelX)
    {
        return pixelX / _width * 2.0f - 1.0f;
    }

    private float NdcY(float pixelY)
    {
        return 1.0f - pixelY / _height * 2.0f;
    }

    private float NdcW(float pixelW)
    {
        return pixelW / _width * 2.0f;
    }

    private float NdcH(float pixelH)
    {
        return pixelH / _height * 2.0f;
    }

    #endregion

    #region 内部类型

    private readonly record struct ClipRect(float X, float Y, float Width, float Height);

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _vertices.Clear();
        _indices.Clear();
        _clipStack.Clear();
    }

    #endregion
}
