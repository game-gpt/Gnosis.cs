using Gnosis.Audio.Clip;

namespace Gnosis.Audio.Synthesis;

public enum OscillatorType
{
    Sine = 0,
    Square = 1,
    Sawtooth = 2,
    Triangle = 3
}

public sealed class Oscillator
{
    #region 属性

    public OscillatorType Type { get; set; } = OscillatorType.Sine;

    public float Frequency { get; set; } = 440f;

    public float Amplitude { get; set; } = 1.0f;

    public float Phase { get; set; }

    #endregion

    #region 公开方法

    public float Sample(float time)
    {
        var t = Frequency * time + Phase;

        return Type switch
        {
            OscillatorType.Sine => Amplitude * MathF.Sin(2f * MathF.PI * t),
            OscillatorType.Square => Amplitude * (MathF.Sin(2f * MathF.PI * t) >= 0f ? 1f : -1f),
            OscillatorType.Sawtooth => Amplitude * (2f * (t % 1f) - 1f),
            OscillatorType.Triangle => Amplitude * (4f * MathF.Abs((t % 1f) - 0.5f) - 1f),
            _ => 0f
        };
    }

    public AudioData Generate(float duration, int sampleRate = 44100, int channels = 1)
    {
        var sampleCount = (int)(duration * sampleRate * channels);
        var samples = new float[sampleCount];

        for (var i = 0; i < sampleCount; i++)
        {
            var time = (float)i / (sampleRate * channels);
            samples[i] = Sample(time);
        }

        return new AudioData
        {
            Samples = samples,
            Channels = channels,
            SampleRate = sampleRate,
            SampleCount = sampleCount
        };
    }

    #endregion
}
