using Gnosis.Network.RPC;
using Gnosis.Network.Serialization;
using Gnosis.Network.Transport;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network;

public class RpcRouterTests : GnosisTester
{
    private RpcRouter _router = null!;
    private RpcSystem _rpcSystem = null!;

    public override void Setup()
    {
        base.Setup();
        _rpcSystem = new RpcSystem(new MessageSerializer());
        _router = new RpcRouter(_rpcSystem);
    }

    [Test]
    public void ConnectionCount_初始为零()
    {
        Assert.That(_router.ConnectionCount, Is.EqualTo(0));
    }

    [Test]
    public void ConnectionIds_初始为空()
    {
        Assert.That(_router.ConnectionIds.Count, Is.EqualTo(0));
    }

    [Test]
    public void RegisterConnection_Null连接抛出异常()
    {
        AssertThrows<ArgumentNullException>(() => _router.RegisterConnection(null!));
    }

    [Test]
    public void UnregisterConnection_注销未注册连接不抛出异常()
    {
        Assert.DoesNotThrow(() => _router.UnregisterConnection(new ConnectionId(999)));
    }

    [Test]
    public void GetConnection_未注册连接返回Null()
    {
        var connection = _router.GetConnection(new ConnectionId(1));
        Assert.That(connection, Is.Null);
    }

    [Test]
    public void OnRpcSent_事件可订阅()
    {
        var eventRaised = false;
        _router.OnRpcSent += (_, _) => eventRaised = true;
        Assert.That(eventRaised, Is.False);
    }

    [Test]
    public void OnRpcReceived_事件可订阅()
    {
        var eventRaised = false;
        _router.OnRpcReceived += (_, _) => eventRaised = true;
        Assert.That(eventRaised, Is.False);
    }

    [Test]
    public void OnRpcSendFailed_事件可订阅()
    {
        var eventRaised = false;
        _router.OnRpcSendFailed += (_, _, _) => eventRaised = true;
        Assert.That(eventRaised, Is.False);
    }

    [Test]
    public void FlushPendingCalls_无待处理调用不抛出异常()
    {
        Assert.DoesNotThrow(() => _router.FlushPendingCalls());
    }

    [Test]
    public void Poll_无连接不抛出异常()
    {
        Assert.DoesNotThrow(() => _router.Poll());
    }
}
