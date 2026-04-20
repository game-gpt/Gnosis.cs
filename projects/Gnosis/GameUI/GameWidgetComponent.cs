using Gnosis.GameUI;

namespace Gnosis.GameUI;

public sealed class GameWidgetComponent : IWidgetComponent
{
    #region Properties

    public ICanvas Canvas { get; set; } = new Canvas();

    public float ResolutionWidth { get; set; } = 512;

    public float ResolutionHeight { get; set; } = 256;

    public bool ReceiveLighting { get; set; } = true;

    public bool Visible { get; set; } = true;

    #endregion
}
