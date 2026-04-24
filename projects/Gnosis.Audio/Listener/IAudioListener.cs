using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Audio.Listener;

public interface IAudioListener
{
    float Volume { get; set; }
    Vector3 Position { get; set; }
    Vector3 Forward { get; set; }
    Vector3 Up { get; set; }
}
