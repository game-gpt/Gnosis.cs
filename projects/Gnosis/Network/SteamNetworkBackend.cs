using System.Collections.Generic;
using Gnosis.Core;

namespace Gnosis.Network;

/// <summary>
/// Steam P2P 网络后端，基于 Steamworks SDK 实现点对点通信
/// </summary>
public sealed class SteamNetworkBackend : NetworkBackendBase
{
    #region 属性

    /// <summary>
    /// Steam 大厅 ID
    /// </summary>
    public ulong SteamLobbyId { get; private set; }

    /// <summary>
    /// 是否使用可靠传输作为默认
    /// </summary>
    public bool DefaultReliable { get; set; } = false;

    /// <summary>
    /// 消息轮询通道
    /// </summary>
    public int PollChannel { get; set; } = 0;

    #endregion

    #region NetworkBackendBase 实现

    /// <summary>
    /// 通过 Steam P2P 建立连接
    /// </summary>
    /// <param name="address">目标地址</param>
    /// <param name="port">目标端口</param>
    protected override void OnConnect(string address, int port)
    {
        SetLocalPlayerId(PlayerId.New());

        throw new NotImplementedException("Steam 后端尚未实现");
    }

    /// <summary>
    /// 断开 Steam P2P 连接
    /// </summary>
    protected override void OnDisconnect()
    {
        SteamLobbyId = 0;

        throw new NotImplementedException("Steam 后端尚未实现");
    }

    /// <summary>
    /// 通过 Steam P2P 发送不可靠消息
    /// </summary>
    /// <param name="data">待发送的数据</param>
    protected override void SendCore(byte[] data)
    {
        throw new NotImplementedException("Steam 后端尚未实现");
    }

    /// <summary>
    /// 通过 Steam P2P 发送可靠消息
    /// </summary>
    /// <param name="data">待发送的数据</param>
    protected override void SendReliableCore(byte[] data)
    {
        throw new NotImplementedException("Steam 后端尚未实现");
    }

    /// <summary>
    /// 从 Steam P2P 接收消息
    /// </summary>
    /// <returns>接收到的消息集合</returns>
    protected override IEnumerable<INetworkMessage> ReceiveCore()
    {
        throw new NotImplementedException("Steam 后端尚未实现");
    }

    #endregion
}
