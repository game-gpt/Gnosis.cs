using Gnosis.Core;
using Gnosis.Core.Event;
using Gnosis.Core.Time;

namespace Gnosis.Network.Serialization;

/// <summary>
/// 网络消息接口
/// </summary>
public interface INetworkMessage
{
    int MessageId { get; }
    PlayerId SenderId { get; }
    Timestamp Timestamp { get; }
    ReadOnlySpan<byte> Payload { get; }
    bool IsReliable { get; }
}
