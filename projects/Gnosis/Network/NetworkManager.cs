using System;
using System.Collections.Generic;
using System.Linq;
using Gnosis.Core;

namespace Gnosis.Network;

/// <summary>
/// 网络管理器，提供大厅管理和消息收发功能
/// </summary>
public sealed class NetworkManager : INetworkManager
{
    #region 字段

    private readonly INetworkBackend _backend;
    private bool _isInitialized;
    private int _maxPlayers;
    private ConnectionState _connectionState;

    #endregion

    #region 属性

    /// <summary>
    /// 获取是否为服务器
    /// </summary>
    public bool IsServer { get; private set; }

    /// <summary>
    /// 获取是否为客户端
    /// </summary>
    public bool IsClient { get; private set; }

    /// <summary>
    /// 获取玩家数量
    /// </summary>
    public int PlayerCount { get; private set; }

    #endregion

    #region 事件

    /// <summary>
    /// 连接状态变化事件
    /// </summary>
    public event Action<ConnectionState>? OnConnectionStateChanged;

    /// <summary>
    /// 消息接收事件
    /// </summary>
    public event Action<INetworkMessage>? OnMessageReceived;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化网络管理器
    /// </summary>
    /// <param name="backend">网络后端实例</param>
    public NetworkManager(INetworkBackend backend)
    {
        _backend = backend;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 初始化网络连接
    /// </summary>
    public void Initialize()
    {
        _backend.Connect("localhost", 0);
        _isInitialized = true;
        SetConnectionState(ConnectionState.Connected);
    }

    /// <summary>
    /// 关闭网络连接并重置所有状态
    /// </summary>
    public void Shutdown()
    {
        SetConnectionState(ConnectionState.Disconnecting);
        _backend.Disconnect();
        IsServer = false;
        IsClient = false;
        PlayerCount = 0;
        _isInitialized = false;
        SetConnectionState(ConnectionState.Disconnected);
    }

    /// <summary>
    /// 创建大厅
    /// </summary>
    /// <param name="maxPlayers">最大玩家数</param>
    public void CreateLobby(int maxPlayers)
    {
        EnsureInitialized();
        IsServer = true;
        PlayerCount = 1;
        _maxPlayers = maxPlayers;
    }

    /// <summary>
    /// 加入指定大厅
    /// </summary>
    /// <param name="lobbyId">大厅标识</param>
    public void JoinLobby(string lobbyId)
    {
        EnsureInitialized();
        IsClient = true;
        PlayerCount = 1;
    }

    /// <summary>
    /// 离开当前大厅
    /// </summary>
    public void LeaveLobby()
    {
        EnsureInitialized();
        IsServer = false;
        IsClient = false;
        PlayerCount = 0;
    }

    /// <summary>
    /// 向服务器发送数据
    /// </summary>
    /// <param name="data">要发送的数据</param>
    /// <param name="reliable">是否使用可靠传输</param>
    public void SendToServer(byte[] data, bool reliable = false)
    {
        EnsureInitialized();
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
        EnsureInitialized();
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
        EnsureInitialized();
        var messages = _backend.Receive();
        foreach (var message in messages)
        {
            OnMessageReceived?.Invoke(message);
        }
        return messages;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 确保网络管理器已初始化
    /// </summary>
    /// <exception cref="InvalidOperationException">网络管理器未初始化时抛出</exception>
    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("网络管理器未初始化，请先调用 Initialize 方法");
        }
    }

    /// <summary>
    /// 设置连接状态并触发事件
    /// </summary>
    /// <param name="state">新的连接状态</param>
    private void SetConnectionState(ConnectionState state)
    {
        if (_connectionState == state)
        {
            return;
        }

        _connectionState = state;
        OnConnectionStateChanged?.Invoke(state);
    }

    #endregion
}
