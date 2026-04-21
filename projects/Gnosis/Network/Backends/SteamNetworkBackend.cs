using Gnosis.Network.Core;

namespace Gnosis.Network.Backends;

/// <summary>
/// Steam P2P 网络后端占位实现，等待 Steamworks SDK 集成后替换
/// </summary>
public sealed class SteamNetworkBackend : NetworkBackendBase
{
    private const string NotSupportedMessage = "Steam 网络后端尚未实现，请等待平台与插件团队完成 Steamworks SDK 集成";

    /// <summary>
    /// 连接时抛出不支持异常
    /// </summary>
    /// <param name="address">服务器地址</param>
    /// <param name="port">服务器端口</param>
    protected override void OnConnect(string address, int port)
    {
        throw new NotSupportedException(NotSupportedMessage);
    }

    /// <summary>
    /// 断开连接时抛出不支持异常
    /// </summary>
    protected override void OnDisconnect()
    {
        throw new NotSupportedException(NotSupportedMessage);
    }

    /// <summary>
    /// 发送数据时抛出不支持异常
    /// </summary>
    /// <param name="data">待发送的数据</param>
    protected override void SendCore(byte[] data)
    {
        throw new NotSupportedException(NotSupportedMessage);
    }

    /// <summary>
    /// 可靠发送数据时抛出不支持异常
    /// </summary>
    /// <param name="data">待发送的数据</param>
    protected override void SendReliableCore(byte[] data)
    {
        throw new NotSupportedException(NotSupportedMessage);
    }

    /// <summary>
    /// 接收消息时抛出不支持异常
    /// </summary>
    /// <returns>不会返回</returns>
    protected override IEnumerable<INetworkMessage> ReceiveCore()
    {
        throw new NotSupportedException(NotSupportedMessage);
    }
}
