using Gnosis.Network;
using NUnit.Framework;

namespace Gnosis.Tests.Network
{
    public class SteamNetworkBackendTests : TestBase
    {
        [Test]
        public void SteamNetworkBackend_继承NetworkBackendBase()
        {
            var backend = new SteamNetworkBackend();

            Assert.That(backend, Is.InstanceOf<NetworkBackendBase>());
        }

        [Test]
        public void SteamNetworkBackend_实现INetworkBackend()
        {
            var backend = new SteamNetworkBackend();

            Assert.That(backend, Is.InstanceOf<INetworkBackend>());
        }

        [Test]
        public void SteamNetworkBackend_Connect抛出NotSupportedException()
        {
            var backend = new SteamNetworkBackend();

            AssertThrows<NotSupportedException>(() => backend.Connect("localhost", 8080), "Steam 网络后端尚未实现");
        }

        [Test]
        public void SteamNetworkBackend_Send抛出NotSupportedException()
        {
            var backend = new SteamNetworkBackend();

            AssertThrows<NotSupportedException>(() => backend.Send(new byte[] { 1 }), "Steam 网络后端尚未实现");
        }

        [Test]
        public void SteamNetworkBackend_SendReliable抛出NotSupportedException()
        {
            var backend = new SteamNetworkBackend();

            AssertThrows<NotSupportedException>(() => backend.SendReliable(new byte[] { 1 }), "Steam 网络后端尚未实现");
        }

        [Test]
        public void SteamNetworkBackend_Receive抛出NotSupportedException()
        {
            var backend = new SteamNetworkBackend();

            AssertThrows<NotSupportedException>(() => backend.Receive(), "Steam 网络后端尚未实现");
        }
    }
}
