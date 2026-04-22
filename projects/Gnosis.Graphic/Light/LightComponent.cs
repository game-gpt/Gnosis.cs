using Gnosis.ECS.Core;

namespace Gnosis.Rendering.Lighting;

public struct LightComponent : IComponent
{
    public ILight? Light { get; set; }
    public LightType Type { get; set; }
    public float Intensity { get; set; }
    public bool CastShadows { get; set; }
}
