using Gnosis.ECS.Core;

namespace Gnosis.Security;

/// <summary>
/// 行为分析器，提取玩家行为特征并检测异常行为
/// </summary>
public sealed class BehaviorAnalyzer
{
    #region 字段

    private readonly Dictionary<PlayerId, PlayerFeatureProfile> _profiles = new();
    private float _anomalyThreshold;

    #endregion

    #region 属性

    /// <summary>
    /// 获取或设置异常检测阈值，范围 0.0~1.0，默认 0.8
    /// </summary>
    public float AnomalyThreshold
    {
        get => _anomalyThreshold;
        set => _anomalyThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// 获取已分析的玩家数量
    /// </summary>
    public int AnalyzedPlayerCount => _profiles.Count;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化行为分析器
    /// </summary>
    /// <param name="anomalyThreshold">异常检测阈值，默认 0.8</param>
    public BehaviorAnalyzer(float anomalyThreshold = 0.8f)
    {
        _anomalyThreshold = Math.Clamp(anomalyThreshold, 0f, 1f);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 分析玩家行为序列，返回作弊概率
    /// </summary>
    /// <param name="sequence">玩家行为序列</param>
    /// <returns>作弊概率，范围 0.0~1.0</returns>
    public float Analyze(PlayerActionSequence sequence)
    {
        return 0f;
    }

    /// <summary>
    /// 提取玩家行为特征向量
    /// </summary>
    /// <param name="sequence">玩家行为序列</param>
    /// <returns>特征向量</returns>
    public float[] ExtractFeatures(PlayerActionSequence sequence)
    {
        return [];
    }

    /// <summary>
    /// 判断指定概率是否超过异常阈值
    /// </summary>
    /// <param name="probability">作弊概率</param>
    /// <returns>如果超过阈值返回 true</returns>
    public bool IsAnomalous(float probability)
    {
        return probability >= _anomalyThreshold;
    }

    /// <summary>
    /// 更新玩家特征档案
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <param name="features">特征向量</param>
    public void UpdateProfile(PlayerId playerId, float[] features)
    {
        _profiles[playerId] = new PlayerFeatureProfile(playerId, features);
    }

    /// <summary>
    /// 获取玩家特征档案
    /// </summary>
    /// <param name="playerId">玩家 ID</param>
    /// <returns>特征档案，如果不存在返回 null</returns>
    public PlayerFeatureProfile? GetProfile(PlayerId playerId)
    {
        return _profiles.TryGetValue(playerId, out var profile) ? profile : null;
    }

    #endregion
}

/// <summary>
/// 玩家特征档案
/// </summary>
/// <param name="PlayerId">玩家 ID</param>
/// <param name="Features">特征向量</param>
public sealed record PlayerFeatureProfile(PlayerId PlayerId, float[] Features);
