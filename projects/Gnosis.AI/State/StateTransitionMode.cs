namespace Gnosis.AI.State;

/// <summary>
/// 状态过渡模式
/// </summary>
public enum StateTransitionMode : byte
{
    Immediate = 0,
    Smooth = 1,
    Conditional = 2
}
