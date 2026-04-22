namespace Gnosis.Audio.Listener;

public sealed class AudioListener : IAudioListener
{
    #region 属性

    public float Volume { get; set; } = 1.0f;

    public float[] Position { get; set; } = [0f, 0f, 0f];

    public float[] Forward { get; set; } = [0f, 0f, -1f];

    public float[] Up { get; set; } = [0f, 1f, 0f];

    #endregion
}
