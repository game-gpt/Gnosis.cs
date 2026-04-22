using Gnosis.Core;
using Gnosis.Core.Event;
using Gnosis.Network.RPC;
using Gnosis.Network.Serialization;
using Gnosis.Network.Transport;
using Gnosis.Security.AntiCheat;

namespace Gnosis.Network.Security;

public sealed class NetworkSecurityLayer
{
    #region 字段

    private readonly ProtocolHoneypot _honeypot;
    private readonly IServerValidator _serverValidator;
    private readonly Dictionary<ConnectionId, PlayerId> _connectionToPlayer = new();
    private readonly Dictionary<PlayerId, ConnectionId> _playerToConnection = new();

    #endregion

    #region 属性

    public ProtocolHoneypot Honeypot => _honeypot;

    public int FlaggedPlayerCount => _honeypot.FlaggedPlayerCount;

    #endregion

    #region 事件

    public event Action<PlayerId, string>? OnCheatDetected;

    public event Action<PlayerId, EntityId>? OnHoneypotTriggered;

    public event Action<PlayerId, string>? OnValidationFailed;

    #endregion

    #region 构造函数

    public NetworkSecurityLayer(ProtocolHoneypot honeypot, IServerValidator serverValidator)
    {
        _honeypot = honeypot ?? throw new ArgumentNullException(nameof(honeypot));
        _serverValidator = serverValidator ?? throw new ArgumentNullException(nameof(serverValidator));
    }

    public NetworkSecurityLayer() : this(new ProtocolHoneypot(), new DefaultServerValidator())
    {
    }

    #endregion

    #region 公共方法 - 连接映射

    public void RegisterPlayer(ConnectionId connectionId, PlayerId playerId)
    {
        _connectionToPlayer[connectionId] = playerId;
        _playerToConnection[playerId] = connectionId;
    }

    public void UnregisterConnection(ConnectionId connectionId)
    {
        if (_connectionToPlayer.TryGetValue(connectionId, out var playerId))
        {
            _playerToConnection.Remove(playerId);
            _connectionToPlayer.Remove(connectionId);
        }
    }

    public bool TryGetPlayerId(ConnectionId connectionId, out PlayerId playerId)
    {
        return _connectionToPlayer.TryGetValue(connectionId, out playerId);
    }

    public bool TryGetConnectionId(PlayerId playerId, out ConnectionId connectionId)
    {
        return _playerToConnection.TryGetValue(playerId, out connectionId);
    }

    #endregion

    #region 公共方法 - 蜜罐检测

    public bool CheckEntityInteraction(ConnectionId connectionId, EntityId targetEntityId)
    {
        if (!TryGetPlayerId(connectionId, out var playerId))
        {
            return false;
        }

        if (_honeypot.CheckInteraction(playerId, targetEntityId))
        {
            OnHoneypotTriggered?.Invoke(playerId, targetEntityId);
            OnCheatDetected?.Invoke(playerId, $"蜜罐实体交互：{targetEntityId}");
            return true;
        }

        return false;
    }

    public bool IsPlayerFlagged(PlayerId playerId)
    {
        return _honeypot.IsFlagged(playerId);
    }

    public bool IsConnectionFlagged(ConnectionId connectionId)
    {
        return TryGetPlayerId(connectionId, out var playerId) && _honeypot.IsFlagged(playerId);
    }

    #endregion

    #region 公共方法 - 服务器校验

    public bool ValidatePlayerAction(ConnectionId connectionId, string actionType, byte[] actionData)
    {
        if (!TryGetPlayerId(connectionId, out var playerId))
        {
            return false;
        }

        var isValid = _serverValidator.ValidatePlayerAction(playerId, actionType, actionData);

        if (!isValid)
        {
            OnValidationFailed?.Invoke(playerId, $"玩家行为校验失败：{actionType}");
            OnCheatDetected?.Invoke(playerId, $"行为校验失败：{actionType}");
        }

        return isValid;
    }

    public bool ValidatePosition(ConnectionId connectionId, float x, float y, float z, float timestamp)
    {
        if (!TryGetPlayerId(connectionId, out var playerId))
        {
            return false;
        }

        var isValid = _serverValidator.ValidatePosition(playerId, x, y, z, timestamp);

        if (!isValid)
        {
            OnValidationFailed?.Invoke(playerId, "位置校验失败");
            _serverValidator.RecordSuspiciousActivity(playerId, ViolationType.SpeedHack, $"位置 ({x},{y},{z}) 时间 {timestamp}");
        }

        return isValid;
    }

    public bool ValidateTransaction(ConnectionId connectionId, string transactionType, long amount)
    {
        if (!TryGetPlayerId(connectionId, out var playerId))
        {
            return false;
        }

        var isValid = _serverValidator.ValidateTransaction(playerId, transactionType, amount);

        if (!isValid)
        {
            OnValidationFailed?.Invoke(playerId, $"交易校验失败：{transactionType} 金额 {amount}");
            _serverValidator.RecordSuspiciousActivity(playerId, ViolationType.ServerValidationFailed, $"交易 {transactionType} 金额 {amount}");
        }

        return isValid;
    }

    #endregion

    #region 公共方法 - 蜜罐管理

    public void SpawnHoneypotEntity(EntityId entityId, string trapType, float x = 0f, float y = -9999f, float z = 0f)
    {
        _honeypot.SpawnTrapEntity(entityId, trapType, x, y, z);
    }

    public void AddHoneypotField(string fieldName, object fakeValue)
    {
        _honeypot.AddTrapField(fieldName, fakeValue);
    }

    #endregion

    #region 内部类

    private sealed class DefaultServerValidator : IServerValidator
    {
        private readonly Dictionary<PlayerId, List<(ViolationType, string)>> _suspiciousActivities = new();

        public bool ValidatePlayerAction(PlayerId playerId, string actionType, byte[] actionData)
        {
            return true;
        }

        public bool ValidatePosition(PlayerId playerId, float x, float y, float z, float timestamp)
        {
            return true;
        }

        public bool ValidateTransaction(PlayerId playerId, string transactionType, long amount)
        {
            return amount >= 0;
        }

        public void RecordSuspiciousActivity(PlayerId playerId, ViolationType type, string details)
        {
            if (!_suspiciousActivities.TryGetValue(playerId, out var activities))
            {
                activities = [];
                _suspiciousActivities[playerId] = activities;
            }

            activities.Add((type, details));
        }
    }

    #endregion
}
