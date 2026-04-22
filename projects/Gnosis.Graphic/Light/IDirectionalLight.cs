namespace Gnosis.Graphic.Light;

public interface IDirectionalLight : ILight
{
    float[] Direction { get; set; }
    int CascadeCount { get; set; }
    float[] CascadeSplits { get; set; }
    float CascadeBlend { get; set; }
}
