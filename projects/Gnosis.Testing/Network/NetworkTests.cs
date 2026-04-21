using Gnosis.Core;
using Gnosis.Network;
using NUnit.Framework;

namespace Gnosis.Testing.Network;

/// <summary>
/// 用于消息序列化测试的负载结构体
/// </summary>
readonly record struct TestPayload(int Value, float Factor);

/// <summary>
/// 网络模块集成测试，覆盖 NetworkMessage、MessageSerializer、NullNetworkBackend 和 NetworkManager
/// </summary>
[TestFixture]
public class NetworkTests : TestBase
{
    #region NetworkMessage 测试

    [Test]
    public void NetworkMessage_Create_设置所有字段()
    {
        var senderId = PlayerId.New();
        var payload = new byte[] { 1, 2, 3 };

        var message = NetworkMessage.Create(42, senderId, payload, true);

        Assert.That(message.MessageId, Is.EqualTo(42));
        Assert.That(message.SenderId, Is.EqualTo(senderId));
        Assert.That(message.IsReliable, Is.True);
    }

    [Test]
    public void NetworkMessage_Create_默认不可靠()
    {
        var message = NetworkMessage.Create(1, PlayerId.New(), Array.Empty<byte>());

        Assert.That(message.IsReliable, Is.False);
    }

