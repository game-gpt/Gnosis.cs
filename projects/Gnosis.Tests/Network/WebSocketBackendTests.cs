using Gnosis.Network;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network
{
    public class WebSocketBackendTests : GnosisTester
    {
        [Test]
        public void WebSocketBackend_继承NetworkBackendBase()
        {
            var backend = new WebSocketBackend();

            Assert.That(backend, Is.InstanceOf<NetworkBackendBase>());
        }

        [Test]
        public void WebSocketBackend_实现INetworkBackend()
        {
            var backend = new WebSocketBackend();

            Assert.That(backend, Is.InstanceOf<INetworkBackend>());
        }

        [Test]
        public void WebSocketBackend_初始状态为未连接()
        {
            var backend = new WebSocketBackend();

            Assert.That(backend.IsConnected, Is.False);
            Assert.That(backend.ConnectionState, Is.EqualTo(ConnectionState.Disconnected));
        }

        [Test]
        public void WebSocketBackend_未连接时Send抛出InvalidOperationException()
        {
            var backend = new WebSocketBackend();

            AssertThrows<InvalidOperationException>(() => backend.Send(new byte[] { 1 }));
        }

        [Test]
        public void WebSocketBackend_未连接时SendReliable抛出InvalidOperationException()
        {
            var backend = new WebSocketBackend();

            AssertThrows<InvalidOperationException>(() => backend.SendReliable(new byte[] { 1 }));
        }

        [Test]
        public void WebSocketBackend_未连接时Receive返回空集合()
        {
            var backend = new WebSocketBackend();

            var messages = backend.Receive();

            Assert.That(messages, Is.Empty);
        }
    }
}
