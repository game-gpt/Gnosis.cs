namespace Gnosis.Network.Channel;

/// <summary>
/// 连接状态（已过时，请使用 TransportState 替代）
/// </summary>
[Obsolete("请使用 TransportState 替代")]
public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Disconnecting
}
