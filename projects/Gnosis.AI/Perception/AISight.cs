namespace Gnosis.AI.Perception;

/// <summary>
/// 视觉感知实现，支持视野锥检测和视线检测
/// </summary>
public sealed class AISight : IAISense
{
    #region 字段

    private readonly AISightConfig _config;
    private readonly List<IAIStimulusSource> _registeredTargets = new();
    private readonly List<IAIStimulusSource> _perceivedTargets = new();
    private float _timeSinceLastUpdate;

    #endregion

    #region 属性

    /// <summary>
    /// 感知类型
    /// </summary>
    public AISenseType SenseType => AISenseType.Sight;

    /// <summary>
    /// 当前感知到的目标列表
    /// </summary>
    public IReadOnlyList<IAIStimulusSource> PerceivedTargets => _perceivedTargets;

    /// <summary>
    /// 感知者位置
    /// </summary>
    public float[] OwnerPosition { get; set; } = [0, 0, 0];

    /// <summary>
    /// 感知者朝向（归一化方向向量）
    /// </summary>
    public float[] OwnerForward { get; set; } = [0, 0, 1];

    /// <summary>
    /// 视线检测回调，返回 true 表示无遮挡
    /// </summary>
    public Func<float[], float[], bool>? LineOfSightCheck { get; set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建视觉感知
    /// </summary>
    /// <param name="config">视觉感知配置</param>
    public AISight(AISightConfig config)
    {
        _config = config;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 更新视觉感知
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

            if (IsTargetVisible(target))
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
        if (source.SenseType != AISenseType.Sight && source.SenseType != AISenseType.Custom)
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
    /// 检测目标是否在视野内可见
    /// </summary>
    private bool IsTargetVisible(IAIStimulusSource target)
    {
        float distance = ComputeDistance(OwnerPosition, target.Position);

        if (distance > _config.Range)
        {
            return false;
        }

        if (distance <= _config.NearClippingRadius)
        {
            return CheckLineOfSight(target);
        }

        if (!IsInFieldOfView(target.Position))
        {
            return false;
        }

        return CheckLineOfSight(target);
    }

    /// <summary>
    /// 检测目标是否在视野锥内
    /// </summary>
    private bool IsInFieldOfView(float[] targetPosition)
    {
        if (_config.FieldOfView <= 0 || _config.FieldOfView >= 360)
        {
            return true;
        }

        if (OwnerForward.Length < 3 || OwnerPosition.Length < 3 || targetPosition.Length < 3)
        {
            return false;
        }

        float dx = targetPosition[0] - OwnerPosition[0];
        float dy = targetPosition[1] - OwnerPosition[1];
        float dz = targetPosition[2] - OwnerPosition[2];

        float lengthSq = dx * dx + dy * dy + dz * dz;
        if (lengthSq < 0.0001f)
        {
            return true;
        }

        float invLength = 1.0f / MathF.Sqrt(lengthSq);
        dx *= invLength;
        dy *= invLength;
        dz *= invLength;

        float dot = OwnerForward[0] * dx + OwnerForward[1] * dy + OwnerForward[2] * dz;

        float halfFovRad = _config.FieldOfView * 0.5f * MathF.PI / 180.0f;
        float cosHalfFov = MathF.Cos(halfFovRad);

        return dot >= cosHalfFov;
    }

    /// <summary>
    /// 检测视线是否被遮挡
    /// </summary>
    private bool CheckLineOfSight(IAIStimulusSource target)
    {
        if (!_config.EnableLineOfSight)
        {
            return true;
        }

        if (LineOfSightCheck is not null)
        {
            float[] eyePosition = new float[3];
            Array.Copy(OwnerPosition, eyePosition, Math.Min(3, OwnerPosition.Length));
            eyePosition[1] += _config.LineOfSightHeightOffset;

            return LineOfSightCheck(eyePosition, target.Position);
        }

        return true;
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
