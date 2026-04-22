using Gnosis.Network.RPC;
using Gnosis.Network.Serialization;
using Gnosis.Network.Transport;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network;

public class RpcSystemTests : GnosisTester
{
    private RpcSystem _system = null!;
    private IMessageSerializer _serializer = null!;

    public override void Setup()
    {
        base.Setup();
        _serializer = new MessageSerializer();
        _system = new RpcSystem(_serializer);
    }

    public override void Teardown()
    {
        _system.Clear();
        base.Teardown();
    }

    [Test]
    public void Register_注册方法后计数增加()
    {
        Assert.That(_system.RegisteredMethodCount, Is.EqualTo(0));

        _system.Register("TestMethod", (_, _) => { });

        Assert.That(_system.RegisteredMethodCount, Is.EqualTo(1));
    }

    [Test]
    public void Register_返回递增的方法标识()
    {
        var id1 = _system.Register("Method1", (_, _) => { });
        var id2 = _system.Register("Method2", (_, _) => { });

        Assert.That(id2, Is.GreaterThan(id1));
    }

    [Test]
    public void Register_重复注册同一方法名更新处理器()
    {
        var id1 = _system.Register("Method", (_, _) => { });
        var id2 = _system.Register("Method", (_, _) => { });

        Assert.That(id2, Is.EqualTo(id1));
        Assert.That(_system.RegisteredMethodCount, Is.EqualTo(1));
    }

    [Test]
    public void Register_空方法名抛出异常()
    {
        AssertThrows<ArgumentException>(() => _system.Register("", (_, _) => { }));
        AssertThrows<ArgumentException>(() => _system.Register(null!, (_, _) => { }));
    }

    [Test]
    public void Register_空处理器抛出异常()
    {
        AssertThrows<ArgumentNullException>(() => _system.Register("Method", null!));
    }

    [Test]
    public void Unregister_注销已注册方法返回True()
    {
        _system.Register("Method", (_, _) => { });

        Assert.That(_system.Unregister("Method"), Is.True);
        Assert.That(_system.RegisteredMethodCount, Is.EqualTo(0));
    }

    [Test]
    public void Unregister_注销未注册方法返回False()
    {
        Assert.That(_system.Unregister("NonExistent"), Is.False);
    }

    [Test]
    public void GetMethodId_已注册方法返回正确标识()
    {
        var id = _system.Register("Method", (_, _) => { });

        Assert.That(_system.GetMethodId("Method"), Is.EqualTo(id));
    }

    [Test]
    public void GetMethodId_未注册方法返回负一()
    {
        Assert.That(_system.GetMethodId("NonExistent"), Is.EqualTo(-1));
    }

    [Test]
    public void GetMethodName_已注册标识返回正确名称()
    {
        var id = _system.Register("TestMethod", (_, _) => { });

        Assert.That(_system.GetMethodName(id), Is.EqualTo("TestMethod"));
    }

    [Test]
    public void Execute_调用已注册方法触发处理器()
    {
        var receivedCaller = ConnectionId.Empty;
        byte[]? receivedData = null;
        var id = _system.Register("Method", (caller, args) =>
        {
            receivedCaller = caller;
            receivedData = args.ToArray();
        });

        var call = _system.CreateServerRpc(id, new byte[] { 0x01, 0x02 });
        _system.Execute(new ConnectionId(1), call);

        Assert.That(receivedCaller, Is.EqualTo(new ConnectionId(1)));
        Assert.That(receivedData, Is.EqualTo(new byte[] { 0x01, 0x02 }));
    }

    [Test]
    public void Execute_调用未注册方法触发失败事件()
    {
        var failedMethodId = -1;
        _system.OnRpcFailed += (methodId, _, _) => failedMethodId = methodId;

        var call = new RpcCall { MethodId = 999, RpcType = RpcType.ServerRpc, Arguments = [] };
        _system.Execute(new ConnectionId(1), call);

        Assert.That(failedMethodId, Is.EqualTo(999));
    }

    [Test]
    public void Execute_成功执行触发OnRpcExecuted事件()
    {
        var executedMethodId = -1;
        var id = _system.Register("Method", (_, _) => { });
        _system.OnRpcExecuted += (methodId, _) => executedMethodId = methodId;

        var call = _system.CreateServerRpc(id, []);
        _system.Execute(new ConnectionId(1), call);

        Assert.That(executedMethodId, Is.EqualTo(id));
    }

    [Test]
    public void SerializeCall_序列化后可正确反序列化()
    {
        var id = _system.Register("Method", (_, _) => { });
        var original = _system.CreateClientRpc(id, new ConnectionId(42), new ReadOnlyMemory<byte>(new byte[] { 0xAA, 0xBB }));

        var data = _system.SerializeCall(original);
        var deserialized = _system.DeserializeCall(data);

        Assert.That(deserialized.MethodId, Is.EqualTo(original.MethodId));
        Assert.That(deserialized.RpcType, Is.EqualTo(original.RpcType));
        Assert.That(deserialized.TargetConnection, Is.EqualTo(original.TargetConnection));
        Assert.That(deserialized.Arguments.ToArray(), Is.EqualTo(original.Arguments.ToArray()));
    }

    [Test]
    public void DeserializeCall_数据不足抛出异常()
    {
        var data = new byte[10];
        AssertThrows<ArgumentException>(() => _system.DeserializeCall(data));
    }

    [Test]
    public void EnqueueCall_加入队列后待处理数增加()
    {
        var id = _system.Register("Method", (_, _) => { });
        var call = _system.CreateServerRpc(id, []);

        _system.EnqueueCall(call);
        Assert.That(_system.PendingCallCount, Is.EqualTo(1));
    }

    [Test]
    public void DrainPendingCalls_取出后清空队列()
    {
        var id = _system.Register("Method", (_, _) => { });
        _system.EnqueueCall(_system.CreateServerRpc(id, []));
        _system.EnqueueCall(_system.CreateServerRpc(id, []));

        var calls = _system.DrainPendingCalls();
        Assert.That(calls.Count, Is.EqualTo(2));
        Assert.That(_system.PendingCallCount, Is.EqualTo(0));
    }

    [Test]
    public void Clear_清除所有注册和待处理调用()
    {
        _system.Register("Method1", (_, _) => { });
        _system.Register("Method2", (_, _) => { });
        var id = _system.GetMethodId("Method1");
        _system.EnqueueCall(_system.CreateServerRpc(id, []));

        _system.Clear();

        Assert.That(_system.RegisteredMethodCount, Is.EqualTo(0));
        Assert.That(_system.PendingCallCount, Is.EqualTo(0));
    }

    [Test]
    public void CreateServerRpc_未注册方法抛出异常()
    {
        AssertThrows<ArgumentException>(() => _system.CreateServerRpc(999, []));
    }

    [Test]
    public void CreateClientRpc_未注册方法抛出异常()
    {
        AssertThrows<ArgumentException>(() => _system.CreateClientRpc(999, new ConnectionId(1), []));
    }

    [Test]
    public void CreateMulticastRpc_未注册方法抛出异常()
    {
        AssertThrows<ArgumentException>(() => _system.CreateMulticastRpc(999, []));
    }

    [Test]
    public void HandleIncoming_正确处理接收到的RPC数据()
    {
        var executed = false;
        var id = _system.Register("Method", (_, _) => executed = true);
        var call = _system.CreateServerRpc(id, []);
        var data = _system.SerializeCall(call);

        _system.HandleIncoming(new ConnectionId(1), data);

        Assert.That(executed, Is.True);
    }
}
