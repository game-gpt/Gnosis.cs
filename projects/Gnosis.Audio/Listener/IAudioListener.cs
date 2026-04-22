namespace Gnosis.Audio.Listener;

public interface IAudioListener
{
    float Volume { get; set; }
    float[] Position { get; set; }
    float[] Forward { get; set; }
    float[] Up { get; set; }
}
