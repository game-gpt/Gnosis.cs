using Gnosis.ECS.Core;

namespace Gnosis.Network.Core;

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
