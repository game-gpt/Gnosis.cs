namespace Gnosis.Graphic.RHI;

/// <summary>
/// 交换链呈现模式
/// </summary>
public enum PresentMode
{
    Immediate = 0,
    Mailbox = 1,
    Fifo = 2,
    FifoRelaxed = 3
}
