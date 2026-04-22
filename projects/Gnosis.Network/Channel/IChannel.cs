namespace Gnosis.Network.Channel;

/// <summary>
/// 信道接口，提供按可靠性和有序性保证的数据传输
/// </summary>
public interface IChannel
{
    /// <summary>
    /// 获取信道标识
    /// </summary>
    ChannelId Id { get; }

    /// <summary>
    /// 获取信道类型
    /// </summary>
    ChannelType ChannelType { get; }

    /// <summary>
    /// 发送数据
    /// </summary>
    void Send(ReadOnlySpan<byte> data);

    /// <summary>
    /// 处理接收到的原始数据，返回按信道规则排序后的可投递消息
    /// </summary>
    IReadOnlyList<ReadOnlyMemory<byte>> ProcessIncoming(ReadOnlyMemory<byte> data);

    /// <summary>
    /// 处理待发送的原始数据，添加信道协议头
    /// </summary>
    ReadOnlyMemory<byte> ProcessOutgoing(ReadOnlySpan<byte> data);

    /// <summary>
    /// 更新信道状态（处理重传、ACK 超时等）
    /// </summary>
    void Update(TimeSpan deltaTime);
}
