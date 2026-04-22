using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Gnosis.Network.Channel;

namespace Gnosis.Network.Transport;

/// <summary>
/// WebSocket 连接，管理单个 WebSocket 连接的数据收发与信道管理
/// </summary>
public sealed class WebSocketConnection : ITransportConnection
{
    #region 字段

    private readonly WebSocket _webSocket;
    private readonly ConcurrentQueue<TransportEvent> _eventQueue = new();
    private readonly Dictionary<byte, IChannel> _channels = new();
    private readonly CancellationTokenSource _receiveCts = new();
    private Task? _receiveTask;
    private byte _nextChannelId;
    private bool _isConnected;
    private bool _isDisposed;

    #endregion

    #region 属性

    /// <summary>
    /// 获取连接标识
    /// </summary>
    public ConnectionId Id { get; }

    /// <summary>
    /// 获取连接是否活跃
    /// </summary>
    public bool IsConnected => _isConnected && !_isDisposed && _webSocket.State == WebSocketState.Open;

    #endregion

    #region 事件

    /// <summary>
    /// 连接断开时触发
    /// </summary>
    public event Action<ConnectionId>? OnDisconnected;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 WebSocket 连接
    /// </summary>
    public WebSocketConnection(ConnectionId id, WebSocket webSocket)
    {
        Id = id;
        _webSocket = webSocket;
        _nextChannelId = 1;
        _isConnected = webSocket.State == WebSocketState.Open;

        if (_isConnected)
        {
            _receiveTask = Task.Run(() => ReceiveLoop(_receiveCts.Token));
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 通过指定信道发送数据
    /// </summary>
    public void Send(ChannelId channelId, ReadOnlySpan<byte> data)
    {
        if (!_isConnected || _isDisposed || _webSocket.State != WebSocketState.Open)
        {
            return;
        }

        var framed = FrameWithChannelHeader(channelId, data);

        try
        {
            _webSocket.SendAsync(framed, WebSocketMessageType.Binary, true, _receiveCts.Token).GetAwaiter().GetResult();
        }
        catch (WebSocketException)
        {
            Disconnect();
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// 轮询接收网络事件
    /// </summary>
    public IReadOnlyList<TransportEvent> Poll()
    {
        var events = new List<TransportEvent>();

        while (_eventQueue.TryDequeue(out var evt))
        {
            events.Add(evt);
        }

        return events;
    }

    /// <summary>
    /// 创建指定类型的信道
    /// </summary>
    public ChannelId CreateChannel(ChannelType channelType)
    {
        var channelId = new ChannelId(_nextChannelId++);
        IChannel channel = channelType switch
        {
            ChannelType.ReliableOrdered => new ReliableChannel(channelId),
            ChannelType.UnreliableUnordered => new UnreliableChannel(channelId),
            _ => new UnreliableChannel(channelId)
        };

        _channels[channelId.Value] = channel;
        return channelId;
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        if (!_isConnected)
        {
            return;
        }

        _isConnected = false;
        _receiveCts.Cancel();

        try
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "断开连接", CancellationToken.None).GetAwaiter().GetResult();
            }
        }
        catch (WebSocketException)
        {
        }

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

        _eventQueue.Enqueue(TransportEvent.Disconnected(Id));
        OnDisconnected?.Invoke(Id);
    }

    /// <summary>
    /// 更新所有信道状态
    /// </summary>
    public void UpdateChannels(TimeSpan deltaTime)
    {
        foreach (var (_, channel) in _channels)
        {
            channel.Update(deltaTime);
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
        _receiveCts.Dispose();
        _webSocket.Dispose();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 接收数据循环
    /// </summary>
    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        while (!cancellationToken.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
        {
            try
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.Count > 0)
                {
                    var data = new byte[result.Count];
                    Array.Copy(buffer, data, result.Count);
                    ProcessReceivedData(data);
                }
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

        _isConnected = false;
    }

    /// <summary>
    /// 处理接收到的数据
    /// </summary>
    private void ProcessReceivedData(byte[] data)
    {
        if (data.Length < 1)
        {
            return;
        }

        var channelIdValue = data[0];
        var channelId = new ChannelId(channelIdValue);
        var payload = new ReadOnlyMemory<byte>(data, 1, data.Length - 1);

        if (_channels.TryGetValue(channelIdValue, out var channel))
        {
            var messages = channel.ProcessIncoming(payload);

            foreach (var message in messages)
            {
                _eventQueue.Enqueue(TransportEvent.DataReceived(Id, channelId, message));
            }
        }
        else
        {
            _eventQueue.Enqueue(TransportEvent.DataReceived(Id, channelId, payload));
        }
    }

    /// <summary>
    /// 封装数据帧，添加信道标识头
    /// </summary>
    private static byte[] FrameWithChannelHeader(ChannelId channelId, ReadOnlySpan<byte> data)
    {
        var framed = new byte[1 + data.Length];
        framed[0] = channelId.Value;
        data.CopyTo(framed.AsSpan(1));
        return framed;
    }

    #endregion
}
