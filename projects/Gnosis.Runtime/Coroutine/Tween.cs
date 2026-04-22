namespace Gnosis.Runtime.Coroutine;

/// <summary>
/// 补间动画实现
/// </summary>
public class Tween : ITween
{
    #region Fields

    private readonly Action<float> _setter;
    private readonly float _startValue;
    private readonly float _endValue;
    private readonly float _duration;

    private float _elapsed;
    private float _delay;
    private Easing _ease;
    private int _loops;
    private int _completedLoops;

    private bool _isPlaying;
    private bool _isComplete;
    private bool _isKilled;

    private Action? _onStart;
    private Action? _onComplete;
    private Action<float>? _onUpdate;
    private Action? _onKill;

    private bool _startFired;

    #endregion

    #region Properties

    public bool IsPlaying => _isPlaying && !_isKilled;
    public bool IsComplete => _isComplete;
    public float Duration => _duration;
    public float Elapsed => _elapsed;
    public float Progress => _duration > 0 ? Math.Clamp(_elapsed / _duration, 0f, 1f) : 0f;

    #endregion

    #region Constructors

    public Tween(Action<float> setter, float startValue, float endValue, float duration)
    {
        _setter = setter;
        _startValue = startValue;
        _endValue = endValue;
        _duration = duration;
        _ease = Easing.Linear;
        _loops = 1;
        _completedLoops = 0;
    }

    #endregion

    #region ITween 链式配置

    public ITween SetDelay(float delay)
    {
        _delay = delay;
        return this;
    }

    public ITween SetEase(Easing ease)
    {
        _ease = ease;
        return this;
    }

    public ITween SetLoops(int loops)
    {
        _loops = loops;
        return this;
    }

    public ITween OnStart(Action callback)
    {
        _onStart = callback;
        return this;
    }

    public ITween OnComplete(Action callback)
    {
        _onComplete = callback;
        return this;
    }

    public ITween OnUpdate(Action<float> callback)
    {
        _onUpdate = callback;
        return this;
    }

    public ITween OnKill(Action callback)
    {
        _onKill = callback;
        return this;
    }

    #endregion

    #region ITween 控制

    public void Play()
    {
        if (_isKilled)
        {
            return;
        }

        _isPlaying = true;
    }

    public void Pause()
    {
        _isPlaying = false;
    }

    public void Restart()
    {
        _elapsed = 0;
        _completedLoops = 0;
        _isComplete = false;
        _isKilled = false;
        _startFired = false;
        _isPlaying = true;
    }

    public void Kill()
    {
        _isKilled = true;
        _isPlaying = false;
        _onKill?.Invoke();
    }

    #endregion

    #region 内部更新

    internal bool Tick(float delta)
    {
        if (_isKilled || !_isPlaying)
        {
            return false;
        }

        if (_delay > 0)
        {
            _delay -= delta;
            return true;
        }

        if (!_startFired)
        {
            _startFired = true;
            _onStart?.Invoke();
        }

        _elapsed += delta;
        _onUpdate?.Invoke(Progress);

        var easedProgress = EasingUtility.Evaluate(_ease, Progress);
        var value = _startValue + (_endValue - _startValue) * easedProgress;
        _setter(value);

        if (_elapsed >= _duration)
        {
            _completedLoops++;

            if (_loops <= 0 || _completedLoops < _loops)
            {
                _elapsed -= _duration;
            }
            else
            {
                _setter(_endValue);
                _isComplete = true;
                _isPlaying = false;
                _onComplete?.Invoke();
                return false;
            }
        }

        return true;
    }

    #endregion
}

