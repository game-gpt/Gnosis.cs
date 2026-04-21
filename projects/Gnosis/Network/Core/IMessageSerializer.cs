namespace Gnosis.Network.Core;

public interface IMessageSerializer
{
    byte[] Serialize<T>(T message) where T : struct;
    T Deserialize<T>(ReadOnlySpan<byte> data) where T : struct;
}
