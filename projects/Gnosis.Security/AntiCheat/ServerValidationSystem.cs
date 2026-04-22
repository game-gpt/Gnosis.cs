using Gnosis.Core;
using Gnosis.Core.Event;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.AntiCheat;

public sealed class ServerValidationSystem : IServerValidator
{
    #region 常量

    private const float MaxPositionDeltaPerSecond = 100f;
    private const float MaxTeleportDistance = 500f;

    #endregion

    #region 字段

    private readonly Dictionary<PlayerId, PlayerValidationState> _playerStates = new();
    private readonly List<SuspiciousActivityRecord> _suspiciousActivities = [];
    private readonly float _maxSpeedPerSecond;
    private readonly float _maxTeleportDistance;

    #endregion

    #region 属性

    public int SuspiciousActivityCount => _suspiciousActivities.Count;

    #endregion

    #region 构造函数

    public ServerValidationSystem(float maxSpeedPerSecond = MaxPositionDeltaPerSecond, float maxTeleportDistance = MaxTeleportDistance)
    {
        _maxSpeedPerSecond = maxSpeedPerSecond;
        _maxTeleportDistance = maxTeleportDistance;
    }

    #endregion

    #region 公开方法

    public bool ValidatePlayerAction(PlayerId playerId, string actionType, byte[] actionData)
    {
        if (actionData is null || actionData.Length == 0) return false;

        EnsurePlayerState(playerId);

        return actionType switch
        {
            "move" => ValidateMovementAction(playerId, actionData),
            "attack" => ValidateAttackAction(playerId, actionData),
            "trade" => ValidateTradeAction(playerId, actionData),
            _ => true
        };
    }

    public bool ValidatePosition(PlayerId playerId, float x, float y, float z, float timestamp)
    {
        var state = EnsurePlayerState(playerId);

        if (state.LastTimestamp <= 0)
        {
            UpdatePositionState(state, x, y, z, timestamp);
            return true;
        }

        var deltaTime = timestamp - state.LastTimestamp;
        if (deltaTime <= 0) return true;

        var dx = x - state.LastValidX;
        var dy = y - state.LastValidY;
        var dz = z - state.LastValidZ;
        var distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

        var speed = distance / deltaTime;

        if (speed > _maxSpeedPerSecond)
        {
            RecordSuspiciousActivity(playerId, ViolationType.SpeedHack,
                $"速度异常：{speed:F1} 单位/秒，最大允许：{_maxSpeedPerSecond} 单位/秒");
            return false;
        }

        if (distance > _maxTeleportDistance)
        {
            RecordSuspiciousActivity(playerId, ViolationType.SpeedHack,
                $"疑似瞬移：距离 {distance:F1}，最大允许：{_maxTeleportDistance}");
            return false;
        }

        UpdatePositionState(state, x, y, z, timestamp);
        return true;
    }

    public bool ValidateTransaction(PlayerId playerId, string transactionType, long amount)
    {
        var state = EnsurePlayerState(playerId);

        if (amount < 0)
        {
            RecordSuspiciousActivity(playerId, ViolationType.ServerValidationFailed,
                $"交易金额为负数：{amount}");
            return false;
        }

        if (amount > 1000000)
        {
            RecordSuspiciousActivity(playerId, ViolationType.ServerValidationFailed,
                $"交易金额异常过大：{amount}");
            return false;
        }

        if (transactionType == "spend" && amount > state.GoldBalance)
        {
            RecordSuspiciousActivity(playerId, ViolationType.ServerValidationFailed,
                $"消费金额 {amount} 超过余额 {state.GoldBalance}");
            return false;
        }

        if (transactionType == "earn")
        {
            state.GoldBalance += amount;
        }
        else if (transactionType == "spend")
        {
            state.GoldBalance -= amount;
        }

        return true;
    }

    public void RecordSuspiciousActivity(PlayerId playerId, ViolationType type, string details)
    {
        _suspiciousActivities.Add(new SuspiciousActivityRecord(playerId, type, details));
    }

    public List<SuspiciousActivityRecord> GetSuspiciousActivities(PlayerId playerId)
    {
        return _suspiciousActivities.FindAll(r => r.PlayerId == playerId);
    }

    #endregion

    #region 私有方法

    private PlayerValidationState EnsurePlayerState(PlayerId playerId)
    {
        if (!_playerStates.TryGetValue(playerId, out var state))
        {
            state = new PlayerValidationState(playerId);
            _playerStates[playerId] = state;
        }

        return state;
    }

    private static void UpdatePositionState(PlayerValidationState state, float x, float y, float z, float timestamp)
    {
        state.LastValidX = x;
        state.LastValidY = y;
        state.LastValidZ = z;
        state.LastTimestamp = timestamp;
    }

    private bool ValidateMovementAction(PlayerId playerId, byte[] actionData)
    {
        if (actionData.Length < 16) return false;
        return true;
    }

    private bool ValidateAttackAction(PlayerId playerId, byte[] actionData)
    {
        if (actionData.Length < 4) return false;
        return true;
    }

    private bool ValidateTradeAction(PlayerId playerId, byte[] actionData)
    {
        if (actionData.Length < 8) return false;
        return true;
    }

    #endregion

    #region 内部记录

    public sealed record SuspiciousActivityRecord(PlayerId PlayerId, ViolationType Type, string Details);

    #endregion

    #region 内部类

    private sealed class PlayerValidationState
    {
        public PlayerId PlayerId { get; }
        public float LastValidX { get; set; }
        public float LastValidY { get; set; }
        public float LastValidZ { get; set; }
        public float LastTimestamp { get; set; }
        public long GoldBalance { get; set; }

        public PlayerValidationState(PlayerId playerId)
        {
            PlayerId = playerId;
        }
    }

    #endregion
}
