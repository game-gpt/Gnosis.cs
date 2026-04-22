using System;
using Gnosis.Core;
using Gnosis.Core.Event;
using Gnosis.Network.Serialization;

namespace Gnosis.Network.Channel;

/// <summary>
/// 网络后端接口（已过时，请使用 ITransport 和 ITransportConnection 替代）
/// </summary>
[Obsolete("请使用 ITransport 和 ITransportConnection 替代。参见 Gnosis.Network.Transport 命名空间。")]
public interface INetworkBackend
{
    void Connect(string address, int port);
    void Disconnect();

    void Send(byte[] data);
    void SendReliable(byte[] data);

    IEnumerable<INetworkMessage> Receive();

    bool IsConnected { get; }
    PlayerId LocalPlayerId { get; }

    event Action<INetworkMessage>? OnMessageReceived;
}
