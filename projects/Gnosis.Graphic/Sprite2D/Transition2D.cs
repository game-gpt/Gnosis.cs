using System.Numerics;

namespace Gnosis.Graphic.Sprite2D;

public sealed class Transition2D
{
    #region 属性

    public TransitionType Type { get; }
    public float Duration { get; }
    public float Progress { get; private set; }
    public bool IsPlaying { get; private set; }
    public bool IsComplete => Progress >= 1.0f;
    public bool IsReverse { get; private set; }
    public EasingFunction Easing { get; set; } = EasingFunction.Linear;

    #endregion

    #region 事件

    public event Action? OnComplete;

    #endregion

    #region 内部状态

    private float _elapsedTime;

    #endregion

    #region 构造函数

    public Transition2D(TransitionType type, float duration)
    {
        Type = type;
        Duration = duration;
    }

    #endregion

    #region 控制

    public void Play()
    {
        IsPlaying = true;
        IsReverse = false;
        _elapsedTime = 0.0f;
        Progress = 0.0f;
    }

    public void PlayReverse()
    {
        IsPlaying = true;
        IsReverse = true;
        _elapsedTime = 0.0f;
        Progress = 1.0f;
    }

    public void Stop()
    {
        IsPlaying = false;
    }

    public void Reset()
    {
        _elapsedTime = 0.0f;
        Progress = IsReverse ? 1.0f : 0.0f;
        IsPlaying = false;
    }

    #endregion

    #region 更新

    public void Update(float deltaTime)
    {
        if (!IsPlaying)
        {
            return;
        }

        if (IsReverse)
        {
            _elapsedTime += deltaTime;
            Progress = Math.Max(1.0f - _elapsedTime / Duration, 0.0f);

            if (Progress <= 0.0f)
            {
                Progress = 0.0f;
                IsPlaying = false;
                OnComplete?.Invoke();
            }
        }
        else
        {
            _elapsedTime += deltaTime;
            Progress = Math.Min(_elapsedTime / Duration, 1.0f);

            if (IsComplete)
            {
                IsPlaying = false;
                OnComplete?.Invoke();
            }
        }
    }

    #endregion

    #region 效果计算

    public float GetEasedProgress()
    {
        return ApplyEasing(Progress, Easing);
    }

    public Vector4 GetFadeColor()
    {
        var p = GetEasedProgress();
        return Type switch
        {
            TransitionType.Fade => new Vector4(0, 0, 0, p),
            _ => Vector4.Zero
        };
    }

    public Vector2 GetSlideOffset(Vector2 screenSize)
    {
        var p = GetEasedProgress();
        return Type switch
        {
            TransitionType.SlideLeft => new Vector2(-screenSize.X * p, 0),
            TransitionType.SlideRight => new Vector2(screenSize.X * p, 0),
            TransitionType.SlideUp => new Vector2(0, -screenSize.Y * p),
            TransitionType.SlideDown => new Vector2(0, screenSize.Y * p),
            _ => Vector2.Zero
        };
    }

    public float GetWipeProgress()
    {
        return Type switch
        {
            TransitionType.WipeLeft => GetEasedProgress(),
            TransitionType.WipeRight => 1.0f - GetEasedProgress(),
            TransitionType.Dissolve => GetEasedProgress(),
            _ => 0.0f
        };
    }

    #endregion

    #region 缓动函数

    private static float ApplyEasing(float t, EasingFunction easing)
    {
        return easing switch
        {
            EasingFunction.Linear => t,
            EasingFunction.EaseInQuad => t * t,
            EasingFunction.EaseOutQuad => t * (2 - t),
            EasingFunction.EaseInOutQuad => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t,
            EasingFunction.EaseInCubic => t * t * t,
            EasingFunction.EaseOutCubic => (--t) * t * t + 1,
            EasingFunction.EaseInOutCubic => t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1,
            _ => t
        };
    }

    #endregion
}

public enum EasingFunction
{
    Linear,
    EaseInQuad,
    EaseOutQuad,
    EaseInOutQuad,
    EaseInCubic,
    EaseOutCubic,
    EaseInOutCubic
}
