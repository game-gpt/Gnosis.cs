using System;
using System.Collections.Generic;
using Gnosis.Network.Channel;
using Gnosis.Network.Serialization;
using Gnosis.Network.Transport;

namespace Gnosis.Network.RPC;

/// <summary>
/// RPC 路由器，负责将 RPC 调用路由到正确的目标连接
/// </summary>
public sealed class RpcRouter
{
    #region 字段

    private readonly RpcSystem _rpcSystem;
    private readonly Dictionary<ConnectionId, ITransportConnection> _connections = new();
    private readonly List<ConnectionId> _connectionList = [];
    private ChannelId _reliableChannel;
    private ChannelId _unreliableChannel;
    private bool _channelsInitialized;

    #endregion

    #region 属性

    /// <summary>
    /// 获取已连接的客户端数量
    /// </summary>
    public int ConnectionCount => _connections.Count;

    /// <summary>
    /// 获取所有已连接的连接标识
    /// </summary>
    public IReadOnlyList<ConnectionId> ConnectionIds => _connectionList;

    #endregion

    #region 事件

    /// <summary>
    /// RPC 消息发送成功时触发
    /// </summary>
    public event Action<ConnectionId, int>? OnRpcSent;

    /// <summary>
    /// RPC 消息发送失败时触发
    /// </summary>
    public event Action<ConnectionId, int, string>? OnRpcSendFailed;

    /// <summary>
    /// 收到 RPC 调用时触发
    /// </summary>
    public event Action<ConnectionId, RpcCall>? OnRpcReceived;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化 RPC 路由器
    /// </summary>
    /// <param name="rpcSystem">RPC 系统</param>
    public RpcRouter(RpcSystem rpcSystem)
    {
        _rpcSystem = rpcSystem ?? throw new ArgumentNullException(nameof(rpcSystem));
    }

    #endregion

    #region 公共方法 - 连接管理

