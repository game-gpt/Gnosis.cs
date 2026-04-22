using Gnosis.Audio.Clip;

namespace Gnosis.Audio.Source;

public class StubAudioSource : IAudioSource
{
    public IAudioClip? Clip { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public bool IsPlaying => throw new NotImplementedException("音频系统尚未实现");
    public bool IsLooping { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public float Volume { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public float Pitch { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public float Pan { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public float Priority { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public bool Mute { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public float Time { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public float TimeSamples { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public string? BusName { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public bool Spatialize { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public float MinDistance { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public float MaxDistance { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public AudioRolloffMode RolloffMode { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public float SpatialBlend { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public float DopplerLevel { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }
    public float Spread { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }

    public void FadeIn(float duration)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void FadeOut(float duration)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void Pause()
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void Play()
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void PlayDelayed(float delay)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void PlayScheduled(double time)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void SetCustomCurve(string curveName, float[] keys)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void Stop()
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void UnPause()
    {
        throw new NotImplementedException("音频系统尚未实现");
    }
}
