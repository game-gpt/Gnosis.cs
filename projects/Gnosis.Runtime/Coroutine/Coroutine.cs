using System.Collections;

namespace Gnosis.Runtime.Coroutine;

public class Coroutine : ICoroutine
{
    private readonly IEnumerator _routine;
    private IYieldInstruction? _currentYield;
    private CoroutineState _state;

    public bool IsRunning => _state == CoroutineState.Running;
    public bool IsPaused => _state == CoroutineState.Paused;
    public bool IsComplete => _state == CoroutineState.Complete;

    internal IYieldInstruction? CurrentYield => _currentYield;

    public Coroutine(IEnumerator routine)
    {
        _routine = routine;
        _state = CoroutineState.Running;
        _currentYield = null;
    }

    public void Pause()
    {
        if (_state == CoroutineState.Running)
        {
            _state = CoroutineState.Paused;
        }
    }

    public void Resume()
    {
        if (_state == CoroutineState.Paused)
        {
            _state = CoroutineState.Running;
        }
    }

    public void Stop()
    {
        _state = CoroutineState.Complete;
        _currentYield = null;
    }

    internal bool MoveNext()
    {
        if (_state != CoroutineState.Running)
        {
            return false;
        }

        if (_currentYield is not null && !_currentYield.IsDone)
        {
            return true;
        }

        _currentYield = null;

        if (!_routine.MoveNext())
        {
            _state = CoroutineState.Complete;
            return false;
        }

        if (_routine.Current is IYieldInstruction yieldInstruction)
        {
            _currentYield = yieldInstruction;
        }

        return true;
    }

    internal void UpdateYield(float delta)
    {
        _currentYield?.Update(delta);
    }

    private enum CoroutineState
    {
        Running,
        Paused,
        Complete
    }
}
