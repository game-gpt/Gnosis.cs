using Gnosis.Core;
using Gnosis.Network.Channel;
using Gnosis.Network.Prediction;
using Gnosis.Network.RPC;
using Gnosis.Network.Serialization;
using Gnosis.Network.Transport;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Network;

public class StateSyncSystemTests : GnosisTester
{
    private StateSyncSystem _system = null!;
    private NetworkManager _networkManager = null!;
    private MessageSerializer _serializer = null!;

    public override void Setup()
    {
        base.Setup();
        _networkManager = new NetworkManager(NetworkBackendType.None);
        _serializer = new MessageSerializer();
        _system = new StateSyncSystem(_networkManager, _serializer);
    }

    public override void Teardown()
    {
        _networkManager.Shutdown();
        base.Teardown();
    }

    [Test]
    public void StateSyncSystem_实现IStateSyncSystem接口()
    {
        Assert.That(_system, Is.InstanceOf<IStateSyncSystem>());
    }

    [Test]
    public void PredictionEnabled_默认为True()
    {
        Assert.That(_system.PredictionEnabled, Is.True);
    }

    [Test]
    public void PredictionEnabled_可设置()
    {
        _system.PredictionEnabled = false;
        Assert.That(_system.PredictionEnabled, Is.False);
    }

    [Test]
    public void RegisterEntity_增加状态数量()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, [1, 2, 3]);

        Assert.That(_system.ServerStateCount, Is.EqualTo(1));
        Assert.That(_system.PredictedStateCount, Is.EqualTo(1));
    }

    [Test]
    public void UnregisterEntity_减少状态数量()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, [1, 2, 3]);
        _system.UnregisterEntity(entityId);

        Assert.That(_system.ServerStateCount, Is.EqualTo(0));
        Assert.That(_system.PredictedStateCount, Is.EqualTo(0));
    }

    [Test]
    public void GetServerState_返回注册的状态()
    {
        var entityId = EntityId.New();
        var state = new byte[] { 10, 20, 30 };
        _system.RegisterEntity(entityId, state);

        var result = _system.GetServerState(entityId);

        Assert.That(result, Is.EqualTo(state));
    }

    [Test]
    public void GetPredictedState_返回注册的状态()
    {
        var entityId = EntityId.New();
        var state = new byte[] { 10, 20, 30 };
        _system.RegisterEntity(entityId, state);

        var result = _system.GetPredictedState(entityId);

        Assert.That(result, Is.EqualTo(state));
    }

    [Test]
    public void UpdatePredictedState_更新预测状态()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, [1]);
        var newState = new byte[] { 2, 3, 4 };

        _system.UpdatePredictedState(entityId, newState);

        Assert.That(_system.GetPredictedState(entityId), Is.EqualTo(newState));
    }

    [Test]
    public void OnReceiveServerState_触发OnServerStateReceived事件()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, [1]);

        EntityId? receivedEntityId = null;
        _system.OnServerStateReceived += (id, _) => receivedEntityId = id;

        var stateData = BuildStateData(entityId, [5, 6]);
        _system.OnReceiveServerState(stateData);

        Assert.That(receivedEntityId, Is.EqualTo(entityId));
    }

    [Test]
    public void OnReceiveServerState_预测误差大时触发OnReconciliation()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, [1, 1, 1, 1, 1, 1, 1, 1, 1, 1]);
        _system.ReconciliationThreshold = 0.01f;

        bool reconciled = false;
        _system.OnReconciliation += (_, _) => reconciled = true;

        var stateData = BuildStateData(entityId, [200, 200, 200, 200, 200, 200, 200, 200, 200, 200]);
        _system.OnReceiveServerState(stateData);

        Assert.That(reconciled, Is.True);
    }

    [Test]
    public void OnReceiveServerState_数据不足16字节时忽略()
    {
        bool eventFired = false;
        _system.OnServerStateReceived += (_, _) => eventFired = true;

        _system.OnReceiveServerState([1, 2, 3]);

        Assert.That(eventFired, Is.False);
    }

    [Test]
    public void Clear_清除所有状态()
    {
        _system.RegisterEntity(EntityId.New(), [1]);
        _system.RegisterEntity(EntityId.New(), [2]);

        _system.Clear();

        Assert.That(_system.ServerStateCount, Is.EqualTo(0));
        Assert.That(_system.PredictedStateCount, Is.EqualTo(0));
    }

    private static byte[] BuildStateData(EntityId entityId, byte[] payload)
    {
        var guidBytes = entityId.Value.ToByteArray();
        var data = new byte[16 + payload.Length];
        Array.Copy(guidBytes, 0, data, 0, 16);
        Array.Copy(payload, 0, data, 16, payload.Length);
        return data;
    }
}