/// <summary>
/// 缓动函数工具
/// </summary>
internal static class EasingUtility
{
    public static float Evaluate(Easing easing, float t)
    {
        return easing switch
        {
            Easing.Linear => t,
            Easing.InQuad => t * t,
            Easing.OutQuad => t * (2 - t),
            Easing.InOutQuad => t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t,
            Easing.InCubic => t * t * t,
            Easing.OutCubic => (--t) * t * t + 1,
            Easing.InOutCubic => t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1,
            Easing.InQuart => t * t * t * t,
            Easing.OutQuart => 1 - (--t) * t * t * t,
            Easing.InOutQuart => t < 0.5f ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t,
            Easing.InQuint => t * t * t * t * t,
            Easing.OutQuint => 1 + (--t) * t * t * t * t,
            Easing.InOutQuint => t < 0.5f ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t,
            Easing.InSine => 1 - MathF.Cos(t * MathF.PI / 2),
            Easing.OutSine => MathF.Sin(t * MathF.PI / 2),
            Easing.InOutSine => -(MathF.Cos(MathF.PI * t) - 1) / 2,
            Easing.InExpo => t == 0 ? 0 : MathF.Pow(2, 10 * t - 10),
            Easing.OutExpo => t == 1 ? 1 : 1 - MathF.Pow(2, -10 * t),
            Easing.InOutExpo => t == 0 ? 0 : t == 1 ? 1 : t < 0.5f
                ? MathF.Pow(2, 20 * t - 10) / 2
                : (2 - MathF.Pow(2, -20 * t + 10)) / 2,
            Easing.InCirc => 1 - MathF.Sqrt(1 - t * t),
            Easing.OutCirc => MathF.Sqrt(1 - (--t) * t),
            Easing.InOutCirc => t < 0.5f
                ? (1 - MathF.Sqrt(1 - 4 * t * t)) / 2
                : (MathF.Sqrt(1 - (--t) * (2 * t - 2) * (-2 * t - 2)) + 1) / 2,
            Easing.InElastic => ElasticIn(t),
            Easing.OutElastic => ElasticOut(t),
            Easing.InOutElastic => ElasticInOut(t),
            Easing.InBack => BackIn(t),
            Easing.OutBack => BackOut(t),
            Easing.InOutBack => BackInOut(t),
            Easing.InBounce => BounceIn(t),
            Easing.OutBounce => BounceOut(t),
            Easing.InOutBounce => BounceInOut(t),
            _ => t
        };
    }

    private static float ElasticIn(float t)
    {
        if (t == 0 || t == 1)
        {
            return t;
        }

        return -MathF.Pow(2, 10 * t - 10) * MathF.Sin((t * 10 - 10.75f) * (2 * MathF.PI / 3));
    }

    private static float ElasticOut(float t)
    {
        if (t == 0 || t == 1)
        {
            return t;
        }

        return MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * (2 * MathF.PI / 3)) + 1;
    }

    private static float ElasticInOut(float t)
    {
        if (t == 0 || t == 1)
        {
            return t;
        }

        if (t < 0.5f)
        {
            return -(MathF.Pow(2, 20 * t - 10) * MathF.Sin((20 * t - 11.125f) * (2 * MathF.PI / 4.5f))) / 2;
        }

        return (MathF.Pow(2, -20 * t + 10) * MathF.Sin((20 * t - 11.125f) * (2 * MathF.PI / 4.5f))) / 2 + 1;
    }

    private static float BackIn(float t)
    {
        const float s = 1.70158f;
        return t * t * ((s + 1) * t - s);
    }

    private static float BackOut(float t)
    {
        const float s = 1.70158f;
        --t;
        return t * t * ((s + 1) * t + s) + 1;
    }

    private static float BackInOut(float t)
    {
        const float s = 1.70158f * 1.525f;

        if (t < 0.5f)
        {
            return 2 * t * t * ((s + 1) * 2 * t - s);
        }

        --t;
        return 2 * t * t * ((s + 1) * 2 * t + s) + 2;
    }

    private static float BounceOut(float t)
    {
        if (t < 1 / 2.75f)
        {
            return 7.5625f * t * t;
        }

        if (t < 2 / 2.75f)
        {
            t -= 1.5f / 2.75f;
            return 7.5625f * t * t + 0.75f;
        }

        if (t < 2.5f / 2.75f)
        {
            t -= 2.25f / 2.75f;
            return 7.5625f * t * t + 0.9375f;
        }

        t -= 2.625f / 2.75f;
        return 7.5625f * t * t + 0.984375f;
    }

    private static float BounceIn(float t)
    {
        return 1 - BounceOut(1 - t);
    }

    private static float BounceInOut(float t)
    {
        return t < 0.5f
            ? (1 - BounceOut(1 - 2 * t)) / 2
            : (1 + BounceOut(2 * t - 1)) / 2;
    }
}
