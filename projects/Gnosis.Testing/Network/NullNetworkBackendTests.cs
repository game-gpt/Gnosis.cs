using Gnosis.Core;
using Gnosis.Network;
using NUnit.Framework;

namespace Gnosis.Testing.Network
{
    public class NullNetworkBackendTests : TestBase
    {
        private NullNetworkBackend _backend = null!;

        public override void Setup()
        {
            base.Setup();
            _backend = new NullNetworkBackend();
        }

        [Test]
        public void Connect_后IsConnected返回True()
        {
            _backend.Connect("localhost", 0);

            Assert.That(_backend.IsConnected, Is.True);
        }

        [Test]
        public void Connect_后LocalPlayerId非空()
        {
            _backend.Connect("localhost", 0);

            Assert.That(_backend.LocalPlayerId, Is.Not.EqualTo(PlayerId.Empty));
        }

        [Test]
        public void Connect_触发OnConnected事件()
        {
            var eventFired = false;
            _backend.OnConnected += () => eventFired = true;

            _backend.Connect("localhost", 0);

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Disconnect_后IsConnected返回False()
        {
            _backend.Connect("localhost", 0);
            _backend.Disconnect();

            Assert.That(_backend.IsConnected, Is.False);
        }

        [Test]
        public void Disconnect_触发OnDisconnected事件()
        {
            _backend.Connect("localhost", 0);
            var eventFired = false;
            _backend.OnDisconnected += () => eventFired = true;

            _backend.Disconnect();

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Send_不抛出异常()
        {
            _backend.Connect("localhost", 0);

            Assert.DoesNotThrow(() => _backend.Send(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void SendReliable_不抛出异常()
        {
            _backend.Connect("localhost", 0);

            Assert.DoesNotThrow(() => _backend.SendReliable(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void Receive_返回空集合()
        {
            _backend.Connect("localhost", 0);

            var messages = _backend.Receive();

            Assert.That(messages, Is.Empty);
        }

        [Test]
        public void Send_未连接时抛出InvalidOperationException()
        {
            AssertThrows<InvalidOperationException>(() => _backend.Send(new byte[] { 1 }));
        }

        [Test]
        public void SendReliable_未连接时抛出InvalidOperationException()
        {
            AssertThrows<InvalidOperationException>(() => _backend.SendReliable(new byte[] { 1 }));
        }

        [Test]
        public void ConnectionState_连接状态转换正确()
        {
            Assert.That(_backend.ConnectionState, Is.EqualTo(ConnectionState.Disconnected));

            _backend.Connect("localhost", 0);
            Assert.That(_backend.ConnectionState, Is.EqualTo(ConnectionState.Connected));

            _backend.Disconnect();
            Assert.That(_backend.ConnectionState, Is.EqualTo(ConnectionState.Disconnected));
        }
    }
}
