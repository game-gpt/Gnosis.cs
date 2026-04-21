using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Gnosis.Core;

namespace Gnosis.Network;

/// <summary>
/// WebSocket 网络后端，支持客户端-服务器模式通信
/// </summary>
public sealed class WebSocketBackend : NetworkBackendBase
{
    #region 字段

    private ClientWebSocket? _clientWebSocket;
    private HttpListener? _serverListener;
    private readonly List<WebSocket> _serverConnections = new();
    private CancellationTokenSource? _serverCts;
    private readonly ConcurrentQueue<INetworkMessage> _receiveBuffer = new();
    private TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);
    private DateTime _lastHeartbeat = DateTime.UtcNow;
    private bool _isServerMode;
    private Task? _receiveTask;
    private Task? _acceptTask;
    private readonly object _lock = new();

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 WebSocket 网络后端
    /// </summary>
    public WebSocketBackend()
    {
    }

    #endregion

    #region 重写方法

    /// <summary>
    /// 连接到 WebSocket 服务器或启动服务器监听
    /// </summary>
    /// <param name="address">服务器地址</param>
    /// <param name="port">服务器端口，0 表示服务器模式</param>
    protected override void OnConnect(string address, int port)
    {
        SetLocalPlayerId(PlayerId.New());

        if (port > 0)
        {
            ConnectAsClient(address, port);
        }
        else
        {
            StartServer(address);
        }
    }

    /// <summary>
    /// 断开 WebSocket 连接并清理资源
    /// </summary>
    protected override void OnDisconnect()
    {
        _serverCts?.Cancel();

        if (_clientWebSocket is not null)
        {
            CloseClientWebSocket();
        }

        CloseServerConnections();
        StopServerListener();
        WaitForReceiveTask();

        _serverCts?.Dispose();
        _serverCts = null;
    }

    /// <summary>
    /// 通过 WebSocket 发送不可靠消息
    /// </summary>
    /// <param name="data">待发送的数据</param>
    protected override void SendCore(byte[] data)
    {
        if (_isServerMode)
        {
            BroadcastToClients(data, WebSocketMessageType.Binary);
        }
        else
        {
            SendToServer(data, WebSocketMessageType.Binary);
        }
    }

    /// <summary>
    /// 通过 WebSocket 发送可靠消息（WebSocket 基于 TCP，等同于普通发送）
    /// </summary>
    /// <param name="data">待发送的数据</param>
    protected override void SendReliableCore(byte[] data)
    {
        SendCore(data);
    }

    /// <summary>
    /// 从接收缓冲区取出所有消息
    /// </summary>
    /// <returns>接收到的消息集合</returns>
    protected override IEnumerable<INetworkMessage> ReceiveCore()
    {
        var messages = new List<INetworkMessage>();

        while (_receiveBuffer.TryDequeue(out var message))
        {
            messages.Add(message);
        }

        return messages;
    }

    #endregion

    #region 客户端方法

    /// <summary>
    /// 以客户端模式连接到 WebSocket 服务器
    /// </summary>
    /// <param name="address">服务器地址</param>
    /// <param name="port">服务器端口</param>
    private void ConnectAsClient(string address, int port)
    {
        _isServerMode = false;
        _serverCts = new CancellationTokenSource();
        _clientWebSocket = new ClientWebSocket();

        var uri = new Uri($"ws://{address}:{port}/gnosis");
        _clientWebSocket.ConnectAsync(uri, _serverCts.Token).GetAwaiter().GetResult();

        _lastHeartbeat = DateTime.UtcNow;
        _receiveTask = Task.Run(() => ReceiveLoop(_serverCts.Token));
    }

    /// <summary>
    /// 客户端接收循环
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        while (!cancellationToken.IsCancellationRequested && _clientWebSocket?.State == WebSocketState.Open)
        {
            try
            {
                var result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.Count > 0)
                {
                    var data = new byte[result.Count];
                    Array.Copy(buffer, data, result.Count);
                    var message = NetworkMessage.Create((int)MessageType.Event, LocalPlayerId, data);
                    _receiveBuffer.Enqueue(message);
                }

                SendHeartbeatIfNeeded();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (WebSocketException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 客户端发送数据到服务器
    /// </summary>
    /// <param name="data">待发送的数据</param>
    /// <param name="messageType">WebSocket 消息类型</param>
    private void SendToServer(byte[] data, WebSocketMessageType messageType)
    {
        if (_clientWebSocket?.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            _clientWebSocket.SendAsync(new ArraySegment<byte>(data), messageType, true, _serverCts?.Token ?? CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (WebSocketException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// 关闭客户端 WebSocket 连接
    /// </summary>
    private void CloseClientWebSocket()
    {
        try
        {
            if (_clientWebSocket?.State == WebSocketState.Open)
            {
                _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "断开连接", CancellationToken.None).GetAwaiter().GetResult();
            }
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            _clientWebSocket?.Dispose();
            _clientWebSocket = null;
        }
    }

    #endregion

    #region 服务器方法

    /// <summary>
    /// 启动 WebSocket 服务器
    /// </summary>
    /// <param name="address">监听地址</param>
    private void StartServer(string address)
    {
        _isServerMode = true;
        _serverCts = new CancellationTokenSource();
        _serverListener = new HttpListener();
        _serverListener.Prefixes.Add($"http://{address}:0/gnosis/");
        _serverListener.Start();

        _acceptTask = Task.Run(() => AcceptLoop(_serverCts.Token));
    }

    /// <summary>
    /// 服务器接受连接循环
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
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
                var webSocket = webSocketContext.WebSocket;

                lock (_lock)
                {
                    _serverConnections.Add(webSocket);
                }

                _ = Task.Run(() => HandleClientConnection(webSocket, cancellationToken), cancellationToken);
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
    /// 处理客户端连接的接收循环
    /// </summary>
    /// <param name="webSocket">客户端 WebSocket 连接</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task HandleClientConnection(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.Count > 0)
                {
                    var data = new byte[result.Count];
                    Array.Copy(buffer, data, result.Count);
                    var message = NetworkMessage.Create((int)MessageType.Event, LocalPlayerId, data);
                    _receiveBuffer.Enqueue(message);
                }
            }
            catch (WebSocketException)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        lock (_lock)
        {
            _serverConnections.Remove(webSocket);
        }

        try
        {
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "连接关闭", CancellationToken.None);
            }
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            webSocket.Dispose();
        }
    }

    /// <summary>
    /// 服务器广播数据到所有客户端
    /// </summary>
    /// <param name="data">待广播的数据</param>
    /// <param name="messageType">WebSocket 消息类型</param>
    private void BroadcastToClients(byte[] data, WebSocketMessageType messageType)
    {
        List<WebSocket> connections;

        lock (_lock)
        {
            connections = new List<WebSocket>(_serverConnections);
        }

        foreach (var connection in connections)
        {
            if (connection.State != WebSocketState.Open)
            {
                continue;
            }

            try
            {
                connection.SendAsync(new ArraySegment<byte>(data), messageType, true, _serverCts?.Token ?? CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (WebSocketException)
            {
            }
            catch (OperationCanceledException)
            {
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
                    if (connection.State == WebSocketState.Open)
                    {
                        connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "服务器关闭", CancellationToken.None).GetAwaiter().GetResult();
                    }
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
    /// 停止服务器监听器
    /// </summary>
    private void StopServerListener()
    {
        try
        {
            _serverListener?.Stop();
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            _serverListener = null;
        }
    }

    #endregion

    #region 心跳机制

    /// <summary>
    /// 检查并发送心跳消息
    /// </summary>
    private void SendHeartbeatIfNeeded()
    {
        if (DateTime.UtcNow - _lastHeartbeat < _heartbeatInterval)
        {
            return;
        }

        _lastHeartbeat = DateTime.UtcNow;

        if (_isServerMode)
        {
            BroadcastToClients(Array.Empty<byte>(), WebSocketMessageType.Binary);
        }
        else
        {
            SendToServer(Array.Empty<byte>(), WebSocketMessageType.Binary);
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 等待接收任务完成
    /// </summary>
    private void WaitForReceiveTask()
    {
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

        _receiveTask = null;
        _acceptTask = null;
    }

    #endregion
}
