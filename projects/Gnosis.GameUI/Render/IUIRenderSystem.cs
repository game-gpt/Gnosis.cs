using Gnosis.GameUI.Canvas;

namespace Gnosis.GameUI.Render;

public interface IUIRenderSystem
{
    void Initialize(ICanvasRenderer renderer);
    void RenderFrame(float deltaTime);
    void RegisterCanvas(ICanvas canvas);
    void UnregisterCanvas(ICanvas canvas);
    IReadOnlyList<ICanvas> ActiveCanvases { get; }
}
