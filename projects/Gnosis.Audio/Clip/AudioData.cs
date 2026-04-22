namespace Gnosis.Audio.Clip;

public struct AudioData
{
    public float[] Samples { get; init; }
    public int Channels { get; init; }
    public int SampleRate { get; init; }
    public int SampleCount { get; init; }
}
