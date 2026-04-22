using Gnosis.Audio.Clip;

namespace Gnosis.Audio.Source;

public sealed class AudioSource : IAudioSource
{
    #region 字段

    private float _volume = 1.0f;
    private float _pitch = 1.0f;
    private float _pan;
    private float _priority;
    private float _time;
    private float _timeSamples;
    private float _fadeInRemaining;
    private float _fadeOutRemaining;
    private float _fadeOutStartVolume;

    #endregion

    #region 属性

    public IAudioClip? Clip { get; set; }

    public bool IsPlaying { get; private set; }

    public bool IsLooping { get; set; }

    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    public float Pitch
    {
        get => _pitch;
        set => _pitch = Math.Clamp(value, -3f, 3f);
    }

    public float Pan
    {
        get => _pan;
        set => _pan = Math.Clamp(value, -1f, 1f);
    }

    public float Priority
    {
        get => _priority;
        set => _priority = Math.Clamp(value, 0f, 256f);
    }

    public bool Mute { get; set; }

    public float Time
    {
        get => _time;
        set => _time = Math.Max(0f, value);
    }

    public float TimeSamples
    {
        get => _timeSamples;
        set => _timeSamples = Math.Max(0f, value);
    }

    public string? BusName { get; set; }

    public bool Spatialize { get; set; }

    public float MinDistance { get; set; } = 1.0f;

    public float MaxDistance { get; set; } = 500.0f;

    public AudioRolloffMode RolloffMode { get; set; } = AudioRolloffMode.Logarithmic;

    public float SpatialBlend { get; set; }

    public float DopplerLevel { get; set; } = 1.0f;

    public float Spread { get; set; }

    #endregion

    #region IAudioSource 实现

    public void Play()
    {
        if (Clip is null)
        {
            return;
        }

        IsPlaying = true;
        _fadeInRemaining = 0f;
        _fadeOutRemaining = 0f;
    }

    public void PlayDelayed(float delay)
    {
        if (Clip is null)
        {
            return;
        }

        IsPlaying = true;
    }

    public void PlayScheduled(double time)
    {
        if (Clip is null)
        {
            return;
        }

        IsPlaying = true;
    }

    public void Stop()
    {
        IsPlaying = false;
        _time = 0f;
        _timeSamples = 0f;
        _fadeInRemaining = 0f;
        _fadeOutRemaining = 0f;
    }

    public void Pause()
    {
        IsPlaying = false;
    }

    public void UnPause()
    {
        if (Clip is null)
        {
            return;
        }

        IsPlaying = true;
    }

    public void SetCustomCurve(string curveName, float[] keys)
    {
    }

    public void FadeIn(float duration)
    {
        _fadeInRemaining = duration;
        _volume = 0f;
    }

    public void FadeOut(float duration)
    {
        _fadeOutRemaining = duration;
        _fadeOutStartVolume = _volume;
    }

    #endregion

    #region 内部方法

    internal void Update(float delta)
    {
        if (!IsPlaying)
        {
            return;
        }

        if (Clip is null)
        {
            IsPlaying = false;
            return;
        }

        if (_fadeInRemaining > 0f)
        {
            _fadeInRemaining -= delta;

            if (_fadeInRemaining <= 0f)
            {
                _fadeInRemaining = 0f;
                _volume = 1.0f;
            }
        }

        if (_fadeOutRemaining > 0f)
        {
            _fadeOutRemaining -= delta;

            if (_fadeOutRemaining <= 0f)
            {
                _fadeOutRemaining = 0f;
                _volume = 0f;
                IsPlaying = false;
                return;
            }

            _volume = _fadeOutStartVolume * (_fadeOutRemaining / (_fadeOutRemaining + delta));
        }

        _time += delta * _pitch;

        if (_time >= Clip.Duration)
        {
            if (IsLooping)
            {
                _time -= Clip.Duration;
            }
            else
            {
                IsPlaying = false;
                _time = 0f;
            }
        }
    }

    #endregion
}
