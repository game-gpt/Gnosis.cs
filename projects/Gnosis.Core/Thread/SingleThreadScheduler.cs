using System.Runtime.CompilerServices;

namespace Gnosis.Core.Thread;

public sealed class SingleThreadScheduler : IScheduler
{
    #region 字段

    private readonly List<Action> _pendingActions = new();
    private readonly List<(Action action, float remainingTime)> _delayedActions = new();

    #endregion

    #region 属性

    public int PendingCount => _pendingActions.Count + _delayedActions.Count;

    #endregion

    #region 公开方法

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Schedule(Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        _pendingActions.Add(action);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ScheduleDelayed(Action action, float delaySeconds)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (delaySeconds <= 0f)
        {
            _pendingActions.Add(action);
            return;
        }

        _delayedActions.Add((action, delaySeconds));
    }

    #endregion

    #region IScheduler 实现

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(float deltaTime)
    {
        for (var i = 0; i < _pendingActions.Count; i++)
        {
            _pendingActions[i]();
        }

        _pendingActions.Clear();

        for (var i = _delayedActions.Count - 1; i >= 0; i--)
        {
            var (action, remaining) = _delayedActions[i];
            remaining -= deltaTime;

            if (remaining <= 0f)
            {
                action();
                _delayedActions.RemoveAt(i);
            }
            else
            {
                _delayedActions[i] = (action, remaining);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void FixedUpdate(float fixedDeltaTime)
    {
    }

    #endregion
}
