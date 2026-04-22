using Gnosis.Network.Backends;
using Gnosis.Network.Core;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Network;

public class SteamNetworkBackendTests : GnosisTester
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

        AssertThrows<NotSupportedException>(() => backend.Send([1]), "Steam 网络后端尚未实现");
    }

    [Test]
    public void SteamNetworkBackend_SendReliable抛出NotSupportedException()
    {
        var backend = new SteamNetworkBackend();

        AssertThrows<NotSupportedException>(() => backend.SendReliable([1]), "Steam 网络后端尚未实现");
    }

    [Test]
    public void SteamNetworkBackend_Receive抛出NotSupportedException()
    {
        var backend = new SteamNetworkBackend();

        AssertThrows<NotSupportedException>(() => backend.Receive(), "Steam 网络后端尚未实现");
    }
}