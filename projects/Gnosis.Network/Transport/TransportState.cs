namespace Gnosis.Network.Transport;

/// <summary>
/// 传输层状态
/// </summary>
public enum TransportState
{
    /// <summary>
    /// 未初始化
    /// </summary>
    None,

    /// <summary>
    /// 正在连接
    /// </summary>
    Connecting,

    /// <summary>
    /// 正在监听
    /// </summary>
    Listening,

    /// <summary>
    /// 已连接
    /// </summary>
    Connected,

    /// <summary>
    /// 正在断开
    /// </summary>
    Disconnecting,

    /// <summary>
    /// 已断开
    /// </summary>
    Disconnected
}
