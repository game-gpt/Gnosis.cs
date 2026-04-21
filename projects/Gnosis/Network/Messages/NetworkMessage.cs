using Gnosis.ECS.Core;
using Gnosis.Infrastructure;

namespace Gnosis.Network.Messages;

/// <summary>
/// 网络消息实现
/// </summary>
public sealed class NetworkMessage : INetworkMessage
{
    private readonly int _messageId;
    private readonly PlayerId _senderId;
    private readonly Timestamp _timestamp;
    private readonly byte[] _payload;
    private readonly bool _isReliable;

    /// <summary>
    /// 获取消息标识
    /// </summary>
    public int MessageId => _messageId;

    /// <summary>
    /// 获取发送者标识
    /// </summary>
    public PlayerId SenderId => _senderId;

    /// <summary>
    /// 获取时间戳
    /// </summary>
    public Timestamp Timestamp => _timestamp;

    /// <summary>
    /// 获取消息负载
    /// </summary>
    public ReadOnlySpan<byte> Payload => new(_payload);

    /// <summary>
    /// 获取是否为可靠消息
    /// </summary>
    public bool IsReliable => _isReliable;

    private NetworkMessage(int messageId, PlayerId senderId, Timestamp timestamp, byte[] payload, bool isReliable)
    {
        _messageId = messageId;
        _senderId = senderId;
        _timestamp = timestamp;
        _payload = payload;
        _isReliable = isReliable;
    }

    /// <summary>
    /// 创建网络消息，自动设置当前时间戳
    /// </summary>
    /// <param name="messageId">消息标识</param>
    /// <param name="senderId">发送者标识</param>
    /// <param name="payload">消息负载</param>
    /// <param name="reliable">是否为可靠消息</param>
    /// <returns>新创建的网络消息实例</returns>
    public static NetworkMessage Create(int messageId, PlayerId senderId, byte[] payload, bool reliable = false)
    {
        return new NetworkMessage(messageId, senderId, Timestamp.Now, payload, reliable);
    }
}
