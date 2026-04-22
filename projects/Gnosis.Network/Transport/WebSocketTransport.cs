using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Gnosis.Network.Channel;

namespace Gnosis.Network.Transport;

/// <summary>
/// WebSocket 传输层，支持客户端连接和服务器监听模式
/// </summary>
public sealed class WebSocketTransport : ITransport
{
    #region 字段

    private ClientWebSocket? _clientWebSocket;
    private HttpListener? _serverListener;
    private readonly List<WebSocketConnection> _serverConnections = [];
    private WebSocketConnection? _clientConnection;
    private CancellationTokenSource? _serverCts;
    private Task? _acceptTask;
    private TransportState _state;
    private ulong _nextConnectionId;
    private bool _isDisposed;
    private readonly object _lock = new();

    #endregion

    #region 属性

    /// <summary>
    /// 获取当前传输层状态
    /// </summary>
    public TransportState State => _state;

    /// <summary>
    /// 获取当前连接数量
    /// </summary>
    public int ConnectionCount
    {
        get
        {
            lock (_lock)
            {
                return _serverConnections.Count + (_clientConnection is not null ? 1 : 0);
            }
        }
    }

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
    /// 初始化 WebSocket 传输层
    /// </summary>
    public WebSocketTransport()
    {
        _state = TransportState.None;
        _nextConnectionId = 1;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 以客户端模式连接到 WebSocket 服务器
    /// </summary>
    public ITransportConnection Connect(string address, int port)
    {
        if (_state != TransportState.None && _state != TransportState.Disconnected)
        {
            throw new InvalidOperationException($"传输层状态 {_state} 不允许连接");
        }

        SetState(TransportState.Connecting);

        _clientWebSocket = new ClientWebSocket();
        var uri = new Uri($"ws://{address}:{port}/gnosis");
        _clientWebSocket.ConnectAsync(uri, CancellationToken.None).GetAwaiter().GetResult();

        var connectionId = new ConnectionId(_nextConnectionId++);
        _clientConnection = new WebSocketConnection(connectionId, _clientWebSocket);

        SetState(TransportState.Connected);

        return _clientConnection;
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

        _serverCts = new CancellationTokenSource();
        _serverListener = new HttpListener();
        _serverListener.Prefixes.Add($"http://+:{port}/gnosis/");
        _serverListener.Start();

        _acceptTask = Task.Run(() => AcceptLoop(_serverCts.Token));

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

        _serverCts?.Cancel();

        if (_clientConnection is not null)
        {
            _clientConnection.Disconnect();
            _clientConnection.Dispose();
            _clientConnection = null;
        }

        CloseServerConnections();

        try
        {
            _serverListener?.Stop();
        }
        catch (ObjectDisposedException)
        {
        }

        _serverListener = null;

        if (_acceptTask is not null)
        {
            try
            {
                _acceptTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
            }
        }

        _acceptTask = null;
        _serverCts?.Dispose();
        _serverCts = null;

        SetState(TransportState.Disconnected);
    }

    /// <summary>
    /// 更新所有连接的信道状态
    /// </summary>
    public void Update(TimeSpan deltaTime)
    {
        _clientConnection?.UpdateChannels(deltaTime);

        lock (_lock)
        {
            foreach (var connection in _serverConnections)
            {
                connection.UpdateChannels(deltaTime);
            }
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
    /// 服务器接受连接循环
    /// </summary>
    private async Task AcceptLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _serverListener?.IsListening == true)
        {
            try
            {
                var httpContext = await _serverListener.GetContextAsync();

                if (!httpContext.Request.IsWebSocketRequest)
                {
                    httpContext.Response.StatusCode = 400;
                    httpContext.Response.Close();
                    continue;
                }

                var webSocketContext = await httpContext.AcceptWebSocketAsync(null);
                var connectionId = new ConnectionId(_nextConnectionId++);
                var connection = new WebSocketConnection(connectionId, webSocketContext.WebSocket);

                lock (_lock)
                {
                    _serverConnections.Add(connection);
                }

                connection.OnDisconnected += id =>
                {
                    lock (_lock)
                    {
                        _serverConnections.Remove(connection);
                    }
                };

                OnConnectionReceived?.Invoke(connection);
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 关闭所有服务器客户端连接
    /// </summary>
    private void CloseServerConnections()
    {
        lock (_lock)
        {
            foreach (var connection in _serverConnections)
            {
                try
                {
                    connection.Disconnect();
                }
                catch (WebSocketException)
                {
                }
                finally
                {
                    connection.Dispose();
                }
            }

            _serverConnections.Clear();
        }
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
