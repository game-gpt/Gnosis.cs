namespace Gnosis.Audio.Interface;

public interface IAudioEffect
{
    string Name { get; }
    AudioEffectType EffectType { get; }
    bool IsEnabled { get; set; }
    float GetParameter(string name);
    void SetParameter(string name, float value);
}

public enum AudioEffectType
{
    Reverb = 0,
    LowPass = 1,
    HighPass = 2,
    Echo = 3,
    Chorus = 4,
    Distortion = 5,
    Compressor = 6,
    Equalizer = 7,
    Custom = 8
}
