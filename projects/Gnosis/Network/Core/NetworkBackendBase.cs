using Gnosis.ECS.Core;

namespace Gnosis.Network.Core;

/// <summary>
/// 网络后端基类，提供连接状态管理和模板方法模式
/// </summary>
public abstract class NetworkBackendBase : INetworkBackend
{
    #region 字段

    private ConnectionState _connectionState = ConnectionState.Disconnected;
    private PlayerId _localPlayerId = PlayerId.Empty;

    #endregion

    #region 属性

    /// <summary>
    /// 当前连接状态
    /// </summary>
    public ConnectionState ConnectionState => _connectionState;

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => ConnectionState == ConnectionState.Connected;

    /// <summary>
    /// 本地玩家标识
    /// </summary>
    public PlayerId LocalPlayerId => _localPlayerId;

    #endregion

    #region 事件

    /// <summary>
    /// 连接成功时触发
    /// </summary>
    public event Action? OnConnected;

    /// <summary>
    /// 断开连接时触发
    /// </summary>
    public event Action? OnDisconnected;

    /// <summary>
    /// 收到消息时触发
    /// </summary>
    public event Action<INetworkMessage>? OnMessageReceived;

    #endregion

    #region 抽象方法

    /// <summary>
    /// 子类重写不可靠发送逻辑
    /// </summary>
    /// <param name="data">待发送的数据</param>
    protected abstract void SendCore(byte[] data);

    /// <summary>
    /// 子类重写可靠发送逻辑
    /// </summary>
    /// <param name="data">待发送的数据</param>
    protected abstract void SendReliableCore(byte[] data);

    /// <summary>
    /// 子类重写消息接收逻辑
    /// </summary>
    /// <returns>接收到的消息集合</returns>
    protected abstract IEnumerable<INetworkMessage> ReceiveCore();

    /// <summary>
    /// 子类实现连接逻辑
    /// </summary>
    /// <param name="address">服务器地址</param>
    /// <param name="port">服务器端口</param>
    protected abstract void OnConnect(string address, int port);

    /// <summary>
    /// 子类实现断开连接逻辑
    /// </summary>
    protected abstract void OnDisconnect();

    #endregion

    #region 公共方法

    /// <summary>
    /// 连接到指定地址和端口
    /// </summary>
    /// <param name="address">服务器地址</param>
    /// <param name="port">服务器端口</param>
    public void Connect(string address, int port)
    {
        _connectionState = ConnectionState.Connecting;
        OnConnect(address, port);
        _connectionState = ConnectionState.Connected;
        OnConnected?.Invoke();
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        _connectionState = ConnectionState.Disconnecting;
        OnDisconnect();
        _connectionState = ConnectionState.Disconnected;
        _localPlayerId = PlayerId.Empty;
        OnDisconnected?.Invoke();
    }

    /// <summary>
    /// 发送不可靠消息
    /// </summary>
    /// <param name="data">待发送的数据</param>
    /// <exception cref="InvalidOperationException">网络未连接时抛出</exception>
    public void Send(byte[] data)
    {
        if (ConnectionState != ConnectionState.Connected)
        {
            throw new InvalidOperationException("网络未连接，无法发送消息");
        }

        SendCore(data);
    }

    /// <summary>
    /// 发送可靠消息
    /// </summary>
    /// <param name="data">待发送的数据</param>
    /// <exception cref="InvalidOperationException">网络未连接时抛出</exception>
    public void SendReliable(byte[] data)
    {
        if (ConnectionState != ConnectionState.Connected)
        {
            throw new InvalidOperationException("网络未连接，无法发送消息");
        }

        SendReliableCore(data);
    }

    /// <summary>
    /// 接收消息
    /// </summary>
    /// <returns>接收到的消息集合</returns>
    public IEnumerable<INetworkMessage> Receive()
    {
        var messages = ReceiveCore();

        foreach (var message in messages)
        {
            OnMessageReceived?.Invoke(message);
        }

        return messages;
    }

    /// <summary>
    /// 设置本地玩家标识
    /// </summary>
    /// <param name="id">玩家标识</param>
    protected void SetLocalPlayerId(PlayerId id)
    {
        _localPlayerId = id;
    }

    #endregion
}
