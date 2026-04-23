using System.Collections.Generic;
using Gnosis.Security.Encryption;

namespace Gnosis.Network.Channel;

public sealed class EncryptedChannel : IChannel
{
    #region 字段

    private readonly IChannel _innerChannel;
    private readonly AesEncryptor _encryptor;
    private byte[] _encryptionKey;

    #endregion

    #region 属性

    public ChannelId Id => _innerChannel.Id;

    public ChannelType ChannelType => _innerChannel.ChannelType;

    #endregion

    #region 构造函数

    public EncryptedChannel(IChannel innerChannel, byte[] encryptionKey)
    {
        _innerChannel = innerChannel ?? throw new ArgumentNullException(nameof(innerChannel));
        _encryptor = new AesEncryptor();
        _encryptionKey = encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey));

        if (encryptionKey.Length != 32)
        {
            throw new ArgumentException("AES 加密密钥长度必须为 32 字节", nameof(encryptionKey));
        }
    }

    public EncryptedChannel(IChannel innerChannel) : this(innerChannel, AesEncryptor.GenerateKey())
    {
    }

    #endregion

    #region 公共方法

    public void Send(ReadOnlySpan<byte> data)
    {
        _innerChannel.Send(data);
    }

    public IReadOnlyList<ReadOnlyMemory<byte>> ProcessIncoming(ReadOnlyMemory<byte> data)
    {
        if (data.Length == 0)
        {
            return _innerChannel.ProcessIncoming(data);
        }

        try
        {
            var decryptedData = _encryptor.Decrypt(data.ToArray(), _encryptionKey);
            return _innerChannel.ProcessIncoming(decryptedData);
        }
        catch (SecurityException)
        {
            return [];
        }
    }

    public ReadOnlyMemory<byte> ProcessOutgoing(ReadOnlySpan<byte> data)
    {
        var innerOutgoing = _innerChannel.ProcessOutgoing(data);

        if (innerOutgoing.Length == 0)
        {
            return innerOutgoing;
        }

        var encryptedData = _encryptor.Encrypt(innerOutgoing.ToArray(), _encryptionKey);
        return encryptedData;
    }

    public void Update(TimeSpan deltaTime)
    {
        _innerChannel.Update(deltaTime);
    }

    public void RotateKey(byte[] newKey)
    {
        if (newKey is null || newKey.Length != 32)
        {
            throw new ArgumentException("AES 加密密钥长度必须为 32 字节", nameof(newKey));
        }

        _encryptionKey = newKey;
    }

    #endregion
}
