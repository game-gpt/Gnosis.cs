using System.Numerics;

namespace Gnosis.Runtime.Coroutine;

/// <summary>
/// 补间动画系统实现
/// </summary>
public class TweenSystem : ITweenSystem
{
    #region Fields

    private readonly List<Tween> _activeTweens = new();
    private readonly List<TweenSequence> _activeSequences = new();
    private readonly List<Tween> _pendingAdd = new();

    #endregion

    #region ITweenSystem 方法

    public ITween To(Action<float> setter, float startValue, float endValue, float duration)
    {
        var tween = new Tween(setter, startValue, endValue, duration);
        _pendingAdd.Add(tween);
        return tween;
    }

    public ITween To(Action<Vector3> setter, Vector3 startValue, Vector3 endValue, float duration)
    {
        void VectorSetter(float progress)
        {
            var current = Vector3.Lerp(startValue, endValue, progress);
            setter(current);
        }

        var tween = new Tween(VectorSetter, 0f, 1f, duration);
        _pendingAdd.Add(tween);
        return tween;
    }

    public ITween To(Action<float[]> setter, float[] startValue, float[] endValue, float duration)
    {
        var current = new float[startValue.Length];
        var start = (float[])startValue.Clone();
        var end = (float[])endValue.Clone();

        void MultiSetter(float progress)
        {
            for (var i = 0; i < current.Length; i++)
            {
                current[i] = start[i] + (end[i] - start[i]) * progress;
            }

            setter(current);
        }

        var tween = new Tween(MultiSetter, 0f, 1f, duration);
        _pendingAdd.Add(tween);
        return tween;
    }

    public ITweenSequence CreateSequence()
    {
        var sequence = new TweenSequence();
        _activeSequences.Add(sequence);
        return sequence;
    }

    public void KillAll()
    {
        foreach (var tween in _activeTweens)
        {
            tween.Kill();
        }

        foreach (var sequence in _activeSequences)
        {
            sequence.Kill();
        }

        _activeTweens.Clear();
        _activeSequences.Clear();
        _pendingAdd.Clear();
    }

    public void Update(float delta)
    {
        if (_pendingAdd.Count > 0)
        {
            _activeTweens.AddRange(_pendingAdd);
            _pendingAdd.Clear();
        }

        for (var i = _activeTweens.Count - 1; i >= 0; i--)
        {
            var tween = _activeTweens[i];

            if (!tween.Tick(delta))
            {
                _activeTweens.RemoveAt(i);
            }
        }

        for (var i = _activeSequences.Count - 1; i >= 0; i--)
        {
            var sequence = _activeSequences[i];

            if (!sequence.Tick(delta))
            {
                _activeSequences.RemoveAt(i);
            }
        }
    }

    #endregion
}
