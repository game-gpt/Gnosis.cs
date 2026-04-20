using Gnosis.Core.ValueObjects;

namespace Gnosis.Network;

public interface INetworkBackend
{
    void Connect(string address, int port);
    void Disconnect();
    
    void Send(byte[] data);
    void SendReliable(byte[] data);
    
    IEnumerable<INetworkMessage> Receive();
    
    bool IsConnected { get; }
    PlayerId LocalPlayerId { get; }
}
