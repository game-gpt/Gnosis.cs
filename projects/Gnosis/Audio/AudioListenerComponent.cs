using Gnosis.Core;

namespace Gnosis.Audio;

public struct AudioListenerComponent : IComponent
{
    public IAudioListener? Listener { get; set; }
}
