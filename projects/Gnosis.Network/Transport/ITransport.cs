namespace Gnosis.Network.Transport;

/// <summary>
/// 传输层接口，负责连接的建立、监听与生命周期管理
/// </summary>
public interface ITransport : IDisposable
{
    /// <summary>
    /// 以客户端模式连接到远程主机
    /// </summary>
    ITransportConnection Connect(string address, int port);

    /// <summary>
    /// 以服务器模式在指定端口监听连接
    /// </summary>
    void Listen(int port);

    /// <summary>
    /// 关闭传输层，释放所有资源
    /// </summary>
    void Disconnect();

    /// <summary>
    /// 获取当前传输层状态
    /// </summary>
    TransportState State { get; }

    /// <summary>
    /// 新连接到达时触发（服务器模式）
    /// </summary>
    event Action<ITransportConnection>? OnConnectionReceived;

    /// <summary>
    /// 传输层状态变化时触发
    /// </summary>
    event Action<TransportState>? OnStateChanged;
}
