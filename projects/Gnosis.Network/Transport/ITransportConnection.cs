using Gnosis.Network.Channel;

namespace Gnosis.Network.Transport;

/// <summary>
/// 传输层连接接口，负责数据的收发与信道管理
/// </summary>
public interface ITransportConnection : IDisposable
{
    /// <summary>
    /// 获取连接标识
    /// </summary>
    ConnectionId Id { get; }

    /// <summary>
    /// 获取连接是否活跃
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 通过指定信道发送数据
    /// </summary>
    void Send(ChannelId channelId, ReadOnlySpan<byte> data);

    /// <summary>
    /// 轮询接收网络事件
    /// </summary>
    IReadOnlyList<TransportEvent> Poll();

    /// <summary>
    /// 创建指定类型的信道
    /// </summary>
    ChannelId CreateChannel(ChannelType channelType);

    /// <summary>
    /// 断开连接
    /// </summary>
    void Disconnect();

    /// <summary>
    /// 连接断开时触发
    /// </summary>
    event Action<ConnectionId>? OnDisconnected;
}
