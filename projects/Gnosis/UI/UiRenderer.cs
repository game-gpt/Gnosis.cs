using GnosisEngine.Renderer;
using GnosisEngine.Shader;
using GnosisEngine.Widget;

namespace GnosisEngine.UI;

public sealed class UiRenderer : IWidgetRenderer
{
    #region Fields

    private readonly SoftwareRenderer _renderer;
    private readonly IShaderModule _shaderModule;
    private readonly List<float> _vertices = new();
    private readonly List<uint> _indices = new();

    // 裁剪矩形栈
    private readonly Stack<(float x, float y, float w, float h)> _clipStack = new();

    #endregion

    #region Constructors

    public UiRenderer(int width, int height)
    {
        _renderer = new SoftwareRenderer(width, height);
        _shaderModule = BuiltinShaderModules.CreateUiShader();
    }

    #endregion

    #region Properties

    public int Width => _renderer.Width;
    public int Height => _renderer.Height;

    #endregion

    #region Public Methods

    public void Begin()
    {
        _vertices.Clear();
        _indices.Clear();
    }

    public void End()
    {
        if (_vertices.Count == 0)
        {
            return;
        }

        _renderer.SetShaderModule(_shaderModule);
        _renderer.SetVertexBuffer(_vertices.ToArray(), 7);
        _renderer.SetIndexBuffer(_indices.ToArray());
        _renderer.DrawIndexedTriangles(_indices.Count);
    }

    public void Clear(float r, float g, float b, float a = 1.0f)
    {
        _renderer.Clear(r, g, b, a);
    }

    public void DrawRect(float x, float y, float width, float height, float r, float g, float b, float a = 1.0f)
    {
        // 如果存在活动裁剪区域，将绘制区域钳制到裁剪范围内
        if (_clipStack.TryPeek(out var clip))
        {
            float clampedX = Math.Max(x, clip.x);
            float clampedY = Math.Max(y, clip.y);
            float clampedRight = Math.Min(x + width, clip.x + clip.w);
            float clampedBottom = Math.Min(y + height, clip.y + clip.h);
            float clampedWidth = Math.Max(0, clampedRight - clampedX);
            float clampedHeight = Math.Max(0, clampedBottom - clampedY);

            if (clampedWidth <= 0 || clampedHeight <= 0)
            {
                return;
            }

            x = clampedX;
            y = clampedY;
            width = clampedWidth;
            height = clampedHeight;
        }

        uint baseIndex = (uint)(_vertices.Count / 7);

        _vertices.Add(x);
        _vertices.Add(y);
        _vertices.Add(0);
        _vertices.Add(r);
        _vertices.Add(g);
        _vertices.Add(b);
        _vertices.Add(a);

        _vertices.Add(x + width);
        _vertices.Add(y);
        _vertices.Add(0);
        _vertices.Add(r);
        _vertices.Add(g);
        _vertices.Add(b);
        _vertices.Add(a);

        _vertices.Add(x + width);
        _vertices.Add(y + height);
        _vertices.Add(0);
        _vertices.Add(r);
        _vertices.Add(g);
        _vertices.Add(b);
        _vertices.Add(a);

        _vertices.Add(x);
        _vertices.Add(y + height);
        _vertices.Add(0);
        _vertices.Add(r);
        _vertices.Add(g);
        _vertices.Add(b);
        _vertices.Add(a);

        _indices.Add(baseIndex);
        _indices.Add(baseIndex + 1);
        _indices.Add(baseIndex + 2);
        _indices.Add(baseIndex);
        _indices.Add(baseIndex + 2);
        _indices.Add(baseIndex + 3);
    }

    public void DrawText(string text, float x, float y, float size, float r, float g, float b)
    {
        float cursorX = x;
        foreach (char c in text)
        {
            DrawChar(c, cursorX, y, size, r, g, b);
            cursorX += size * 0.6f;
        }
    }

    public byte[] GetFramebufferData() => _renderer.GetFramebufferData();

    // 压入裁剪矩形
    public void PushClip(float x, float y, float w, float h)
    {
        if (_clipStack.TryPeek(out var current))
        {
            // 与当前裁剪区域求交集
            float intersectX = Math.Max(x, current.x);
            float intersectY = Math.Max(y, current.y);
            float intersectRight = Math.Min(x + w, current.x + current.w);
            float intersectBottom = Math.Min(y + h, current.y + current.h);
            float intersectW = Math.Max(0, intersectRight - intersectX);
            float intersectH = Math.Max(0, intersectBottom - intersectY);

            _clipStack.Push((intersectX, intersectY, intersectW, intersectH));
        }
        else
        {
            _clipStack.Push((x, y, w, h));
        }
    }

    // 弹出裁剪矩形
    public void PopClip()
    {
        if (_clipStack.Count > 0)
        {
            _clipStack.Pop();
        }
    }

    #endregion

    #region Private Methods

    private void DrawChar(char c, float x, float y, float size, float r, float g, float b)
    {
        DrawRect(x, y, size, size, r, g, b);
    }

    #endregion
}
