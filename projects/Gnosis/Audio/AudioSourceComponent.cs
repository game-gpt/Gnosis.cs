using Gnosis.Core;

namespace Gnosis.Audio;

public struct AudioSourceComponent : IComponent
{
    public IAudioSource? Source { get; set; }
    public string? ClipPath { get; set; }
    public bool PlayOnAwake { get; set; }
    public bool Loop { get; set; }
    public float Volume { get; set; }
}
