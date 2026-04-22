using Gnosis.Network.Serialization;

namespace Gnosis.Network.RPC;

/// <summary>
/// 网络管理器接口
/// </summary>
public interface INetworkManager
{
    void Initialize();
    void Shutdown();
    
    void CreateLobby(int maxPlayers);
    void JoinLobby(string lobbyId);
    void LeaveLobby();
    
    void SendToServer(byte[] data, bool reliable = false);
    void SendToAll(byte[] data, bool reliable = false);
    
    IEnumerable<INetworkMessage> PollMessages();
    
    bool IsServer { get; }
    bool IsClient { get; }
    int PlayerCount { get; }
}