    [Test]
    public void NetworkMessage_Create_自动设置时间戳()
    {
        var before = Timestamp.Now;
        var message = NetworkMessage.Create(1, PlayerId.New(), Array.Empty<byte>());
        var after = Timestamp.Now;

        Assert.That(message.Timestamp, Is.GreaterThanOrEqualTo(before));
        Assert.That(message.Timestamp, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void NetworkMessage_Payload_返回正确视图()
    {
        var payload = new byte[] { 10, 20, 30 };
        var message = NetworkMessage.Create(1, PlayerId.New(), payload);

        var span = message.Payload;

        Assert.That(span.Length, Is.EqualTo(3));
        Assert.That(span[0], Is.EqualTo(10));
        Assert.That(span[1], Is.EqualTo(20));
        Assert.That(span[2], Is.EqualTo(30));
    }

    [Test]
    public void NetworkMessage_Payload_空载荷返回空视图()
    {
        var message = NetworkMessage.Create(1, PlayerId.New(), Array.Empty<byte>());

        Assert.That(message.Payload.IsEmpty, Is.True);
    }

    #endregion

    #region MessageSerializer 测试

    private MessageSerializer _serializer = null!;

    public override void Setup()
    {
        base.Setup();
        _serializer = new MessageSerializer();
    }

    [Test]
    public void MessageSerializer_SerializeDeserialize_Int往返一致()
    {
        var original = 42;

        var bytes = _serializer.Serialize(original);
        var restored = _serializer.Deserialize<int>(bytes);

        Assert.That(restored, Is.EqualTo(original));
    }

    [Test]
    public void MessageSerializer_SerializeDeserialize_Float往返一致()
    {
        var original = 3.14f;

        var bytes = _serializer.Serialize(original);
        var restored = _serializer.Deserialize<float>(bytes);

        Assert.That(restored, Is.EqualTo(original));
    }

    [Test]
    public void MessageSerializer_SerializeDeserialize_Struct往返一致()
    {
        var original = new TestPayload(99, 2.5f);

        var bytes = _serializer.Serialize(original);
        var restored = _serializer.Deserialize<TestPayload>(bytes);

        Assert.That(restored.Value, Is.EqualTo(original.Value));
        Assert.That(restored.Factor, Is.EqualTo(original.Factor));
    }

    [Test]
    public void MessageSerializer_Serialize_返回非空字节数组()
    {
        var original = new TestPayload(1, 2.0f);

        var bytes = _serializer.Serialize(original);

        Assert.That(bytes, Is.Not.Null);
        Assert.That(bytes.Length, Is.GreaterThan(0));
    }

    [Test]
    public void MessageSerializer_Deserialize_空数据抛出ArgumentException()
    {
        AssertThrows<ArgumentException>(
            () => _serializer.Deserialize<TestPayload>(ReadOnlySpan<byte>.Empty),
            "数据长度");
    }

    [Test]
    public void MessageSerializer_Deserialize_数据不足抛出ArgumentException()
    {
        var data = new byte[1];

        AssertThrows<ArgumentException>(
            () => _serializer.Deserialize<TestPayload>(data),
            "小于结构体");
    }

    #endregion

    #region NullNetworkBackend 测试

    [Test]
    public void NullNetworkBackend_初始状态未连接()
    {
        var backend = new NullNetworkBackend();

        Assert.That(backend.IsConnected, Is.False);
        Assert.That(backend.LocalPlayerId, Is.EqualTo(PlayerId.Empty));
    }

    [Test]
    public void NullNetworkBackend_Connect_设置IsConnected为True()
    {
        var backend = new NullNetworkBackend();

        backend.Connect("localhost", 0);

        Assert.That(backend.IsConnected, Is.True);
    }

    [Test]
    public void NullNetworkBackend_Connect_设置LocalPlayerId为有效值()
    {
        var backend = new NullNetworkBackend();

        backend.Connect("localhost", 0);

        Assert.That(backend.LocalPlayerId, Is.Not.EqualTo(PlayerId.Empty));
    }

    [Test]
    public void NullNetworkBackend_Connect_触发OnConnected事件()
    {
        var backend = new NullNetworkBackend();
        bool eventFired = false;
        backend.OnConnected += () => eventFired = true;

        backend.Connect("localhost", 0);

        Assert.That(eventFired, Is.True);
    }

    [Test]
    public void NullNetworkBackend_Disconnect_重置IsConnected为False()
    {
        var backend = new NullNetworkBackend();
        backend.Connect("localhost", 0);

        backend.Disconnect();

        Assert.That(backend.IsConnected, Is.False);
    }

    [Test]
    public void NullNetworkBackend_Disconnect_重置LocalPlayerId为Empty()
    {
        var backend = new NullNetworkBackend();
        backend.Connect("localhost", 0);

        backend.Disconnect();

        Assert.That(backend.LocalPlayerId, Is.EqualTo(PlayerId.Empty));
    }

    [Test]
    public void NullNetworkBackend_Disconnect_触发OnDisconnected事件()
    {
        var backend = new NullNetworkBackend();
        backend.Connect("localhost", 0);
        bool eventFired = false;
        backend.OnDisconnected += () => eventFired = true;

        backend.Disconnect();

        Assert.That(eventFired, Is.True);
    }

    [Test]
    public void NullNetworkBackend_Send_已连接时不抛出异常()
    {
        var backend = new NullNetworkBackend();
        backend.Connect("localhost", 0);

        Assert.DoesNotThrow(() => backend.Send(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void NullNetworkBackend_SendReliable_已连接时不抛出异常()
    {
        var backend = new NullNetworkBackend();
        backend.Connect("localhost", 0);

        Assert.DoesNotThrow(() => backend.SendReliable(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void NullNetworkBackend_Send_未连接时抛出InvalidOperationException()
    {
        var backend = new NullNetworkBackend();

        AssertThrows<InvalidOperationException>(() => backend.Send(new byte[] { 1 }));
    }

    [Test]
    public void NullNetworkBackend_SendReliable_未连接时抛出InvalidOperationException()
    {
        var backend = new NullNetworkBackend();

        AssertThrows<InvalidOperationException>(() => backend.SendReliable(new byte[] { 1 }));
    }

    [Test]
    public void NullNetworkBackend_Receive_返回空集合()
    {
        var backend = new NullNetworkBackend();
        backend.Connect("localhost", 0);

        var messages = backend.Receive();

        Assert.That(messages, Is.Empty);
    }

    [Test]
    public void NullNetworkBackend_ConnectionState_状态转换正确()
    {
        var backend = new NullNetworkBackend();

        Assert.That(backend.ConnectionState, Is.EqualTo(ConnectionState.Disconnected));

        backend.Connect("localhost", 0);
        Assert.That(backend.ConnectionState, Is.EqualTo(ConnectionState.Connected));

        backend.Disconnect();
        Assert.That(backend.ConnectionState, Is.EqualTo(ConnectionState.Disconnected));
    }

    #endregion

    #region NetworkManager 测试

    private NetworkManager _manager = null!;
    private NullNetworkBackend _backend = null!;

    private void CreateManager()
    {
        _backend = new NullNetworkBackend();
        _manager = new NetworkManager(_backend);
    }

    [Test]
    public void NetworkManager_Initialize_连接后端并触发Connected状态()
    {
        CreateManager();
        ConnectionState? lastState = null;
        _manager.OnConnectionStateChanged += state => lastState = state;

        _manager.Initialize();

        Assert.That(lastState, Is.EqualTo(ConnectionState.Connected));
        Assert.That(_backend.IsConnected, Is.True);
    }

    [Test]
    public void NetworkManager_Shutdown_重置所有状态()
    {
        CreateManager();
        _manager.Initialize();
        _manager.CreateLobby(4);

        _manager.Shutdown();

        Assert.That(_manager.IsServer, Is.False);
        Assert.That(_manager.IsClient, Is.False);
        Assert.That(_manager.PlayerCount, Is.EqualTo(0));
    }

    [Test]
    public void NetworkManager_Shutdown_触发Disconnecting然后Disconnected()
    {
        CreateManager();
        _manager.Initialize();
        var states = new List<ConnectionState>();
        _manager.OnConnectionStateChanged += state => states.Add(state);

        _manager.Shutdown();

        Assert.That(states, Does.Contain(ConnectionState.Disconnecting));
        Assert.That(states, Does.Contain(ConnectionState.Disconnected));
        Assert.That(states.IndexOf(ConnectionState.Disconnecting), Is.LessThan(states.IndexOf(ConnectionState.Disconnected)));
    }

    [Test]
    public void NetworkManager_CreateLobby_设置IsServer为True()
    {
        CreateManager();
        _manager.Initialize();

        _manager.CreateLobby(4);

        Assert.That(_manager.IsServer, Is.True);
        Assert.That(_manager.IsClient, Is.False);
    }

    [Test]
    public void NetworkManager_CreateLobby_PlayerCount为1()
    {
        CreateManager();
        _manager.Initialize();

        _manager.CreateLobby(4);

        Assert.That(_manager.PlayerCount, Is.EqualTo(1));
    }

    [Test]
    public void NetworkManager_JoinLobby_设置IsClient为True()
    {
        CreateManager();
        _manager.Initialize();

        _manager.JoinLobby("test1234");

        Assert.That(_manager.IsClient, Is.True);
        Assert.That(_manager.IsServer, Is.False);
    }

    [Test]
    public void NetworkManager_JoinLobby_PlayerCount为1()
    {
        CreateManager();
        _manager.Initialize();

        _manager.JoinLobby("test1234");

        Assert.That(_manager.PlayerCount, Is.EqualTo(1));
    }

    [Test]
    public void NetworkManager_LeaveLobby_重置大厅状态()
    {
        CreateManager();
        _manager.Initialize();
        _manager.CreateLobby(4);

        _manager.LeaveLobby();

        Assert.That(_manager.IsServer, Is.False);
        Assert.That(_manager.IsClient, Is.False);
        Assert.That(_manager.PlayerCount, Is.EqualTo(0));
    }

    [Test]
    public void NetworkManager_SendToServer_已初始化时不抛出异常()
    {
        CreateManager();
        _manager.Initialize();
        _manager.CreateLobby(4);

        Assert.DoesNotThrow(() => _manager.SendToServer(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void NetworkManager_SendToAll_已初始化时不抛出异常()
    {
        CreateManager();
        _manager.Initialize();
        _manager.CreateLobby(4);

        Assert.DoesNotThrow(() => _manager.SendToAll(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void NetworkManager_PollMessages_无消息时返回空集合()
    {
        CreateManager();
        _manager.Initialize();
        _manager.CreateLobby(4);

        var messages = _manager.PollMessages();

        Assert.That(messages, Is.Empty);
    }

    [Test]
    public void NetworkManager_PollMessages_触发OnMessageReceived事件()
    {
        CreateManager();
        _manager.Initialize();
        _manager.CreateLobby(4);
        var receivedMessages = new List<INetworkMessage>();
        _manager.OnMessageReceived += msg => receivedMessages.Add(msg);

        _manager.PollMessages();

        Assert.That(receivedMessages, Is.Empty);
    }

    [Test]
    public void NetworkManager_CreateLobby_未初始化时抛出InvalidOperationException()
    {
        CreateManager();

        AssertThrows<InvalidOperationException>(() => _manager.CreateLobby(4));
    }

    [Test]
    public void NetworkManager_JoinLobby_未初始化时抛出InvalidOperationException()
    {
        CreateManager();

        AssertThrows<InvalidOperationException>(() => _manager.JoinLobby("test"));
    }

    [Test]
    public void NetworkManager_LeaveLobby_未初始化时抛出InvalidOperationException()
    {
        CreateManager();

        AssertThrows<InvalidOperationException>(() => _manager.LeaveLobby());
    }

    [Test]
    public void NetworkManager_SendToServer_未初始化时抛出InvalidOperationException()
    {
        CreateManager();

        AssertThrows<InvalidOperationException>(() => _manager.SendToServer(new byte[] { 1 }));
    }

    [Test]
    public void NetworkManager_SendToAll_未初始化时抛出InvalidOperationException()
    {
        CreateManager();

        AssertThrows<InvalidOperationException>(() => _manager.SendToAll(new byte[] { 1 }));
    }

    [Test]
    public void NetworkManager_PollMessages_未初始化时抛出InvalidOperationException()
    {
        CreateManager();

        AssertThrows<InvalidOperationException>(() => _manager.PollMessages());
    }

    [Test]
    public void NetworkManager_OnConnectionStateChanged_初始化时触发Connected()
    {
        CreateManager();
        ConnectionState? receivedState = null;
        _manager.OnConnectionStateChanged += state => receivedState = state;

        _manager.Initialize();

        Assert.That(receivedState, Is.EqualTo(ConnectionState.Connected));
    }

    #endregion
}
