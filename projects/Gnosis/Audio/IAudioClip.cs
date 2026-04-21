using Gnosis.Assets.Formats;

namespace Gnosis.Audio;

public interface IAudioClip
{
    string Name { get; }
    float Duration { get; }
    int Channels { get; }
    int SampleRate { get; }
    bool IsLoaded { get; }
    AudioData? Data { get; }
    void Load();
    void Unload();
}
