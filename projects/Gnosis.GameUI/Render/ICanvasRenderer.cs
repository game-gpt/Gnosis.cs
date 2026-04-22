using Gnosis.GameUI.Canvas;

namespace Gnosis.GameUI.Render;

public interface ICanvasRenderer
{
    void BeginFrame();
    void EndFrame();
    void SubmitCanvas(ICanvas canvas);
    void WriteVertex(UIVertex vertex);
    void WriteIndex(uint index);
    int VertexCount { get; }
    int IndexCount { get; }
}
