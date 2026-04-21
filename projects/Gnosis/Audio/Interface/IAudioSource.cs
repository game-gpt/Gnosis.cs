namespace Gnosis.Audio.Interface;

public interface IAudioSource
{
    IAudioClip? Clip { get; set; }
    bool IsPlaying { get; }
    bool IsLooping { get; set; }
    float Volume { get; set; }
    float Pitch { get; set; }
    float Pan { get; set; }
    float Priority { get; set; }
    bool Mute { get; set; }
    float Time { get; set; }
    float TimeSamples { get; set; }
    string? BusName { get; set; }
    bool Spatialize { get; set; }
    float MinDistance { get; set; }
    float MaxDistance { get; set; }
    AudioRolloffMode RolloffMode { get; set; }
    float SpatialBlend { get; set; }
    float DopplerLevel { get; set; }
    float Spread { get; set; }
    void Play();
    void PlayDelayed(float delay);
    void PlayScheduled(double time);
    void Stop();
    void Pause();
    void UnPause();
    void SetCustomCurve(string curveName, float[] keys);
    void FadeIn(float duration);
    void FadeOut(float duration);
}
