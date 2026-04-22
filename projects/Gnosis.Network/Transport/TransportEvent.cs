using Gnosis.Network.Channel;

namespace Gnosis.Network.Transport;

/// <summary>
/// 传输层事件类型
/// </summary>
public enum TransportEventType : byte
{
    /// <summary>
    /// 数据接收
    /// </summary>
    DataReceived,

    /// <summary>
    /// 连接建立
    /// </summary>
    Connected,

    /// <summary>
    /// 连接断开
    /// </summary>
    Disconnected
}

/// <summary>
/// 传输层事件，表示一次网络事件（数据接收、连接、断开）
/// </summary>
public readonly record struct TransportEvent
{
    /// <summary>
    /// 事件类型
    /// </summary>
    public TransportEventType Type { get; init; }

    /// <summary>
    /// 连接标识
    /// </summary>
    public ConnectionId ConnectionId { get; init; }

    /// <summary>
    /// 信道标识
    /// </summary>
    public ChannelId ChannelId { get; init; }

    /// <summary>
    /// 事件数据
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; init; }

    /// <summary>
    /// 创建数据接收事件
    /// </summary>
    public static TransportEvent DataReceived(ConnectionId connectionId, ChannelId channelId, ReadOnlyMemory<byte> data)
    {
        return new TransportEvent
        {
            Type = TransportEventType.DataReceived,
            ConnectionId = connectionId,
            ChannelId = channelId,
            Data = data
        };
    }

    /// <summary>
    /// 创建连接建立事件
    /// </summary>
    public static TransportEvent Connected(ConnectionId connectionId)
    {
        return new TransportEvent
        {
            Type = TransportEventType.Connected,
            ConnectionId = connectionId
        };
    }

    /// <summary>
    /// 创建连接断开事件
    /// </summary>
    public static TransportEvent Disconnected(ConnectionId connectionId)
    {
        return new TransportEvent
        {
            Type = TransportEventType.Disconnected,
            ConnectionId = connectionId
        };
    }
}
