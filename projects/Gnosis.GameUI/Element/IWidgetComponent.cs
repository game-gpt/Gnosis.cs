using Gnosis.GameUI.Canvas;

namespace Gnosis.GameUI.Element;

public interface IWidgetComponent
{
    ICanvas Canvas { get; set; }
    float ResolutionWidth { get; set; }
    float ResolutionHeight { get; set; }
    bool ReceiveLighting { get; set; }
    bool Visible { get; set; }
}
