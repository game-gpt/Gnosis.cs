namespace Gnosis.AI.Perception;

/// <summary>
/// 触觉感知实现，基于距离检测接触
/// </summary>
public sealed class AITouch : IAISense
{
    #region 字段

    private readonly AITouchConfig _config;
    private readonly List<IAIStimulusSource> _registeredTargets = new();
    private readonly List<IAIStimulusSource> _perceivedTargets = new();
    private float _timeSinceLastUpdate;

    #endregion

    #region 属性

    /// <summary>
    /// 感知类型
    /// </summary>
    public AISenseType SenseType => AISenseType.Touch;

    /// <summary>
    /// 当前感知到的目标列表
    /// </summary>
    public IReadOnlyList<IAIStimulusSource> PerceivedTargets => _perceivedTargets;

    /// <summary>
    /// 感知者位置
    /// </summary>
    public float[] OwnerPosition { get; set; } = [0, 0, 0];

    /// <summary>
    /// 感知者碰撞半径
    /// </summary>
    public float OwnerRadius { get; set; } = 50.0f;

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建触觉感知
    /// </summary>
    /// <param name="config">触觉感知配置</param>
    public AITouch(AITouchConfig config)
    {
        _config = config;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 更新触觉感知
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

            if (IsTargetTouching(target))
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
        if (source.SenseType != AISenseType.Touch && source.SenseType != AISenseType.Custom)
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
    /// 检测目标是否与感知者接触
    /// </summary>
    private bool IsTargetTouching(IAIStimulusSource target)
    {
        float distance = ComputeDistance(OwnerPosition, target.Position);
        float touchRadius = OwnerRadius + _config.Range;

        if (target is AIStimulusSource concreteTarget)
        {
            touchRadius += concreteTarget.Radius;
        }

        return distance <= touchRadius;
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
