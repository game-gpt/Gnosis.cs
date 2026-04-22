using Gnosis.Core;
using Gnosis.Core.Event;

namespace Gnosis.Security.AntiCheat;

/// <summary>
/// 行为分析器，提取玩家行为特征并检测异常行为
/// </summary>
public sealed class BehaviorAnalyzer
{
    #region 字段

    private readonly Dictionary<PlayerId, PlayerFeatureProfile> _profiles = new();
    private readonly List<BehaviorRule> _rules = [];
    private float _anomalyThreshold;

    #endregion

    #region 属性

    public float AnomalyThreshold
    {
        get => _anomalyThreshold;
        set => _anomalyThreshold = Math.Clamp(value, 0f, 1f);
    }

    public int AnalyzedPlayerCount => _profiles.Count;

    public int RuleCount => _rules.Count;

    #endregion

    #region 构造函数

    public BehaviorAnalyzer(float anomalyThreshold = 0.8f)
    {
        _anomalyThreshold = Math.Clamp(anomalyThreshold, 0f, 1f);
    }

    #endregion

    #region 公开方法

    public void RegisterRule(string ruleName, Func<PlayerActionSequence, float> analyzeFunc)
    {
        _rules.Add(new BehaviorRule(ruleName, analyzeFunc));
    }

    public float Analyze(PlayerActionSequence sequence)
    {
        if (_rules.Count == 0)
        {
            return 0f;
        }

        var maxProbability = 0f;

        foreach (var rule in _rules)
        {
            var probability = rule.AnalyzeFunc(sequence);
            maxProbability = Math.Max(maxProbability, probability);
        }

        return maxProbability;
    }

    public float[] ExtractFeatures(PlayerActionSequence sequence)
    {
        if (sequence.Actions.Length == 0)
        {
            return [];
        }

        var features = new float[4];
        features[0] = sequence.Actions.Length;

        var actionTypes = new HashSet<string>();
        foreach (var action in sequence.Actions)
        {
            actionTypes.Add(action.ActionType);
        }
        features[1] = actionTypes.Count;

        if (sequence.Actions.Length >= 2)
        {
            var totalInterval = sequence.Actions[^1].TimestampMs - sequence.Actions[0].TimestampMs;
            features[2] = totalInterval / (float)(sequence.Actions.Length - 1);
        }

        features[3] = sequence.Actions.Count(a => a.ActionType == "move") / (float)sequence.Actions.Length;

        return features;
    }

    public bool IsAnomalous(float probability)
    {
        return probability >= _anomalyThreshold;
    }

    public void UpdateProfile(PlayerId playerId, float[] features)
    {
        _profiles[playerId] = new PlayerFeatureProfile(playerId, features);
    }

    public PlayerFeatureProfile? GetProfile(PlayerId playerId)
    {
        return _profiles.GetValueOrDefault(playerId);
    }

    #endregion

    #region 内部记录

    private sealed record BehaviorRule(string Name, Func<PlayerActionSequence, float> AnalyzeFunc);

    #endregion
}

/// <summary>
/// 玩家特征档案
/// </summary>
/// <param name="PlayerId">玩家 ID</param>
/// <param name="Features">特征向量</param>
public sealed record PlayerFeatureProfile(PlayerId PlayerId, float[] Features);
