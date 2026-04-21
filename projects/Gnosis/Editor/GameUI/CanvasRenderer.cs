namespace Gnosis.Editor.GameUI;

public sealed class CanvasRenderer : ICanvasRenderer
{
    #region Fields

    private const int RingBufferSize = 65536;

    private readonly UIVertex[] _vertexRingBuffer = new UIVertex[RingBufferSize];
    private readonly uint[] _indexRingBuffer = new uint[RingBufferSize];
    private int _vertexHead;
    private int _indexHead;

    #endregion

    #region Properties

    public int VertexCount => _vertexHead;

    public int IndexCount => _indexHead;

    #endregion

    #region Public Methods

    public void BeginFrame()
    {
        _vertexHead = 0;
        _indexHead = 0;
    }

    public void EndFrame()
    {
    }

    public void SubmitCanvas(ICanvas canvas)
    {
        foreach (var element in canvas.Elements)
        {
            if (!element.Visible)
            {
                continue;
            }

            EmitQuad(element);
        }
    }

    public void WriteVertex(UIVertex vertex)
    {
        if (_vertexHead >= RingBufferSize)
        {
            return;
        }

        _vertexRingBuffer[_vertexHead++] = vertex;
    }

    public void WriteIndex(uint index)
    {
        if (_indexHead >= RingBufferSize)
        {
            return;
        }

        _indexRingBuffer[_indexHead++] = index;
    }

    #endregion

    #region Private Methods

    private void EmitQuad(IUIElement element)
    {
        uint baseIndex = (uint)_vertexHead;

        float x = element.X;
        float y = element.Y;
        float w = element.Width;
        float h = element.Height;
        float r = element.R;
        float g = element.G;
        float b = element.B;
        float a = element.A;
        float u0 = element.AtlasUV.U;
        float v0 = element.AtlasUV.V;
        float u1 = element.AtlasUV.U + element.AtlasUV.Width;
        float v1 = element.AtlasUV.V + element.AtlasUV.Height;

        WriteVertex(new UIVertex(x, y, 0, u0, v0, r, g, b, a));
        WriteVertex(new UIVertex(x + w, y, 0, u1, v0, r, g, b, a));
        WriteVertex(new UIVertex(x + w, y + h, 0, u1, v1, r, g, b, a));
        WriteVertex(new UIVertex(x, y + h, 0, u0, v1, r, g, b, a));

        WriteIndex(baseIndex);
        WriteIndex(baseIndex + 1);
        WriteIndex(baseIndex + 2);
        WriteIndex(baseIndex);
        WriteIndex(baseIndex + 2);
        WriteIndex(baseIndex + 3);
    }

    #endregion
}
