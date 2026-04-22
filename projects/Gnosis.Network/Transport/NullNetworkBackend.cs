using System;
using Gnosis.Core;
using Gnosis.Core.Event;
using Gnosis.Network.Channel;
using Gnosis.Network.Serialization;

namespace Gnosis.Network.Transport;

/// <summary>
/// 空网络后端，用于不需要实际网络通信的场景，所有操作均为空操作（已过时，请使用 UdpTransport 或 WebSocketTransport 替代）
/// </summary>
[Obsolete("请使用 UdpTransport 或 WebSocketTransport 替代")]
public sealed class NullNetworkBackend : NetworkBackendBase
{
    /// <summary>
    /// 连接时设置本地玩家 ID。
    /// </summary>
    /// <param name="address">目标地址。</param>
    /// <param name="port">目标端口。</param>
    protected override void OnConnect(string address, int port)
    {
        SetLocalPlayerId(PlayerId.New());
    }

    /// <summary>
    /// 断开连接时执行空操作。
    /// </summary>
    protected override void OnDisconnect()
    {
    }

    /// <summary>
    /// 发送数据时静默丢弃消息。
    /// </summary>
    /// <param name="data">要发送的数据。</param>
    protected override void SendCore(byte[] data)
    {
    }

    /// <summary>
    /// 可靠发送数据时静默丢弃消息。
    /// </summary>
    /// <param name="data">要发送的数据。</param>
    protected override void SendReliableCore(byte[] data)
    {
    }

    /// <summary>
    /// 接收消息时返回空集合。
    /// </summary>
    /// <returns>空的消息集合。</returns>
    protected override IEnumerable<INetworkMessage> ReceiveCore()
    {
        return [];
    }
}
