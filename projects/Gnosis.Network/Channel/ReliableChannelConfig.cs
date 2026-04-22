namespace Gnosis.Network.Channel;

/// <summary>
/// 可靠信道配置
/// </summary>
public sealed class ReliableChannelConfig
{
    /// <summary>
    /// 重传超时时间（毫秒），默认 200ms
    /// </summary>
    public int RetransmitTimeoutMs { get; set; } = 200;

    /// <summary>
    /// 最大重试次数，默认 20 次
    /// </summary>
    public int MaxRetries { get; set; } = 20;

    /// <summary>
    /// 发送窗口大小，默认 64
    /// </summary>
    public int WindowSize { get; set; } = 64;

    /// <summary>
    /// 最大消息大小（字节），默认 1200
    /// </summary>
    public int MaxMessageSize { get; set; } = 1200;

    /// <summary>
    /// 是否启用拥塞控制，默认启用
    /// </summary>
    public bool CongestionControlEnabled { get; set; } = true;

    /// <summary>
    /// 获取默认配置
    /// </summary>
    public static ReliableChannelConfig Default => new();
}
