using System;
using System.Collections.Generic;
using Gnosis.Core;
using Gnosis.Core.Event;
using Gnosis.Network.Channel;
using Gnosis.Network.Serialization;
using Gnosis.Network.Transport;

namespace Gnosis.Network.RPC;

/// <summary>
/// 网络管理器，提供大厅管理和消息收发功能
/// </summary>
public sealed class NetworkManager : INetworkManager
{
    #region 字段

    private ITransport? _transport;
    private INetworkBackend? _backend;
    private NetworkMode _networkMode = NetworkMode.Offline;
    private int _playerCount;
    private string? _lobbyId;
    private int _maxPlayers;
    private readonly List<INetworkMessage> _pendingMessages = [];
    private readonly Dictionary<ConnectionId, ITransportConnection> _connections = new();
    private ITransportConnection? _clientConnection;
    private ChannelId _reliableChannel;
    private ChannelId _unreliableChannel;
    private bool _channelsInitialized;

    #endregion

    #region 属性

    /// <summary>
    /// 获取是否为服务器
    /// </summary>
    public bool IsServer => _networkMode is NetworkMode.Host or NetworkMode.Server;

    /// <summary>
    /// 获取是否为客户端
    /// </summary>
    public bool IsClient => _networkMode == NetworkMode.Client;

    /// <summary>
    /// 获取玩家数量
    /// </summary>
    public int PlayerCount => _playerCount;

    #endregion

    #region 事件

    /// <summary>
    /// 玩家加入时触发
    /// </summary>
    public event Action<PlayerId>? OnPlayerJoined;

    /// <summary>
    /// 玩家离开时触发
    /// </summary>
    public event Action<PlayerId>? OnPlayerLeft;

    /// <summary>
    /// 收到消息时触发
    /// </summary>
    public event Action<INetworkMessage>? OnMessageReceived;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用传输层实例初始化网络管理器
    /// </summary>
    /// <param name="transport">传输层实例</param>
    public NetworkManager(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _transport.OnConnectionReceived += OnTransportConnectionReceived;
        _transport.OnStateChanged += OnTransportStateChanged;
    }

    private void OnTransportConnectionReceived(ITransportConnection connection)
    {
        EnsureChannelsInitialized(connection);

        _connections[connection.Id] = connection;
        _playerCount = _connections.Count + (IsServer ? 1 : 0);

        connection.OnDisconnected += id =>
        {
            _connections.Remove(id);
            _playerCount = Math.Max(0, _connections.Count + (IsServer ? 1 : 0));
            OnPlayerLeft?.Invoke(default);
        };

        OnPlayerJoined?.Invoke(default);
    }

    private void OnTransportStateChanged(TransportState state)
    {
        if (state == TransportState.Disconnected)
        {
            _networkMode = NetworkMode.Offline;
            _connections.Clear();
            _clientConnection = null;
            _playerCount = 0;
        }
    }

    private void EnsureChannelsInitialized(ITransportConnection connection)
    {
        if (_channelsInitialized)
        {
            return;
        }

        _reliableChannel = connection.CreateChannel(ChannelType.ReliableOrdered);
        _unreliableChannel = connection.CreateChannel(ChannelType.UnreliableUnordered);
        _channelsInitialized = true;
    }

