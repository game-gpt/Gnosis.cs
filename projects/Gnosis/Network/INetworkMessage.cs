using Gnosis.Core;
using Gnosis.Infrastructure;

namespace Gnosis.Network;

public interface INetworkMessage
{
    int MessageId { get; }
    PlayerId SenderId { get; }
    Timestamp Timestamp { get; }
    ReadOnlySpan<byte> Payload { get; }
    bool IsReliable { get; }
}
