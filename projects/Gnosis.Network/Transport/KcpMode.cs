namespace Gnosis.Network.Transport;

/// <summary>
/// KCP 传输模式
/// </summary>
public enum KcpMode
{
    /// <summary>
    /// 默认模式：常规传输
    /// </summary>
    Normal,

    /// <summary>
    /// 快速模式：降低延迟，增加带宽消耗
    /// </summary>
    Fast,

    /// <summary>
    /// 极速模式：最低延迟，最大带宽消耗
    /// </summary>
    Fast3
}
