using System.Numerics;

namespace Gnosis.Audio.Listener;

public sealed class AudioListener : IAudioListener
{
    #region 属性

    public float Volume { get; set; } = 1.0f;

    public Vector3 Position { get; set; } = Vector3.Zero;

    public Vector3 Forward { get; set; } = -Vector3.UnitZ;

    public Vector3 Up { get; set; } = Vector3.UnitY;

    #endregion
}
