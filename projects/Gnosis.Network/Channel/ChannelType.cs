namespace Gnosis.Network.Channel;

/// <summary>
/// 信道类型，定义数据传输的可靠性和有序性保证
/// </summary>
public enum ChannelType : byte
{
    /// <summary>
    /// 可靠有序：保证消息可靠且按序到达，适用于 RPC、状态同步
    /// </summary>
    ReliableOrdered = 0,

    /// <summary>
    /// 可靠无序：保证消息可靠但不保证顺序，适用于独立状态更新
    /// </summary>
    ReliableUnordered = 1,

    /// <summary>
    /// 不可靠有序：不保证可靠性但保证顺序，适用于输入帧
    /// </summary>
    UnreliableOrdered = 2,

    /// <summary>
    /// 不可靠无序：不保证可靠性和有序性，适用于语音、动画
    /// </summary>
    UnreliableUnordered = 3
}
