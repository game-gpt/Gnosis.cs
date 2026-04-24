namespace Gnosis.AI.Perception;

/// <summary>
/// 感知记忆条目
/// </summary>
public struct PerceptionMemoryEntry
{
    /// <summary>
    /// 目标刺激源
    /// </summary>
    public IAIStimulusSource Target { get; init; }

    /// <summary>
    /// 最后已知位置
    /// </summary>
    public Vector3 LastKnownPosition { get; init; }

    /// <summary>
    /// 最后感知时间
    /// </summary>
    public float LastPerceivedTime { get; init; }

    /// <summary>
    /// 感知强度
    /// </summary>
    public float Strength { get; init; }

    /// <summary>
    /// 是否已过期
    /// </summary>
    public bool IsExpired => Strength <= 0;
}