    /// <summary>
    /// 使用网络后端类型初始化网络管理器（已过时，请使用 ITransport 构造函数替代）
    /// </summary>
    /// <param name="backendType">网络后端类型</param>
    [Obsolete("请使用 NetworkManager(ITransport) 构造函数替代")]
    public NetworkManager(NetworkBackendType backendType = NetworkBackendType.None)
    {
#pragma warning disable CS0618
        _backend = backendType switch
        {
            NetworkBackendType.None => new NullNetworkBackend(),
            NetworkBackendType.WebSocket => new WebSocketBackend(),
            NetworkBackendType.Steam => new SteamNetworkBackend(),
            _ => throw new NotSupportedException($"不支持的网络后端类型：{backendType}")
        };
#pragma warning restore CS0618

        _backend.OnMessageReceived += message =>
        {
            _pendingMessages.Add(message);
            OnMessageReceived?.Invoke(message);
        };

        _networkMode = NetworkMode.Offline;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化网络管理器，根据传输层或后端类型创建对应实例
    /// </summary>
    public void Initialize()
    {
        if (_transport is not null)
        {
            _networkMode = NetworkMode.Offline;
            _connections.Clear();
            _clientConnection = null;
            _channelsInitialized = false;
            return;
        }

        if (_backend is not null)
        {
            _backend.OnMessageReceived += message =>
            {
                _pendingMessages.Add(message);
                OnMessageReceived?.Invoke(message);
            };
        }

        _networkMode = NetworkMode.Offline;
    }

    /// <summary>
    /// 关闭网络连接并重置所有状态
    /// </summary>
    public void Shutdown()
    {
        if (_transport is not null)
        {
            if (_transport.State == TransportState.Connected || _transport.State == TransportState.Listening)
            {
                _transport.Disconnect();
            }

            _pendingMessages.Clear();
            _networkMode = NetworkMode.Offline;
            _playerCount = 0;
            _lobbyId = null;
            return;
        }

        if (_backend is not null && _backend.IsConnected)
        {
            _backend.Disconnect();
        }

        _pendingMessages.Clear();
        _networkMode = NetworkMode.Offline;
        _playerCount = 0;
        _lobbyId = null;
        _backend = null;
    }

    /// <summary>
    /// 创建大厅
    /// </summary>
    /// <param name="maxPlayers">最大玩家数</param>
    public void CreateLobby(int maxPlayers)
    {
        if (_transport is not null)
        {
            _transport.Listen(0);
            _networkMode = NetworkMode.Host;
            _maxPlayers = maxPlayers;
            _playerCount = 1;
            _lobbyId = Guid.NewGuid().ToString("N")[..8];
            OnPlayerJoined?.Invoke(default);
            return;
        }

        if (_backend is null)
        {
            throw new InvalidOperationException("网络管理器未初始化");
        }

        _backend.Connect("localhost", 0);
        _networkMode = NetworkMode.Host;
        _maxPlayers = maxPlayers;
        _playerCount = 1;
        _lobbyId = Guid.NewGuid().ToString("N")[..8];
        OnPlayerJoined?.Invoke(_backend.LocalPlayerId);
    }

    /// <summary>
    /// 加入指定大厅
    /// </summary>
    /// <param name="lobbyId">大厅标识</param>
    public void JoinLobby(string lobbyId)
    {
        if (_transport is not null)
        {
            var connection = _transport.Connect("server", 0);
            EnsureChannelsInitialized(connection);
            _clientConnection = connection;
            _connections[connection.Id] = connection;
            _networkMode = NetworkMode.Client;
            _lobbyId = lobbyId;
            _playerCount = 1;

            connection.OnDisconnected += id =>
            {
                _connections.Remove(id);
                if (_clientConnection?.Id == id)
                {
                    _clientConnection = null;
                }
            };

            return;
        }

        if (_backend is null)
        {
            throw new InvalidOperationException("网络管理器未初始化");
        }

        _backend.Connect("server", 0);
        _networkMode = NetworkMode.Client;
        _lobbyId = lobbyId;
        _playerCount = 1;
    }

    /// <summary>
    /// 离开当前大厅
    /// </summary>
    public void LeaveLobby()
    {
        if (_transport is not null)
        {
            if (_transport.State == TransportState.Connected || _transport.State == TransportState.Listening)
            {
                _transport.Disconnect();
            }

            foreach (var (_, connection) in _connections)
            {
                connection.Dispose();
            }

            _connections.Clear();
            _clientConnection = null;
            _channelsInitialized = false;
            _networkMode = NetworkMode.Offline;
            _playerCount = 0;
            _lobbyId = null;
            return;
        }

        if (_backend is not null && _backend.IsConnected)
        {
            _backend.Disconnect();
        }

        _networkMode = NetworkMode.Offline;
        _playerCount = 0;
        _lobbyId = null;
    }

    /// <summary>
    /// 向服务器发送数据
    /// </summary>
    /// <param name="data">要发送的数据</param>
    /// <param name="reliable">是否使用可靠传输</param>
    public void SendToServer(byte[] data, bool reliable = false)
    {
        if (_transport is not null)
        {
            var connection = _clientConnection ?? GetFirstConnection();

            if (connection is null)
            {
                throw new InvalidOperationException("没有可用的服务器连接");
            }

            var channelId = reliable ? _reliableChannel : _unreliableChannel;
            connection.Send(channelId, data);
            return;
        }

        if (_backend is null)
        {
            throw new InvalidOperationException("网络管理器未初始化");
        }

        if (reliable)
        {
            _backend.SendReliable(data);
        }
        else
        {
            _backend.Send(data);
        }
    }

    /// <summary>
    /// 向所有客户端广播数据
    /// </summary>
    /// <param name="data">要发送的数据</param>
    /// <param name="reliable">是否使用可靠传输</param>
    public void SendToAll(byte[] data, bool reliable = false)
    {
        if (_transport is not null)
        {
            var channelId = reliable ? _reliableChannel : _unreliableChannel;

            foreach (var (_, connection) in _connections)
            {
                if (connection.IsConnected)
                {
                    connection.Send(channelId, data);
                }
            }

            return;
        }

        if (_backend is null)
        {
            throw new InvalidOperationException("网络管理器未初始化");
        }

        if (reliable)
        {
            _backend.SendReliable(data);
        }
        else
        {
            _backend.Send(data);
        }
    }

    /// <summary>
    /// 轮询接收到的网络消息
    /// </summary>
    /// <returns>接收到的消息集合</returns>
    public IEnumerable<INetworkMessage> PollMessages()
    {
        if (_transport is not null)
        {
            foreach (var (_, connection) in _connections)
            {
                var events = connection.Poll();

                foreach (var evt in events)
                {
                    if (evt.Type == TransportEventType.DataReceived && evt.Data.Length > 0)
                    {
                        var message = NetworkMessage.Create(
                            0,
                            default,
                            evt.Data.ToArray(),
                            evt.ChannelId == _reliableChannel
                        );
                        _pendingMessages.Add(message);
                        OnMessageReceived?.Invoke(message);
                    }
                }
            }

            var messages = _pendingMessages.ToList();
            _pendingMessages.Clear();
            return messages;
        }

        if (_backend is null)
        {
            return [];
        }

        var received = _backend.Receive();
        _pendingMessages.AddRange(received);

        var result = _pendingMessages.ToList();
        _pendingMessages.Clear();
        return result;
    }

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
