using Gnosis.ECS.Component;

namespace Gnosis.Graphic.Light;

public struct LightComponent : IComponent
{
    public ILight? Light { get; set; }
    public LightType Type { get; set; }
    public float Intensity { get; set; }
    public bool CastShadows { get; set; }
}
