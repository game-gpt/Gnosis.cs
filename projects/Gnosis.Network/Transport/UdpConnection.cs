using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Gnosis.Network.Channel;

namespace Gnosis.Network.Transport;

/// <summary>
/// UDP 连接，管理单个 UDP 端点的数据收发与信道管理
/// </summary>
public sealed class UdpConnection : ITransportConnection
{
    #region 字段

    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _remoteEndPoint;
    private readonly ConcurrentQueue<TransportEvent> _eventQueue = new();
    private readonly Dictionary<byte, IChannel> _channels = new();
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
    /// 获取远程端点
    /// </summary>
    public IPEndPoint RemoteEndPoint => _remoteEndPoint;

    /// <summary>
    /// 获取连接是否活跃
    /// </summary>
    public bool IsConnected => _isConnected && !_isDisposed;

    #endregion

    #region 事件

    /// <summary>
    /// 连接断开时触发
    /// </summary>
    public event Action<ConnectionId>? OnDisconnected;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 UDP 连接
    /// </summary>
    public UdpConnection(ConnectionId id, UdpClient udpClient, IPEndPoint remoteEndPoint)
    {
        Id = id;
        _udpClient = udpClient;
        _remoteEndPoint = remoteEndPoint;
        _nextChannelId = 1;
        _isConnected = true;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 通过指定信道发送数据
    /// </summary>
    public void Send(ChannelId channelId, ReadOnlySpan<byte> data)
    {
        if (!_isConnected || _isDisposed)
        {
            return;
        }

        var framed = FrameWithChannelHeader(channelId, data);

        try
        {
            _udpClient.Send(framed, framed.Length, _remoteEndPoint);
        }
        catch (SocketException)
        {
            Disconnect();
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
        _eventQueue.Enqueue(TransportEvent.Disconnected(Id));
        OnDisconnected?.Invoke(Id);
    }

    /// <summary>
    /// 处理接收到的 UDP 数据报
    /// </summary>
    public void OnDataReceived(ReadOnlyMemory<byte> data)
    {
        if (!_isConnected || _isDisposed)
        {
            return;
        }

        if (data.Length < 1)
        {
            return;
        }

        var channelIdValue = data.Span[0];
        var channelId = new ChannelId(channelIdValue);
        var payload = data[1..];

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
        _isConnected = false;
        _channels.Clear();
        _eventQueue.Clear();
    }

    #endregion

    #region 私有方法

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
