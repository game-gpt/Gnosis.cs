namespace Gnosis.Runtime.Coroutine;

/// <summary>
/// 补间动画序列实现
/// </summary>
public class TweenSequence : ITweenSequence
{
    #region Fields

    private readonly List<SequenceEntry> _entries = new();
    private float _totalDuration;
    private float _elapsed;
    private bool _isPlaying;
    private bool _isKilled;
    private Action? _onComplete;

    #endregion

    #region Properties

    public bool IsPlaying => _isPlaying && !_isKilled;

    #endregion

    #region ITweenSequence 方法

    public ITweenSequence Append(ITween tween)
    {
        _entries.Add(new SequenceEntry(_totalDuration, tween));
        _totalDuration += tween.Duration;
        return this;
    }

    public ITweenSequence Join(ITween tween)
    {
        var startTime = _entries.Count > 0 ? _entries[^1].StartTime : 0;
        _entries.Add(new SequenceEntry(startTime, tween));
        var endTime = startTime + tween.Duration;

        if (endTime > _totalDuration)
        {
            _totalDuration = endTime;
        }

        return this;
    }

    public ITweenSequence Insert(float time, ITween tween)
    {
        _entries.Add(new SequenceEntry(time, tween));
        var endTime = time + tween.Duration;

        if (endTime > _totalDuration)
        {
            _totalDuration = endTime;
        }

        return this;
    }

    public ITweenSequence OnComplete(Action callback)
    {
        _onComplete = callback;
        return this;
    }

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
        _isKilled = false;
        _isPlaying = true;
    }

    public void Kill()
    {
        _isKilled = true;
        _isPlaying = false;

        foreach (var entry in _entries)
        {
            entry.Tween.Kill();
        }
    }

    #endregion

    #region 内部更新

    internal bool Tick(float delta)
    {
        if (_isKilled || !_isPlaying)
        {
            return false;
        }

        _elapsed += delta;

        foreach (var entry in _entries)
        {
            var localTime = _elapsed - entry.StartTime;

            if (localTime < 0)
            {
                continue;
            }

            if (!entry.Tween.IsPlaying && !entry.Tween.IsComplete && localTime >= 0)
            {
                entry.Tween.Play();
            }
        }

        if (_elapsed >= _totalDuration)
        {
            _isPlaying = false;
            _onComplete?.Invoke();
            return false;
        }

        return true;
    }

    #endregion

    #region 嵌套类型

    private record SequenceEntry(float StartTime, ITween Tween);

    #endregion
}
