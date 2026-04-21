using Gnosis.ECS.Core;
using Gnosis.Network.Backends;

namespace Gnosis.Network.Core;

/// <summary>
/// 网络管理器，提供大厅管理和消息收发功能
/// </summary>
public sealed class NetworkManager : INetworkManager
{
    #region 字段

    private INetworkBackend? _backend;
    private NetworkBackendType _backendType;
    private NetworkMode _networkMode = NetworkMode.Offline;
    private int _playerCount;
    private string? _lobbyId;
    private int _maxPlayers;
    private readonly List<INetworkMessage> _pendingMessages = new();

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
    /// 初始化网络管理器
    /// </summary>
    /// <param name="backendType">网络后端类型</param>
    public NetworkManager(NetworkBackendType backendType = NetworkBackendType.None)
    {
        _backendType = backendType;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化网络管理器，根据后端类型创建对应后端实例
    /// </summary>
    public void Initialize()
    {
        _backend = _backendType switch
        {
            NetworkBackendType.None => new NullNetworkBackend(),
            NetworkBackendType.WebSocket => new WebSocketBackend(),
            NetworkBackendType.Steam => new SteamNetworkBackend(),
            _ => throw new NotSupportedException($"不支持的网络后端类型：{_backendType}")
        };

        _backend.OnMessageReceived += message =>
        {
            _pendingMessages.Add(message);
            OnMessageReceived?.Invoke(message);
        };

        _networkMode = NetworkMode.Offline;
    }

    /// <summary>
    /// 关闭网络连接并重置所有状态
    /// </summary>
    public void Shutdown()
    {
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
        if (_backend is null)
        {
            return Enumerable.Empty<INetworkMessage>();
        }

        var received = _backend.Receive();
        _pendingMessages.AddRange(received);

        var messages = _pendingMessages.ToList();
        _pendingMessages.Clear();
        return messages;
    }

    #endregion
}
