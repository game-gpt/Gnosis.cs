using Gnosis.Audio.Interface;
using Gnosis.ECS.Core;

namespace Gnosis.Audio;

public struct AudioListenerComponent : IComponent
{
    public IAudioListener? Listener { get; set; }
}
