using System.Collections.Generic;
using Gnosis.Core;

namespace Gnosis.Network;

/// <summary>
/// WebSocket 网络后端，基于客户端-服务器模式通信
/// </summary>
public sealed class WebSocketBackend : NetworkBackendBase
{
    #region 属性

    /// <summary>
    /// 服务器地址
    /// </summary>
    public string? ServerAddress { get; private set; }

    /// <summary>
    /// 服务器端口
    /// </summary>
    public int ServerPort { get; private set; }

    /// <summary>
    /// 心跳间隔（毫秒）
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 5000;

    /// <summary>
    /// 是否启用心跳
    /// </summary>
    public bool HeartbeatEnabled { get; set; } = true;

    #endregion

    #region NetworkBackendBase 实现

    /// <summary>
    /// 连接到 WebSocket 服务器
    /// </summary>
    /// <param name="address">服务器地址</param>
    /// <param name="port">服务器端口</param>
    protected override void OnConnect(string address, int port)
    {
        ServerAddress = address;
        ServerPort = port;
        SetLocalPlayerId(PlayerId.New());

        throw new NotImplementedException("WebSocket 后端尚未实现");
    }

    /// <summary>
    /// 断开 WebSocket 连接
    /// </summary>
    protected override void OnDisconnect()
    {
        ServerAddress = null;
        ServerPort = 0;

        throw new NotImplementedException("WebSocket 后端尚未实现");
    }

    /// <summary>
    /// 通过 WebSocket 发送不可靠消息
    /// </summary>
    /// <param name="data">待发送的数据</param>
    protected override void SendCore(byte[] data)
    {
        throw new NotImplementedException("WebSocket 后端尚未实现");
    }

    /// <summary>
    /// 通过 WebSocket 发送可靠消息
    /// </summary>
    /// <param name="data">待发送的数据</param>
    protected override void SendReliableCore(byte[] data)
    {
        throw new NotImplementedException("WebSocket 后端尚未实现");
    }

    /// <summary>
    /// 从 WebSocket 接收消息
    /// </summary>
    /// <returns>接收到的消息集合</returns>
    protected override IEnumerable<INetworkMessage> ReceiveCore()
    {
        throw new NotImplementedException("WebSocket 后端尚未实现");
    }

    #endregion
}
