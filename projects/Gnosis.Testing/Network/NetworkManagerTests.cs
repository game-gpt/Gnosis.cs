using Gnosis.Core;
using Gnosis.Network;
using NUnit.Framework;

namespace Gnosis.Testing.Network
{
    public class NetworkManagerTests : TestBase
    {
        private NetworkManager _manager = null!;

        public override void Setup()
        {
            base.Setup();
            _manager = new NetworkManager(NetworkBackendType.None);
        }

        public override void Teardown()
        {
            _manager.Shutdown();
            base.Teardown();
        }

        [Test]
        public void Initialize_None类型创建NullNetworkBackend()
        {
            _manager.Initialize();

            Assert.That(_manager.IsServer, Is.False);
            Assert.That(_manager.IsClient, Is.False);
        }

        [Test]
        public void Initialize_不支持的后端类型抛出NotSupportedException()
        {
            var manager = new NetworkManager(NetworkBackendType.Steam);

            AssertThrows<NotSupportedException>(() => manager.Initialize());
        }

        [Test]
        public void CreateLobby_设置IsServer为True()
        {
            _manager.Initialize();
            _manager.CreateLobby(4);

            Assert.That(_manager.IsServer, Is.True);
        }

        [Test]
        public void CreateLobby_PlayerCount为1()
        {
            _manager.Initialize();
            _manager.CreateLobby(4);

            Assert.That(_manager.PlayerCount, Is.EqualTo(1));
        }

        [Test]
        public void CreateLobby_触发OnPlayerJoined事件()
        {
            _manager.Initialize();
            PlayerId? joinedPlayer = null;
            _manager.OnPlayerJoined += id => joinedPlayer = id;

            _manager.CreateLobby(4);

            Assert.That(joinedPlayer, Is.Not.Null);
            Assert.That(joinedPlayer, Is.Not.EqualTo(PlayerId.Empty));
        }

        [Test]
        public void CreateLobby_未初始化时抛出InvalidOperationException()
        {
            AssertThrows<InvalidOperationException>(() => _manager.CreateLobby(4));
        }

        [Test]
        public void JoinLobby_设置IsClient为True()
        {
            _manager.Initialize();
            _manager.JoinLobby("test1234");

            Assert.That(_manager.IsClient, Is.True);
            Assert.That(_manager.IsServer, Is.False);
        }

        [Test]
        public void JoinLobby_未初始化时抛出InvalidOperationException()
        {
            AssertThrows<InvalidOperationException>(() => _manager.JoinLobby("test"));
        }

        [Test]
        public void LeaveLobby_重置状态()
        {
            _manager.Initialize();
            _manager.CreateLobby(4);
            _manager.LeaveLobby();

            Assert.That(_manager.IsServer, Is.False);
            Assert.That(_manager.IsClient, Is.False);
            Assert.That(_manager.PlayerCount, Is.EqualTo(0));
        }

        [Test]
        public void SendToServer_未初始化时抛出InvalidOperationException()
        {
            AssertThrows<InvalidOperationException>(() => _manager.SendToServer(new byte[] { 1 }));
        }

        [Test]
        public void SendToAll_未初始化时抛出InvalidOperationException()
        {
            AssertThrows<InvalidOperationException>(() => _manager.SendToAll(new byte[] { 1 }));
        }

        [Test]
        public void SendToServer_已连接时不抛出异常()
        {
            _manager.Initialize();
            _manager.CreateLobby(4);

            Assert.DoesNotThrow(() => _manager.SendToServer(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void SendToAll_已连接时不抛出异常()
        {
            _manager.Initialize();
            _manager.CreateLobby(4);

            Assert.DoesNotThrow(() => _manager.SendToAll(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void PollMessages_未初始化时返回空集合()
        {
            var messages = _manager.PollMessages();

            Assert.That(messages, Is.Empty);
        }

        [Test]
        public void PollMessages_无消息时返回空集合()
        {
            _manager.Initialize();
            _manager.CreateLobby(4);

            var messages = _manager.PollMessages();

            Assert.That(messages, Is.Empty);
        }

        [Test]
        public void Shutdown_重置所有状态()
        {
            _manager.Initialize();
            _manager.CreateLobby(4);
            _manager.Shutdown();

            Assert.That(_manager.IsServer, Is.False);
            Assert.That(_manager.IsClient, Is.False);
            Assert.That(_manager.PlayerCount, Is.EqualTo(0));
        }
    }
}
