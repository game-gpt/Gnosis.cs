namespace Gnosis.Network.Serialization;

/// <summary>
/// 消息类型
/// </summary>
public enum MessageType
{
    PlayerInput,
    ServerState,
    Rpc,
    Event,
    SyncHash,
    LobbyUpdate
}
