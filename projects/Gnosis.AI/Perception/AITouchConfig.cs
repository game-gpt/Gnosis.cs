namespace Gnosis.AI.Perception;

/// <summary>
/// 触觉感知配置
/// </summary>
public sealed class AITouchConfig : IAISenseConfig
{
    /// <summary>
    /// 感知类型
    /// </summary>
    public AISenseType SenseType => AISenseType.Touch;

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 感知范围（触觉半径）
    /// </summary>
    public float Range { get; set; } = 50.0f;

    /// <summary>
    /// 检测间隔
    /// </summary>
    public float Interval { get; set; } = 0.1f;
}
