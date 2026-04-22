namespace Gnosis.Rendering.Lighting;

public interface IShadowSettings
{
    int Resolution { get; set; }
    float Distance { get; set; }
    int CascadeCount { get; set; }
    float[] CascadeSplits { get; set; }
    float Bias { get; set; }
    float NormalBias { get; set; }
    bool SoftShadows { get; set; }
    int SoftShadowQuality { get; set; }
}
