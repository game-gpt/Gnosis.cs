using Gnosis.ECS.Core;

namespace Gnosis.Security;

/// <summary>
/// 频率限制器，限制玩家对特定操作的调用频率
/// </summary>
public sealed class RateLimiterImpl : IRateLimiter
{
    #region 字段

    private readonly Dictionary<(PlayerId, string), RateLimitState> _states = new();
    private readonly int _maxCalls;
    private readonly double _perSeconds;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化频率限制器
    /// </summary>
    /// <param name="maxCalls">时间窗口内最大调用次数</param>
    /// <param name="perSeconds">时间窗口（秒）</param>
    public RateLimiterImpl(int maxCalls, double perSeconds)
    {
        _maxCalls = maxCalls;
        _perSeconds = perSeconds;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 检查玩家对指定操作是否被允许调用
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="actionType">操作类型</param>
    /// <returns>如果允许调用返回 true，否则返回 false</returns>
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

    /// <summary>
    /// 记录玩家的一次操作调用
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="actionType">操作类型</param>
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

    /// <summary>
    /// 重置指定玩家的所有频率限制状态
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    public void Reset(PlayerId playerId)
    {
        var keysToRemove = new List<(PlayerId, string)>();

        foreach (var key in _states.Keys)
        {
            if (key.Item1 == playerId)
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _states.Remove(key);
        }
    }

    #endregion

    #region 内部类

    private sealed class RateLimitState
    {
        private readonly List<double> _callTimes = new();

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
