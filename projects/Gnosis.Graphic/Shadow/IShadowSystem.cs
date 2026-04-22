using Gnosis.Graphic.Light;

namespace Gnosis.Graphic.Shadow;

public interface IShadowSystem
{
    IShadowSettings Settings { get; set; }
    void RenderShadowMaps();
    void UpdateCascades(IDirectionalLight light);
}
