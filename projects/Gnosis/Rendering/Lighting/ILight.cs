namespace Gnosis.Rendering.Lighting;

public interface ILight
{
    string Name { get; }
    LightType Type { get; }
    float[] Color { get; set; }
    float Intensity { get; set; }
    bool IsEnabled { get; set; }
    bool CastShadows { get; set; }
    float ShadowStrength { get; set; }
    float ShadowBias { get; set; }
    float ShadowNormalBias { get; set; }
    float ShadowNearPlane { get; set; }
    int ShadowResolution { get; set; }
}
