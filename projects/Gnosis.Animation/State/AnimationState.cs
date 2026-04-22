namespace Gnosis.Animation.State;

public sealed class AnimationState : IAnimationState
{
    #region 属性

    public string Name { get; }

    public string ClipName { get; }

    public float Speed { get; set; } = 1.0f;

    public bool IsLooping { get; set; } = true;

    public float Duration { get; set; }

    public float NormalizedTime { get; set; }

    public bool IsPlaying { get; private set; } = true;

    #endregion

    #region 构造函数

    public AnimationState(string name, string clipName, float duration = 1.0f)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ClipName = clipName ?? throw new ArgumentNullException(nameof(clipName));
        Duration = duration;
    }

    #endregion

    #region IAnimationState 实现

    public void Play()
    {
        IsPlaying = true;
    }

    public void Stop()
    {
        IsPlaying = false;
        NormalizedTime = 0f;
    }

    public void Pause()
    {
        IsPlaying = false;
    }

    internal void Update(float delta)
    {
        if (!IsPlaying || Duration <= 0f)
        {
            return;
        }

        NormalizedTime += (delta * Speed) / Duration;

        if (NormalizedTime >= 1f)
        {
            if (IsLooping)
            {
                NormalizedTime %= 1f;
            }
            else
            {
                NormalizedTime = 1f;
                IsPlaying = false;
            }
        }
    }

    #endregion
}
