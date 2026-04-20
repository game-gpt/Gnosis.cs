using Gnosis.Core.ValueObjects;

namespace Gnosis.Network.Interfaces;

public interface INetworkMessage
{
    int MessageId { get; }
    PlayerId SenderId { get; }
    Core.ValueObjects.Timestamp Timestamp { get; }
    ReadOnlySpan<byte> Payload { get; }
    bool IsReliable { get; }
}
