using Gnosis.Audio.Listener;
using Gnosis.ECS.Component;

namespace Gnosis.Audio;

public struct AudioListenerComponent : IComponent
{
    public IAudioListener? Listener { get; set; }
}
