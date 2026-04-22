namespace Gnosis.AI.Perception;

/// <summary>
/// 听觉感知配置
/// </summary>
public sealed class AIHearingConfig : IAISenseConfig
{
    /// <summary>
    /// 感知类型
    /// </summary>
    public AISenseType SenseType => AISenseType.Hearing;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 感知范围
    /// </summary>
    public float Range { get; set; } = 600.0f;

    /// <summary>
    /// 检测间隔
    /// </summary>
    public float Interval { get; set; } = 0.3f;

    /// <summary>
    /// 最小可感知声音强度阈值
    /// </summary>
    public float MinSoundStrength { get; set; } = 0.1f;

    /// <summary>
    /// 声音衰减系数
    /// </summary>
    public float AttenuationFactor { get; set; } = 1.0f;

    /// <summary>
    /// 是否启用方向感知（能否判断声音来源方向）
    /// </summary>
    public bool EnableDirectionalHearing { get; set; } = true;
}
