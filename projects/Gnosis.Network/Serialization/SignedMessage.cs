using Gnosis.Core.Event;
using Gnosis.Core.Time;
using Gnosis.Security.Encryption;

namespace Gnosis.Network.Serialization;

public sealed class SignedMessage : INetworkMessage
{
    #region 常量

    private const int SignatureSize = 32;
    private const int GuidSize = 16;
    private const int TimestampSize = 8;

    #endregion

    #region 字段

    private readonly int _messageId;
    private readonly PlayerId _senderId;
    private readonly Timestamp _timestamp;
    private readonly byte[] _payload;
    private readonly bool _isReliable;
    private readonly byte[] _signature;

    #endregion

    #region 属性

    public int MessageId => _messageId;
    public PlayerId SenderId => _senderId;
    public Timestamp Timestamp => _timestamp;
    public ReadOnlySpan<byte> Payload => new(_payload);
    public bool IsReliable => _isReliable;
    public ReadOnlySpan<byte> Signature => new(_signature);
    public bool IsSigned => _signature.Length == SignatureSize;

    #endregion

    #region 构造函数

    private SignedMessage(int messageId, PlayerId senderId, Timestamp timestamp, byte[] payload, bool isReliable, byte[] signature)
    {
        _messageId = messageId;
        _senderId = senderId;
        _timestamp = timestamp;
        _payload = payload;
        _isReliable = isReliable;
        _signature = signature;
    }

    #endregion

    #region 工厂方法

    public static SignedMessage Create(int messageId, PlayerId senderId, byte[] payload, byte[] signingKey, bool reliable = false)
    {
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (signingKey is null || signingKey.Length == 0)
        {
            throw new ArgumentException("签名密钥不能为空", nameof(signingKey));
        }

        var timestamp = Timestamp.Now;
        var signer = new HmacSigner();
        var signData = BuildSignData(messageId, senderId, timestamp, payload);
        var signature = signer.Sign(signData, signingKey);

        return new SignedMessage(messageId, senderId, timestamp, payload, reliable, signature);
    }

    public static SignedMessage CreateUnsigned(int messageId, PlayerId senderId, byte[] payload, bool reliable = false)
    {
        return new SignedMessage(messageId, senderId, Timestamp.Now, payload, reliable, []);
    }

    #endregion

    #region 公共方法

    public bool Verify(byte[] signingKey)
    {
        if (!IsSigned)
        {
            return false;
        }

        if (signingKey is null || signingKey.Length == 0)
        {
            return false;
        }

        var signer = new HmacSigner();
        var signData = BuildSignData(_messageId, _senderId, _timestamp, _payload);

        return signer.Verify(signData, _signature, signingKey);
    }

    public byte[] ToBytes()
    {
        var headerSize = 4 + GuidSize + TimestampSize + 1;
        var signatureLength = _signature.Length;
        var totalSize = headerSize + 4 + signatureLength + 4 + _payload.Length;
        var buffer = new byte[totalSize];
        var offset = 0;

        WriteInt32(buffer, ref offset, _messageId);
        WriteGuid(buffer, ref offset, _senderId.Value);
        WriteInt64(buffer, ref offset, _timestamp.Value.ToUnixTimeMilliseconds());
        buffer[offset++] = (byte)(_isReliable ? 1 : 0);
        WriteInt32(buffer, ref offset, signatureLength);
        Buffer.BlockCopy(_signature, 0, buffer, offset, signatureLength);
        offset += signatureLength;
        WriteInt32(buffer, ref offset, _payload.Length);
        Buffer.BlockCopy(_payload, 0, buffer, offset, _payload.Length);

        return buffer;
    }

    public static SignedMessage FromBytes(ReadOnlySpan<byte> data)
    {
        var offset = 0;
        var messageId = ReadInt32(data, ref offset);
        var senderGuid = ReadGuid(data, ref offset);
        var timestampMs = ReadInt64(data, ref offset);
        var isReliable = data[offset++] != 0;
        var signatureLength = ReadInt32(data, ref offset);
        var signature = new byte[signatureLength];
        data.Slice(offset, signatureLength).CopyTo(signature);
        offset += signatureLength;
        var payloadLength = ReadInt32(data, ref offset);
        var payload = new byte[payloadLength];
        data.Slice(offset, payloadLength).CopyTo(payload);

        return new SignedMessage(
            messageId,
            new PlayerId(senderGuid),
            Timestamp.FromUnixTimeMilliseconds(timestampMs),
            payload,
            isReliable,
            signature
        );
    }

    #endregion

    #region 私有方法

    private static byte[] BuildSignData(int messageId, PlayerId senderId, Timestamp timestamp, byte[] payload)
    {
        var size = 4 + GuidSize + TimestampSize + payload.Length;
        var buffer = new byte[size];
        var offset = 0;

        WriteInt32(buffer, ref offset, messageId);
        WriteGuid(buffer, ref offset, senderId.Value);
        WriteInt64(buffer, ref offset, timestamp.Value.ToUnixTimeMilliseconds());
        Buffer.BlockCopy(payload, 0, buffer, offset, payload.Length);

        return buffer;
    }

    private static void WriteInt32(byte[] buffer, ref int offset, int value)
    {
        buffer[offset++] = (byte)value;
        buffer[offset++] = (byte)(value >> 8);
        buffer[offset++] = (byte)(value >> 16);
        buffer[offset++] = (byte)(value >> 24);
    }

    private static void WriteInt64(byte[] buffer, ref int offset, long value)
    {
        buffer[offset++] = (byte)value;
        buffer[offset++] = (byte)(value >> 8);
        buffer[offset++] = (byte)(value >> 16);
        buffer[offset++] = (byte)(value >> 24);
        buffer[offset++] = (byte)(value >> 32);
        buffer[offset++] = (byte)(value >> 40);
        buffer[offset++] = (byte)(value >> 48);
        buffer[offset++] = (byte)(value >> 56);
    }

    private static void WriteGuid(byte[] buffer, ref int offset, Guid value)
    {
        value.TryWriteBytes(buffer.AsSpan(offset, GuidSize));
        offset += GuidSize;
    }

    private static int ReadInt32(ReadOnlySpan<byte> data, ref int offset)
    {
        var value = data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24);
        offset += 4;
        return value;
    }

    private static long ReadInt64(ReadOnlySpan<byte> data, ref int offset)
    {
        var low = (uint)(data[offset] | (data[offset + 1] << 8) | (data[offset + 2] << 16) | (data[offset + 3] << 24));
        var high = (uint)(data[offset + 4] | (data[offset + 5] << 8) | (data[offset + 6] << 16) | (data[offset + 7] << 24));
        offset += 8;
        return (long)((ulong)high << 32) | low;
    }

    private static Guid ReadGuid(ReadOnlySpan<byte> data, ref int offset)
    {
        var guidBytes = data.Slice(offset, GuidSize);
        offset += GuidSize;
        return new Guid(guidBytes);
    }

    #endregion
}
