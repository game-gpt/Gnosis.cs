using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Gnosis.Network.Channel;

namespace Gnosis.Network.Transport;

/// <summary>
/// KCP 传输层，基于 UDP 的可靠传输，延迟低于 TCP 重传
/// </summary>
public sealed class KcpTransport : ITransport
{
    #region 字段

    private UdpClient? _udpClient;
    private readonly ConcurrentDictionary<ConnectionId, KcpConnection> _connections = new();
    private CancellationTokenSource? _cts;
    private Task? _receiveTask;
    private TransportState _state;
    private ulong _nextConnectionId;
    private bool _isDisposed;
    private readonly KcpMode _kcpMode;

    #endregion

    #region 属性

    /// <summary>
    /// 获取当前传输层状态
    /// </summary>
    public TransportState State => _state;

    /// <summary>
    /// 获取 KCP 模式
    /// </summary>
    public KcpMode Mode => _kcpMode;

    /// <summary>
    /// 获取当前连接数量
    /// </summary>
    public int ConnectionCount => _connections.Count;

    #endregion

    #region 事件

    /// <summary>
    /// 新连接到达时触发（服务器模式）
    /// </summary>
    public event Action<ITransportConnection>? OnConnectionReceived;

    /// <summary>
    /// 传输层状态变化时触发
    /// </summary>
    public event Action<TransportState>? OnStateChanged;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 KCP 传输层
    /// </summary>
    public KcpTransport(KcpMode mode = KcpMode.Fast)
    {
        _state = TransportState.None;
        _nextConnectionId = 1;
        _kcpMode = mode;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 以客户端模式连接到远程主机
    /// </summary>
    public ITransportConnection Connect(string address, int port)
    {
        if (_state != TransportState.None && _state != TransportState.Disconnected)
        {
            throw new InvalidOperationException($"传输层状态 {_state} 不允许连接");
        }

        SetState(TransportState.Connecting);

        _udpClient = new UdpClient();
        var remoteEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
        _udpClient.Connect(remoteEndPoint);

        var connectionId = new ConnectionId(_nextConnectionId++);
        var connection = new KcpConnection(connectionId, _udpClient, remoteEndPoint, _kcpMode);
        _connections[connectionId] = connection;

        _cts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));

        SetState(TransportState.Connected);

        return connection;
    }

    /// <summary>
    /// 以服务器模式在指定端口监听连接
    /// </summary>
    public void Listen(int port)
    {
        if (_state != TransportState.None && _state != TransportState.Disconnected)
        {
            throw new InvalidOperationException($"传输层状态 {_state} 不允许监听");
        }

        _udpClient = new UdpClient(port);
        _cts = new CancellationTokenSource();
        _receiveTask = Task.Run(() => ReceiveLoop(_cts.Token));

        SetState(TransportState.Listening);
    }

    /// <summary>
    /// 关闭传输层，释放所有资源
    /// </summary>
    public void Disconnect()
    {
        if (_state == TransportState.None || _state == TransportState.Disconnected)
        {
            return;
        }

        SetState(TransportState.Disconnecting);

        _cts?.Cancel();

        foreach (var (_, connection) in _connections)
        {
            connection.Disconnect();
            connection.Dispose();
        }

        _connections.Clear();

        try
        {
            _udpClient?.Close();
        }
        catch (SocketException)
        {
        }

        _udpClient = null;

        if (_receiveTask is not null)
        {
            try
            {
                _receiveTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
            }
        }

        _receiveTask = null;
        _cts?.Dispose();
        _cts = null;

        SetState(TransportState.Disconnected);
    }

    /// <summary>
    /// 获取指定连接
    /// </summary>
    public KcpConnection? GetConnection(ConnectionId connectionId)
    {
        return _connections.GetValueOrDefault(connectionId);
    }

    /// <summary>
    /// 更新所有连接的信道状态
    /// </summary>
    public void Update(TimeSpan deltaTime)
    {
        foreach (var (_, connection) in _connections)
        {
            connection.UpdateChannels(deltaTime);
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Disconnect();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 接收数据循环
    /// </summary>
    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _udpClient is not null)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(cancellationToken);
                var remoteEndPoint = result.RemoteEndPoint;
                var data = result.Buffer;

                var connection = FindOrCreateConnection(remoteEndPoint);

                if (connection is not null)
                {
                    connection.OnDataReceived(data);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 查找或创建连接
    /// </summary>
    private KcpConnection? FindOrCreateConnection(IPEndPoint remoteEndPoint)
    {
        foreach (var (_, conn) in _connections)
        {
            if (conn.RemoteEndPoint.Equals(remoteEndPoint))
            {
                return conn;
            }
        }

        if (_state != TransportState.Listening)
        {
            return null;
        }

        var connectionId = new ConnectionId(_nextConnectionId++);
        var connection = new KcpConnection(connectionId, _udpClient!, remoteEndPoint, _kcpMode);
        _connections[connectionId] = connection;

        OnConnectionReceived?.Invoke(connection);

        return connection;
    }

    /// <summary>
    /// 设置传输层状态
    /// </summary>
    private void SetState(TransportState newState)
    {
        _state = newState;
        OnStateChanged?.Invoke(newState);
    }

    #endregion
}
