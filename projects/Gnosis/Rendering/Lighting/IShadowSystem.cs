namespace Gnosis.Rendering.Lighting;

public interface IShadowSystem
{
    IShadowSettings Settings { get; set; }
    void RenderShadowMaps();
    void UpdateCascades(IDirectionalLight light);
}
