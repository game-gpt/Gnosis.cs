using System.Collections;

namespace Gnosis.Runtime.Coroutine;

public class CoroutineScheduler : ICoroutineSystem
{
    private readonly List<Coroutine> _activeCoroutines = new();
    private readonly List<Coroutine> _pendingAdd = new();

    public ICoroutine Start(IEnumerator routine)
    {
        var coroutine = new Coroutine(routine);
        _pendingAdd.Add(coroutine);
        return coroutine;
    }

    public void Stop(ICoroutine coroutine)
    {
        if (coroutine is Coroutine co)
        {
            co.Stop();
        }
    }

    public void StopAll()
    {
        foreach (var coroutine in _activeCoroutines)
        {
            coroutine.Stop();
        }

        _activeCoroutines.Clear();
        _pendingAdd.Clear();
    }

    public void Update(float delta)
    {
        if (_pendingAdd.Count > 0)
        {
            _activeCoroutines.AddRange(_pendingAdd);
            _pendingAdd.Clear();
        }

        for (var i = _activeCoroutines.Count - 1; i >= 0; i--)
        {
            var coroutine = _activeCoroutines[i];

            if (coroutine.IsComplete)
            {
                _activeCoroutines.RemoveAt(i);
                continue;
            }

            if (coroutine.IsPaused)
            {
                continue;
            }

            coroutine.UpdateYield(delta);

            if (!coroutine.MoveNext())
            {
                _activeCoroutines.RemoveAt(i);
            }
        }
    }

    public int ActiveCount => _activeCoroutines.Count;
}
