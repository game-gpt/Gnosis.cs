namespace Gnosis.Editor.GameUI;

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