    /// <summary>
    /// 注册连接到路由器
    /// </summary>
    /// <param name="connection">传输层连接</param>
    public void RegisterConnection(ITransportConnection connection)
    {
        if (connection is null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        if (!_channelsInitialized)
        {
            _reliableChannel = connection.CreateChannel(ChannelType.ReliableOrdered);
            _unreliableChannel = connection.CreateChannel(ChannelType.UnreliableUnordered);
            _channelsInitialized = true;
        }

        _connections[connection.Id] = connection;

        if (!_connectionList.Contains(connection.Id))
        {
            _connectionList.Add(connection.Id);
        }

        connection.OnDisconnected += id =>
        {
            _connections.Remove(id);
            _connectionList.Remove(id);
        };
    }

    /// <summary>
    /// 注销连接
    /// </summary>
    /// <param name="connectionId">连接标识</param>
    public void UnregisterConnection(ConnectionId connectionId)
    {
        _connections.Remove(connectionId);
        _connectionList.Remove(connectionId);
    }

    /// <summary>
    /// 获取指定连接
    /// </summary>
    /// <param name="connectionId">连接标识</param>
    /// <returns>传输层连接，不存在则返回 null</returns>
    public ITransportConnection? GetConnection(ConnectionId connectionId)
    {
        return _connections.GetValueOrDefault(connectionId);
    }

    #endregion

    #region 公共方法 - 路由

    /// <summary>
    /// 路由 RPC 调用到目标
    /// </summary>
    /// <param name="call">RPC 调用描述</param>
    /// <param name="reliable">是否使用可靠信道</param>
    public void Route(RpcCall call, bool reliable = true)
    {
        var data = _rpcSystem.SerializeCall(call);
        var channelId = reliable ? _reliableChannel : _unreliableChannel;

        switch (call.RpcType)
        {
            case RpcType.ServerRpc:
                SendToServer(data, channelId, call.MethodId);
                break;

            case RpcType.ClientRpc:
                SendToClient(call.TargetConnection, data, channelId, call.MethodId);
                break;

            case RpcType.MulticastRpc:
                SendToAllClients(data, channelId, call.MethodId);
                break;
        }
    }

    /// <summary>
    /// 轮询所有连接的 RPC 消息
    /// </summary>
    public void Poll()
    {
        foreach (var (_, connection) in _connections)
        {
            var events = connection.Poll();

            foreach (var evt in events)
            {
                if (evt.Type == TransportEventType.DataReceived && evt.Data.Length > 0)
                {
                    HandleIncomingData(evt.ConnectionId, evt.Data.Span);
                }
            }
        }
    }

    /// <summary>
    /// 发送所有待处理的 RPC 调用
    /// </summary>
    /// <param name="reliable">是否使用可靠信道</param>
    public void FlushPendingCalls(bool reliable = true)
    {
        var calls = _rpcSystem.DrainPendingCalls();

        foreach (var call in calls)
        {
            Route(call, reliable);
        }
    }

    /// <summary>
    /// 向服务器发送 RPC
    /// </summary>
    /// <param name="data">RPC 数据</param>
    /// <param name="channelId">信道标识</param>
    /// <param name="methodId">方法标识</param>
    private void SendToServer(byte[] data, ChannelId channelId, int methodId)
    {
        var firstConnection = GetFirstConnection();

        if (firstConnection is null)
        {
            OnRpcSendFailed?.Invoke(ConnectionId.Empty, methodId, "没有可用的服务器连接");
            return;
        }

        try
        {
            firstConnection.Send(channelId, data);
            OnRpcSent?.Invoke(firstConnection.Id, methodId);
        }
        catch (Exception ex)
        {
            OnRpcSendFailed?.Invoke(firstConnection.Id, methodId, $"发送 Server RPC 失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 向特定客户端发送 RPC
    /// </summary>
    /// <param name="targetConnection">目标连接标识</param>
    /// <param name="data">RPC 数据</param>
    /// <param name="channelId">信道标识</param>
    /// <param name="methodId">方法标识</param>
    private void SendToClient(ConnectionId targetConnection, byte[] data, ChannelId channelId, int methodId)
    {
        if (!_connections.TryGetValue(targetConnection, out var connection))
        {
            OnRpcSendFailed?.Invoke(targetConnection, methodId, $"目标连接 {targetConnection} 不存在");
            return;
        }

        try
        {
            connection.Send(channelId, data);
            OnRpcSent?.Invoke(targetConnection, methodId);
        }
        catch (Exception ex)
        {
            OnRpcSendFailed?.Invoke(targetConnection, methodId, $"发送 Client RPC 失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 向所有客户端广播 RPC
    /// </summary>
    /// <param name="data">RPC 数据</param>
    /// <param name="channelId">信道标识</param>
    /// <param name="methodId">方法标识</param>
    private void SendToAllClients(byte[] data, ChannelId channelId, int methodId)
    {
        foreach (var (connectionId, connection) in _connections)
        {
            try
            {
                connection.Send(channelId, data);
                OnRpcSent?.Invoke(connectionId, methodId);
            }
            catch (Exception ex)
            {
                OnRpcSendFailed?.Invoke(connectionId, methodId, $"发送 Multicast RPC 失败：{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 处理接收到的 RPC 数据
    /// </summary>
    /// <param name="senderId">发送者连接标识</param>
    /// <param name="data">RPC 数据</param>
    private void HandleIncomingData(ConnectionId senderId, ReadOnlySpan<byte> data)
    {
        try
        {
            var call = _rpcSystem.DeserializeCall(data);
            _rpcSystem.Execute(senderId, call);
            OnRpcReceived?.Invoke(senderId, call);
        }
        catch (Exception ex)
        {
            OnRpcSendFailed?.Invoke(senderId, -1, $"处理接收到的 RPC 数据失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 获取第一个可用连接
    /// </summary>
    private ITransportConnection? GetFirstConnection()
    {
        foreach (var (_, connection) in _connections)
        {
            if (connection.IsConnected)
            {
                return connection;
            }
        }

        return null;
    }

    #endregion
}
