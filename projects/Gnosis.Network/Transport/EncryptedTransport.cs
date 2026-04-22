using Gnosis.Network.Channel;
using Gnosis.Security.Encryption;

namespace Gnosis.Network.Transport;

public sealed class EncryptedTransport : ITransport
{
    #region 字段

    private readonly ITransport _innerTransport;
    private readonly AesEncryptor _encryptor;
    private byte[] _encryptionKey;
    private bool _isDisposed;

    #endregion

    #region 属性

    public TransportState State => _innerTransport.State;

    public bool IsEncryptionEnabled => _encryptionKey.Length > 0;

    #endregion

    #region 事件

    public event Action<ITransportConnection>? OnConnectionReceived
    {
        add => _innerTransport.OnConnectionReceived += value;
        remove => _innerTransport.OnConnectionReceived -= value;
    }

    public event Action<TransportState>? OnStateChanged
    {
        add => _innerTransport.OnStateChanged += value;
        remove => _innerTransport.OnStateChanged -= value;
    }

    #endregion

    #region 构造函数

    public EncryptedTransport(ITransport innerTransport, byte[] encryptionKey)
    {
        _innerTransport = innerTransport ?? throw new ArgumentNullException(nameof(innerTransport));
        _encryptor = new AesEncryptor();
        _encryptionKey = encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey));

        if (encryptionKey.Length != 32)
        {
            throw new ArgumentException("AES 加密密钥长度必须为 32 字节", nameof(encryptionKey));
        }

        _innerTransport.OnConnectionReceived += connection =>
        {
            var encryptedConnection = new EncryptedConnection(connection, _encryptor, _encryptionKey);
            OnConnectionReceivedInternal?.Invoke(encryptedConnection);
        };
    }

    public EncryptedTransport(ITransport innerTransport) : this(innerTransport, AesEncryptor.GenerateKey())
    {
    }

    #endregion

    #region 公共方法

    public ITransportConnection Connect(string address, int port)
    {
        var innerConnection = _innerTransport.Connect(address, port);
        return new EncryptedConnection(innerConnection, _encryptor, _encryptionKey);
    }

    public void Listen(int port)
    {
        _innerTransport.Listen(port);
    }

    public void Disconnect()
    {
        _innerTransport.Disconnect();
    }

    public byte[] GetEncryptionKey()
    {
        return _encryptionKey;
    }

    public void RotateKey(byte[] newKey)
    {
        if (newKey is null || newKey.Length != 32)
        {
            throw new ArgumentException("AES 加密密钥长度必须为 32 字节", nameof(newKey));
        }

        _encryptionKey = newKey;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _innerTransport.Dispose();
        CryptographicOperations.ZeroMemory(_encryptionKey);
    }

    #endregion

    #region 内部事件

    private event Action<ITransportConnection>? OnConnectionReceivedInternal;

    #endregion
}

public sealed class EncryptedConnection : ITransportConnection
{
    #region 字段

    private readonly ITransportConnection _innerConnection;
    private readonly AesEncryptor _encryptor;
    private byte[] _encryptionKey;
    private bool _isDisposed;

    #endregion

    #region 属性

    public ConnectionId Id => _innerConnection.Id;

    public bool IsConnected => _innerConnection.IsConnected;

    #endregion

    #region 事件

    public event Action<ConnectionId>? OnDisconnected
    {
        add => _innerConnection.OnDisconnected += value;
        remove => _innerConnection.OnDisconnected -= value;
    }

    #endregion

    #region 构造函数

    public EncryptedConnection(ITransportConnection innerConnection, AesEncryptor encryptor, byte[] encryptionKey)
    {
        _innerConnection = innerConnection ?? throw new ArgumentNullException(nameof(innerConnection));
        _encryptor = encryptor ?? throw new ArgumentNullException(nameof(encryptor));
        _encryptionKey = encryptionKey ?? throw new ArgumentNullException(nameof(encryptionKey));
    }

    #endregion

    #region 公共方法

    public void Send(ChannelId channelId, ReadOnlySpan<byte> data)
    {
        var plainData = data.ToArray();
        var encryptedData = _encryptor.Encrypt(plainData, _encryptionKey);

        var framed = new byte[1 + encryptedData.Length];
        framed[0] = channelId.Value;
        Buffer.BlockCopy(encryptedData, 0, framed, 1, encryptedData.Length);

        _innerConnection.Send(channelId, framed);
    }

    public IReadOnlyList<TransportEvent> Poll()
    {
        var events = _innerConnection.Poll();
        var decryptedEvents = new List<TransportEvent>(events.Count);

        foreach (var evt in events)
        {
            if (evt.Type == TransportEventType.DataReceived && evt.Data.Length > 0)
            {
                try
                {
                    var decryptedData = _encryptor.Decrypt(evt.Data.ToArray(), _encryptionKey);
                    decryptedEvents.Add(TransportEvent.DataReceived(evt.ConnectionId, evt.ChannelId, decryptedData));
                }
                catch (SecurityException)
                {
                    continue;
                }
            }
            else
            {
                decryptedEvents.Add(evt);
            }
        }

        return decryptedEvents;
    }

    public ChannelId CreateChannel(ChannelType channelType)
    {
        return _innerConnection.CreateChannel(channelType);
    }

    public void Disconnect()
    {
        _innerConnection.Disconnect();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _innerConnection.Dispose();
    }

    internal void UpdateKey(byte[] newKey)
    {
        _encryptionKey = newKey;
    }

    #endregion
}
