namespace Gnosis.Network.Transport;

/// <summary>
/// 传输层工厂，提供创建不同类型传输实例的工厂方法
/// </summary>
public static class TransportFactory
{
    /// <summary>
    /// 创建 UDP 传输层实例
    /// </summary>
    public static UdpTransport CreateUdp()
    {
        return new UdpTransport();
    }

    /// <summary>
    /// 创建 WebSocket 传输层实例
    /// </summary>
    public static WebSocketTransport CreateWebSocket()
    {
        return new WebSocketTransport();
    }

    /// <summary>
    /// 创建 KCP 传输层实例
    /// </summary>
    /// <param name="mode">KCP 传输模式，默认为快速模式</param>
    public static KcpTransport CreateKcp(KcpMode mode = KcpMode.Fast)
    {
        return new KcpTransport(mode);
    }

    /// <summary>
    /// 根据传输类型名称创建传输层实例
    /// </summary>
    /// <param name="transportType">传输类型名称：udp、websocket、kcp</param>
    /// <returns>传输层实例</returns>
    /// <exception cref="ArgumentException">不支持的传输类型</exception>
    public static ITransport Create(string transportType)
    {
        return transportType.ToLowerInvariant() switch
        {
            "udp" => CreateUdp(),
            "websocket" => CreateWebSocket(),
            "kcp" => CreateKcp(),
            _ => throw new ArgumentException($"不支持的传输类型：{transportType}，支持的类型：udp、websocket、kcp")
        };
    }
}
