namespace Gnosis.Scene.Stream;

public enum SceneTransitionType
{
    Fade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
    WipeLeft,
    WipeRight,
    Dissolve
}

public sealed class SceneTransitionController
{
    #region 属性

    public SceneTransitionType Type { get; private set; }
    public float Duration { get; private set; }
    public float Progress { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool IsComplete => Progress >= 1.0f;

    #endregion

    #region 事件

    public event Action? OnComplete;

    #endregion

    #region 字段

    private float _elapsedTime;

    #endregion

    #region 公开方法

    public void Play(SceneTransitionType type, float duration)
    {
        Type = type;
        Duration = duration;
        IsPlaying = true;
        _elapsedTime = 0f;
        Progress = 0f;
    }

    public void Stop()
    {
        IsPlaying = false;
    }

    public void Reset()
    {
        _elapsedTime = 0f;
        Progress = 0f;
        IsPlaying = false;
    }

    public void Update(float deltaTime)
    {
        if (!IsPlaying)
        {
            return;
        }

        _elapsedTime += deltaTime;
        Progress = Math.Min(_elapsedTime / Duration, 1.0f);

        if (IsComplete)
        {
            IsPlaying = false;
            OnComplete?.Invoke();
        }
    }

    public static SceneTransitionType ParseTransitionType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "fade" => SceneTransitionType.Fade,
            "slideleft" => SceneTransitionType.SlideLeft,
            "slideright" => SceneTransitionType.SlideRight,
            "slideup" => SceneTransitionType.SlideUp,
            "slidedown" => SceneTransitionType.SlideDown,
            "wipeleft" => SceneTransitionType.WipeLeft,
            "wiperight" => SceneTransitionType.WipeRight,
            "dissolve" => SceneTransitionType.Dissolve,
            _ => SceneTransitionType.Fade
        };
    }

    #endregion
}
