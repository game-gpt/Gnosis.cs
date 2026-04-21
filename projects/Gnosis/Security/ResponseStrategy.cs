using Gnosis.Core;

namespace Gnosis.Security;

/// <summary>
/// 响应策略，根据违规类型和严重程度执行不同的惩罚措施
/// </summary>
public sealed class ResponseStrategy
{
    #region 字段

    private readonly Dictionary<ViolationType, ResponseRule> _rules = new();
    private readonly Dictionary<PlayerId, int> _offenseCounts = new();
    private readonly Dictionary<PlayerId, List<ViolationRecord>> _violationHistory = new();

    #endregion

    #region 属性

    /// <summary>
    /// 获取是否启用延迟惩罚
    /// </summary>
    public bool IsDelayedPenaltyEnabled { get; private set; }

    /// <summary>
    /// 获取延迟惩罚的时间（秒），默认 300 秒（5 分钟）
    /// </summary>
    public int DelayedPenaltySeconds { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化响应策略
    /// </summary>
    /// <param name="enableDelayedPenalty">是否启用延迟惩罚</param>
    /// <param name="delayedPenaltySeconds">延迟惩罚时间（秒）</param>
    public ResponseStrategy(bool enableDelayedPenalty = true, int delayedPenaltySeconds = 300)
    {
        IsDelayedPenaltyEnabled = enableDelayedPenalty;
        DelayedPenaltySeconds = delayedPenaltySeconds;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 配置指定违规类型的响应规则
    /// </summary>
    /// <param name="violationType">违规类型</param>
    /// <param name="action">响应动作</param>
    /// <param name="detectionLevel">检测级别</param>
    public void ConfigureRule(ViolationType violationType, ResponseAction action, DetectionLevel detectionLevel = DetectionLevel.Warning)
    {
        _rules[violationType] = new ResponseRule(violationType, action, detectionLevel);
    }

    /// <summary>
    /// 根据违规事件决定响应动作
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="violationType">违规类型</param>
    /// <returns>响应动作</returns>
    public ResponseAction DetermineResponse(PlayerId playerId, ViolationType violationType)
    {
        if (!_offenseCounts.TryGetValue(playerId, out var count))
        {
            count = 0;
        }

        _offenseCounts[playerId] = count + 1;

        if (!_rules.TryGetValue(violationType, out var rule))
        {
            return ResponseAction.Log;
        }

        return EscalateAction(rule.Action, count);
    }

    /// <summary>
    /// 记录违规事件
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="violationType">违规类型</param>
    /// <param name="details">详细信息</param>
    public void RecordViolation(PlayerId playerId, ViolationType violationType, string details)
    {
        if (!_violationHistory.TryGetValue(playerId, out var history))
        {
            history = [];
            _violationHistory[playerId] = history;
        }

        history.Add(new ViolationRecord(violationType, details, Environment.TickCount64));
    }

    /// <summary>
    /// 获取指定玩家的违规历史
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <returns>违规记录列表</returns>
    public List<ViolationRecord> GetViolationHistory(PlayerId playerId)
    {
        return _violationHistory.TryGetValue(playerId, out var history) ? history : [];
    }

    /// <summary>
    /// 获取指定玩家的违规次数
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <returns>违规次数</returns>
    public int GetOffenseCount(PlayerId playerId)
    {
        return _offenseCounts.TryGetValue(playerId, out var count) ? count : 0;
    }

    #endregion

    #region 私有方法

    private static ResponseAction EscalateAction(ResponseAction baseAction, int offenseCount)
    {
        if (offenseCount <= 1)
        {
            return baseAction;
        }

        if (offenseCount == 2)
        {
            return baseAction switch
            {
                ResponseAction.Log => ResponseAction.SoftPenalty,
                ResponseAction.SoftPenalty => ResponseAction.Kick,
                _ => baseAction
            };
        }

        if (offenseCount >= 3)
        {
            return baseAction switch
            {
                ResponseAction.Log => ResponseAction.Kick,
                ResponseAction.SoftPenalty => ResponseAction.ShadowBan,
                ResponseAction.Kick => ResponseAction.Ban,
                _ => baseAction
            };
        }

        return baseAction;
    }

    #endregion

    #region 内部记录

    /// <summary>
    /// 响应规则
    /// </summary>
    /// <param name="ViolationType">违规类型</param>
    /// <param name="Action">响应动作</param>
    /// <param name="DetectionLevel">检测级别</param>
    private sealed record ResponseRule(ViolationType ViolationType, ResponseAction Action, DetectionLevel DetectionLevel);

    #endregion
}

/// <summary>
/// 违规记录
/// </summary>
/// <param name="ViolationType">违规类型</param>
/// <param name="Details">详细信息</param>
/// <param name="TimestampMs">时间戳（毫秒）</param>
public sealed record ViolationRecord(ViolationType ViolationType, string Details, long TimestampMs);
