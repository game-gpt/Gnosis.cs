namespace Gnosis.AI.Perception;

/// <summary>
/// 听觉感知实现，支持声音衰减和方向感知
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
    public float[] OwnerPosition { get; set; } = [0, 0, 0];

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

    /// <summary>
    /// 计算指定目标在当前感知者位置的感知强度
    /// </summary>
    /// <param name="target">目标刺激源</param>
    /// <returns>感知强度，0 表示不可感知</returns>
    public float ComputePerceivedStrength(IAIStimulusSource target)
    {
        float distance = ComputeDistance(OwnerPosition, target.Position);

        if (distance > _config.Range)
        {
            return 0f;
        }

        if (distance < 0.001f)
        {
            return target.Strength;
        }

        float attenuation = 1.0f / (1.0f + _config.AttenuationFactor * distance);

        return target.Strength * attenuation;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 检测目标是否可被听到
    /// </summary>
    private bool IsTargetAudible(IAIStimulusSource target)
    {
        float perceivedStrength = ComputePerceivedStrength(target);

        return perceivedStrength >= _config.MinSoundStrength;
    }

    /// <summary>
    /// 计算两点间距离
    /// </summary>
    private static float ComputeDistance(float[] a, float[] b)
    {
        if (a.Length < 3 || b.Length < 3)
        {
            return 0f;
        }

        float dx = a[0] - b[0];
        float dy = a[1] - b[1];
        float dz = a[2] - b[2];

        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    #endregion
}
