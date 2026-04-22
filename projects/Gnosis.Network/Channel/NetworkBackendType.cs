using System;

namespace Gnosis.Network.Channel;

/// <summary>
/// 网络后端类型（已过时，请使用 TransportFactory 或直接构造具体传输实例替代）
/// </summary>
[Obsolete("请使用 TransportFactory 或直接构造具体传输实例替代。")]
public enum NetworkBackendType
{
    None,
    Steam,
    WebSocket,
    Custom
}
