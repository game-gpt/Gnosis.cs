using Gnosis.Core;
using Gnosis.Core.Event;

namespace Gnosis.Security.AntiCheat;

public sealed class RateLimiterImpl : IRateLimiter
{
    #region 字段

    private readonly Dictionary<(PlayerId, string), RateLimitState> _states = new();
    private readonly int _maxCalls;
    private readonly double _perSeconds;

    #endregion

    #region 构造函数

    public RateLimiterImpl(int maxCalls, double perSeconds)
    {
        _maxCalls = maxCalls;
        _perSeconds = perSeconds;
    }

    #endregion

    #region 公开方法

    public bool IsAllowed(PlayerId playerId, string actionType)
    {
        var key = (playerId, actionType);

        if (!_states.TryGetValue(key, out var state))
        {
            return true;
        }

        var now = Environment.TickCount64 / 1000.0;
        var windowStart = now - _perSeconds;
        state.PruneOlderThan(windowStart);

        return state.CallCount < _maxCalls;
    }

    public void RecordAction(PlayerId playerId, string actionType)
    {
        var key = (playerId, actionType);

        if (!_states.TryGetValue(key, out var state))
        {
            state = new RateLimitState();
            _states[key] = state;
        }

        state.Record();
    }

    public void Reset(PlayerId playerId)
    {
        var keysToRemove = _states.Keys.Where(k => k.Item1 == playerId).ToList();

        foreach (var key in keysToRemove)
        {
            _states.Remove(key);
        }
    }

    #endregion

    #region 内部类

    private sealed class RateLimitState
    {
        private readonly List<double> _callTimes = [];

        public int CallCount => _callTimes.Count;

        public void Record()
        {
            _callTimes.Add(Environment.TickCount64 / 1000.0);
        }

        public void PruneOlderThan(double timestamp)
        {
            _callTimes.RemoveAll(t => t < timestamp);
        }
    }

    #endregion
}
