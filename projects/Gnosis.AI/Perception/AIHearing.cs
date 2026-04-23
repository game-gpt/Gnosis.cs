using Gnosis.Core.Math;

namespace Gnosis.AI.Perception;

/// <summary>
/// 听觉感知实现，基于距离和声音强度检测
/// </summary>
public sealed class AIHearing : IAISense
{
    #region 字段

    private readonly AIHearingConfig _config;
    private readonly List<IAIStimulusSource> _registeredTargets = new();
    private readonly List<IAIStimulusSource> _perceivedTargets = new();
    private float _timeSinceLastUpdate;

    #endregion

    #region 属性

    /// <summary>
    /// 感知类型
    /// </summary>
    public AISenseType SenseType => AISenseType.Hearing;

    /// <summary>
    /// 当前感知到的目标列表
    /// </summary>
    public IReadOnlyList<IAIStimulusSource> PerceivedTargets => _perceivedTargets;

    /// <summary>
    /// 感知者位置
    /// </summary>
    public Vector3 OwnerPosition { get; set; } = new(0, 0, 0);

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建听觉感知
    /// </summary>
    /// <param name="config">听觉感知配置</param>
    public AIHearing(AIHearingConfig config)
    {
        _config = config;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 更新听觉感知
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void Update(float delta)
    {
        if (!_config.IsEnabled)
        {
            _perceivedTargets.Clear();
            return;
        }

        _timeSinceLastUpdate += delta;

        if (_timeSinceLastUpdate < _config.Interval)
        {
            return;
        }

        _timeSinceLastUpdate = 0;
        _perceivedTargets.Clear();

        foreach (var target in _registeredTargets)
        {
            if (!target.IsActive)
            {
                continue;
            }

            if (IsTargetAudible(target))
            {
                _perceivedTargets.Add(target);
            }
        }
    }

    /// <summary>
    /// 注册可感知目标
    /// </summary>
    /// <param name="source">刺激源</param>
    public void RegisterTarget(IAIStimulusSource source)
    {
        if (source.SenseType != AISenseType.Hearing && source.SenseType != AISenseType.Custom)
        {
            return;
        }

        if (!_registeredTargets.Contains(source))
        {
            _registeredTargets.Add(source);
        }
    }

    /// <summary>
    /// 注销可感知目标
    /// </summary>
    /// <param name="source">刺激源</param>
    public void UnregisterTarget(IAIStimulusSource source)
    {
        _registeredTargets.Remove(source);
        _perceivedTargets.Remove(source);
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 检测目标是否可听见
    /// </summary>
    private bool IsTargetAudible(IAIStimulusSource target)
    {
        float distance = Vector3.Distance(OwnerPosition, target.Position);

        if (distance > _config.Range)
        {
            return false;
        }

        float attenuation = 1.0f / (1.0f + distance * distance * _config.Attenuation);
        float audibleStrength = target.Strength * attenuation;

        return audibleStrength >= _config.Threshold;
    }

    #endregion
}
