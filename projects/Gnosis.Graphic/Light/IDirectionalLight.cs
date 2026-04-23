using Gnosis.Core.Math;

namespace Gnosis.Graphic.Light;

public interface IDirectionalLight : ILight
{
    Vector3 Direction { get; set; }
    int CascadeCount { get; set; }
    float[] CascadeSplits { get; set; }
    float CascadeBlend { get; set; }
}
