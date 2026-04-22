namespace Gnosis.Network.Serialization;

/// <summary>
/// 消息序列化器接口
/// </summary>
public interface IMessageSerializer
{
    byte[] Serialize<T>(T message) where T : struct;
    T Deserialize<T>(ReadOnlySpan<byte> data) where T : struct;
}
