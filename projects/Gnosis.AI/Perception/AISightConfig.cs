namespace Gnosis.AI.Perception;

/// <summary>
/// 视觉感知配置
/// </summary>
public sealed class AISightConfig : IAISenseConfig
{
    /// <summary>
    /// 感知类型
    /// </summary>
    public AISenseType SenseType => AISenseType.Sight;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 感知范围
    /// </summary>
    public float Range { get; set; } = 1000.0f;

    /// <summary>
    /// 检测间隔
    /// </summary>
    public float Interval { get; set; } = 0.2f;

    /// <summary>
    /// 视野角度（度数），0 表示全方向
    /// </summary>
    public float FieldOfView { get; set; } = 90.0f;

    /// <summary>
    /// 近距离感知范围，在此范围内无视视野角度限制
    /// </summary>
    public float NearClippingRadius { get; set; } = 100.0f;

    /// <summary>
    /// 是否启用视线检测
    /// </summary>
    public bool EnableLineOfSight { get; set; } = true;

    /// <summary>
    /// 视线检测高度偏移（相对于感知者位置）
    /// </summary>
    public float LineOfSightHeightOffset { get; set; } = 1.5f;
}
