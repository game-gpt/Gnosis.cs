using Gnosis.Core;
using Gnosis.Core.Event;

namespace Gnosis.Security.AntiCheat;

public sealed class ResponseStrategy
{
    #region 字段

    private readonly Dictionary<ViolationType, ResponseRule> _rules = new();
    private readonly Dictionary<PlayerId, int> _offenseCounts = new();
    private readonly Dictionary<PlayerId, List<ViolationRecord>> _violationHistory = new();

    #endregion

    #region 属性

    public bool IsDelayedPenaltyEnabled { get; private set; }
    public int DelayedPenaltySeconds { get; private set; }

    #endregion

    #region 构造函数

    public ResponseStrategy(bool enableDelayedPenalty = true, int delayedPenaltySeconds = 300)
    {
        IsDelayedPenaltyEnabled = enableDelayedPenalty;
        DelayedPenaltySeconds = delayedPenaltySeconds;
    }

    #endregion

    #region 公开方法

    public void ConfigureRule(ViolationType violationType, ResponseAction action, DetectionLevel detectionLevel = DetectionLevel.Warning)
    {
        _rules[violationType] = new ResponseRule(violationType, action, detectionLevel);
    }

    public ResponseAction DetermineResponse(PlayerId playerId, ViolationType violationType)
    {
        var count = _offenseCounts.GetValueOrDefault(playerId, 0);

        _offenseCounts[playerId] = count + 1;

        if (!_rules.TryGetValue(violationType, out var rule))
        {
            return ResponseAction.Log;
        }

        return EscalateAction(rule.Action, count);
    }

    public void RecordViolation(PlayerId playerId, ViolationType violationType, string details)
    {
        if (!_violationHistory.TryGetValue(playerId, out var history))
        {
            history = [];
            _violationHistory[playerId] = history;
        }

        history.Add(new ViolationRecord(violationType, details, Environment.TickCount64));
    }

    public List<ViolationRecord> GetViolationHistory(PlayerId playerId)
    {
        return _violationHistory.TryGetValue(playerId, out var history) ? history : [];
    }

    public int GetOffenseCount(PlayerId playerId)
    {
        return _offenseCounts.GetValueOrDefault(playerId, 0);
    }

    #endregion

    #region 私有方法

    private static ResponseAction EscalateAction(ResponseAction baseAction, int offenseCount)
    {
        if (offenseCount <= 1) return baseAction;
        if (offenseCount == 2) return baseAction switch
        {
            ResponseAction.Log => ResponseAction.SoftPenalty,
            ResponseAction.SoftPenalty => ResponseAction.Kick,
            _ => baseAction
        };
        return baseAction switch
        {
            ResponseAction.Log => ResponseAction.Kick,
            ResponseAction.SoftPenalty => ResponseAction.ShadowBan,
            ResponseAction.Kick => ResponseAction.Ban,
            _ => baseAction
        };
    }

    #endregion

    private sealed record ResponseRule(ViolationType ViolationType, ResponseAction Action, DetectionLevel DetectionLevel);
}

public sealed record ViolationRecord(ViolationType ViolationType, string Details, long TimestampMs);
