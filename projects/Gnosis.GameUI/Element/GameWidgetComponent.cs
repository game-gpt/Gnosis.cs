using Gnosis.GameUI.Canvas;

namespace Gnosis.GameUI.Element;

public sealed class GameWidgetComponent : IWidgetComponent
{
    #region Properties

    public ICanvas Canvas { get; set; } = new Canvas.Canvas();

    public float ResolutionWidth { get; set; } = 512;

    public float ResolutionHeight { get; set; } = 256;

    public bool ReceiveLighting { get; set; } = true;

    public bool Visible { get; set; } = true;

    #endregion
}
