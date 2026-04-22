using Gnosis.Core;
using Gnosis.Core.Event;

namespace Gnosis.Security.AntiCheat;

/// <summary>
/// 行为记录器，记录玩家行为序列用于后续分析
/// </summary>
public sealed class BehaviorLogger
{
    #region 字段

    private readonly Dictionary<PlayerId, PlayerBehaviorLog> _logs = new();
    private readonly int _maxActionsPerPlayer;

    #endregion

    #region 属性

    public int TrackedPlayerCount => _logs.Count;

    #endregion

    #region 构造函数

    public BehaviorLogger(int maxActionsPerPlayer = 1000)
    {
        _maxActionsPerPlayer = maxActionsPerPlayer;
    }

    #endregion

    #region 公开方法

    public void RecordAction(PlayerId playerId, string actionType, string details)
    {
        if (!_logs.TryGetValue(playerId, out var log))
        {
            log = new PlayerBehaviorLog(playerId, _maxActionsPerPlayer);
            _logs[playerId] = log;
        }

        log.Record(actionType, details);
    }

    public PlayerActionSequence GetActionSequence(PlayerId playerId)
    {
        if (!_logs.TryGetValue(playerId, out var log))
        {
            return new PlayerActionSequence(playerId, []);
        }

        return log.GetSequence();
    }

    public void ClearPlayerLog(PlayerId playerId)
    {
        _logs.Remove(playerId);
    }

    public void ClearAll()
    {
        _logs.Clear();
    }

    #endregion

    #region 内部类

    private sealed class PlayerBehaviorLog
    {
        private readonly PlayerId _playerId;
        private readonly int _maxActions;
        private readonly List<ActionRecord> _actions = [];

        public PlayerBehaviorLog(PlayerId playerId, int maxActions)
        {
            _playerId = playerId;
            _maxActions = maxActions;
        }

        public void Record(string actionType, string details)
        {
            _actions.Add(new ActionRecord(actionType, details, Environment.TickCount64));

            if (_actions.Count > _maxActions)
            {
                _actions.RemoveAt(0);
            }
        }

        public PlayerActionSequence GetSequence()
        {
            return new PlayerActionSequence(_playerId, _actions.ToArray());
        }
    }

    #endregion
}

/// <summary>
/// 玩家行为序列
/// </summary>
/// <param name="PlayerId">玩家 ID</param>
/// <param name="Actions">行为记录数组</param>
public sealed record PlayerActionSequence(PlayerId PlayerId, ActionRecord[] Actions);

/// <summary>
/// 行为记录
/// </summary>
/// <param name="ActionType">行为类型</param>
/// <param name="Details">行为详情</param>
/// <param name="TimestampMs">时间戳（毫秒）</param>
public sealed record ActionRecord(string ActionType, string Details, long TimestampMs);
