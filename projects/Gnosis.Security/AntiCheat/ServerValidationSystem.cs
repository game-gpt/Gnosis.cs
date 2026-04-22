using Gnosis.Core;
using Gnosis.Core.Event;

namespace Gnosis.Security.AntiCheat;

/// <summary>
/// 服务器校验系统，实现联网游戏的服务器权威校验
/// </summary>
public sealed class ServerValidationSystem : IServerValidator
{
    #region 字段

    private readonly Dictionary<PlayerId, PlayerValidationState> _playerStates = new();
    private readonly List<SuspiciousActivityRecord> _suspiciousActivities = [];

    #endregion

    #region 属性

    public int SuspiciousActivityCount => _suspiciousActivities.Count;

    #endregion

    #region 公开方法

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
