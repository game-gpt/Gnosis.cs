using Gnosis.Core.ValueObjects;

namespace Gnosis.Network;

public interface INetworkMessage
{
    int MessageId { get; }
    PlayerId SenderId { get; }
    Core.ValueObjects.Timestamp Timestamp { get; }
    ReadOnlySpan<byte> Payload { get; }
    bool IsReliable { get; }
}
