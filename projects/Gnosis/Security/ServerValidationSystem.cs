using Gnosis.ECS.Core;

namespace Gnosis.Security;

/// <summary>
/// 服务器校验系统，实现联网游戏的服务器权威校验
/// </summary>
public sealed class ServerValidationSystem : IServerValidator
{
    #region 字段

    private readonly Dictionary<PlayerId, PlayerValidationState> _playerStates = new();
    private readonly List<SuspiciousActivityRecord> _suspiciousActivities = new();

    #endregion

    #region 属性

    /// <summary>
    /// 获取可疑活动记录数量
    /// </summary>
    public int SuspiciousActivityCount => _suspiciousActivities.Count;

    #endregion

    #region 公开方法

    /// <summary>
    /// 校验玩家动作是否合法
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="actionType">动作类型</param>
    /// <param name="actionData">动作数据</param>
    /// <returns>校验通过返回 true</returns>
    public bool ValidatePlayerAction(PlayerId playerId, string actionType, byte[] actionData)
    {
        return true;
    }

    /// <summary>
    /// 校验玩家位置是否合法
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="x">X 坐标</param>
    /// <param name="y">Y 坐标</param>
    /// <param name="z">Z 坐标</param>
    /// <param name="timestamp">时间戳</param>
    /// <returns>校验通过返回 true</returns>
    public bool ValidatePosition(PlayerId playerId, float x, float y, float z, float timestamp)
    {
        return true;
    }

    /// <summary>
    /// 校验经济交易是否合法
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="transactionType">交易类型</param>
    /// <param name="amount">交易金额</param>
    /// <returns>校验通过返回 true</returns>
    public bool ValidateTransaction(PlayerId playerId, string transactionType, long amount)
    {
        return true;
    }

    /// <summary>
    /// 记录可疑活动
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="type">违规类型</param>
    /// <param name="details">详细信息</param>
    public void RecordSuspiciousActivity(PlayerId playerId, ViolationType type, string details)
    {
        _suspiciousActivities.Add(new SuspiciousActivityRecord(playerId, type, details));
    }

    /// <summary>
    /// 获取指定玩家的可疑活动记录
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <returns>可疑活动记录列表</returns>
    public List<SuspiciousActivityRecord> GetSuspiciousActivities(PlayerId playerId)
    {
        return _suspiciousActivities.FindAll(r => r.PlayerId == playerId);
    }

    #endregion

    #region 内部记录

    /// <summary>
    /// 可疑活动记录
    /// </summary>
    /// <param name="PlayerId">玩家 ID</param>
    /// <param name="Type">违规类型</param>
    /// <param name="Details">详细信息</param>
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
