namespace Gnosis.AI.Perception;

/// <summary>
/// 伤害感知配置
/// </summary>
public sealed class AIDamageConfig : IAISenseConfig
{
    /// <summary>
    /// 感知类型
    /// </summary>
    public AISenseType SenseType => AISenseType.Damage;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 感知范围（伤害感知无范围限制，始终为最大值）
    /// </summary>
    public float Range { get; set; } = float.MaxValue;

    /// <summary>
    /// 检测间隔（伤害感知为即时触发）
    /// </summary>
    public float Interval { get; set; } = 0.0f;
}
