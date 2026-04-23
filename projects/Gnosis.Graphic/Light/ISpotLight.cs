using Gnosis.Core.Math;

namespace Gnosis.Graphic.Light;

public interface ISpotLight : ILight
{
    Vector3 Position { get; set; }
    Vector3 Direction { get; set; }
    float Range { get; set; }
    float InnerConeAngle { get; set; }
    float OuterConeAngle { get; set; }
}
