using Gnosis.ECS.Core;

namespace Gnosis.Graphic.FX;

public struct ParticleSystemComponent : IComponent
{
    public IParticleSystem? ParticleSystem { get; set; }
    public bool PlayOnAwake { get; set; }
    public float PlaybackSpeed { get; set; }
}
