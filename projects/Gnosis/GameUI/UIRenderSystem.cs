using Gnosis.GameUI;

namespace Gnosis.GameUI;

public sealed class UIRenderSystem : IUIRenderSystem
{
    #region Fields

    private ICanvasRenderer? _renderer;
    private readonly List<ICanvas> _canvases = new();

    #endregion

    #region Properties

    public IReadOnlyList<ICanvas> ActiveCanvases => _canvases;

    #endregion

    #region Public Methods

    public void Initialize(ICanvasRenderer renderer)
    {
        _renderer = renderer;
    }

    public void RenderFrame(float deltaTime)
    {
        if (_renderer == null)
        {
            return;
        }

        _renderer.BeginFrame();

        var sorted = _canvases.OrderBy(c => c.SortingOrder).ToList();

        foreach (var canvas in sorted)
        {
            _renderer.SubmitCanvas(canvas);
        }

        _renderer.EndFrame();
    }

    public void RegisterCanvas(ICanvas canvas)
    {
        if (!_canvases.Contains(canvas))
        {
            _canvases.Add(canvas);
        }
    }

    public void UnregisterCanvas(ICanvas canvas)
    {
        _canvases.Remove(canvas);
    }

    #endregion
}
