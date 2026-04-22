namespace Gnosis.Neural.Inference;

/// <summary>
/// 推理优先级
/// </summary>
public enum InferencePriority : byte
{
    Low = 0,
    Normal = 1,
    High = 2,
    Realtime = 3
}
