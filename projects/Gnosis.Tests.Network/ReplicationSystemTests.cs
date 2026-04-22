using Gnosis.Core.Event;
using Gnosis.Network.Replication;
using Gnosis.Network.Serialization;
using Gnosis.Network.Transport;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network;

public class ReplicationSystemTests : GnosisTester
{
    private ReplicationSystem _system = null!;
    private IMessageSerializer _serializer = null!;

    public override void Setup()
    {
        base.Setup();
        _serializer = new MessageSerializer();
        _system = new ReplicationSystem(_serializer);
    }

    public override void Teardown()
    {
        _system.Clear();
        base.Teardown();
    }

    [Test]
    public void RegisterEntity_注册后实体数量增加()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3]);

        Assert.That(_system.ReplicatedEntityCount, Is.EqualTo(1));
    }

    [Test]
    public void RegisterEntity_注册后可查询到实体()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.PlayerCharacter, [1, 2, 3]);

        Assert.That(_system.IsRegistered(entityId), Is.True);
    }

    [Test]
    public void RegisterEntity_注册后触发OnEntityRegistered事件()
    {
        EntityId registeredId = default;
        ReplicationGroup registeredGroup = default;
        _system.OnEntityRegistered += (id, group) => { registeredId = id; registeredGroup = group; };

        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.Npc, [1, 2, 3]);

        Assert.That(registeredId, Is.EqualTo(entityId));
        Assert.That(registeredGroup, Is.EqualTo(ReplicationGroup.Npc));
    }

    [Test]
    public void RegisterEntity_重复注册抛出异常()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3]);

        AssertThrows<InvalidOperationException>(() => _system.RegisterEntity(entityId, ReplicationGroup.Default, [4, 5, 6]));
    }

    [Test]
    public void UnregisterEntity_注销后实体数量减少()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3]);
        _system.UnregisterEntity(entityId);

        Assert.That(_system.ReplicatedEntityCount, Is.EqualTo(0));
        Assert.That(_system.IsRegistered(entityId), Is.False);
    }

    [Test]
    public void UnregisterEntity_注销未注册实体不抛出异常()
    {
        Assert.DoesNotThrow(() => _system.UnregisterEntity(EntityId.New()));
    }

    [Test]
    public void UpdateState_更新后状态数据改变()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3]);
        _system.UpdateState(entityId, [4, 5, 6]);

        var state = _system.GetEntityState(entityId);
        Assert.That(state!.Value.StateData.ToArray(), Is.EqualTo(new byte[] { 4, 5, 6 }));
    }

    [Test]
    public void UpdateState_更新后触发OnStateUpdated事件()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3]);

        byte[]? updatedData = null;
        _system.OnStateUpdated += (_, data) => updatedData = data.ToArray();
        _system.UpdateState(entityId, [4, 5, 6]);

        Assert.That(updatedData, Is.Not.Null);
        Assert.That(updatedData, Is.EqualTo(new byte[] { 4, 5, 6 }));
    }

    [Test]
    public void ReceiveStateUpdate_更高序列号更新状态()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3]);

        _system.ReceiveStateUpdate(entityId, [7, 8, 9], 100);

        var state = _system.GetEntityState(entityId);
        Assert.That(state!.Value.StateData.ToArray(), Is.EqualTo(new byte[] { 7, 8, 9 }));
    }

    [Test]
    public void ReceiveStateUpdate_更低序列号不更新状态()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3]);

        _system.ReceiveStateUpdate(entityId, [7, 8, 9], 100);
        _system.ReceiveStateUpdate(entityId, [0, 0, 0], 50);

        var state = _system.GetEntityState(entityId);
        Assert.That(state!.Value.StateData.ToArray(), Is.EqualTo(new byte[] { 7, 8, 9 }));
    }

    [Test]
    public void ComputeDelta_状态变化时返回增量数据()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3, 4, 5]);
        _system.CollectDirtyEntities();

        _system.UpdateState(entityId, [1, 2, 9, 4, 5]);
        var delta = _system.ComputeDelta(entityId);

        Assert.That(delta.Length, Is.GreaterThan(0));
    }

    [Test]
    public void ComputeDelta_状态未变化时返回空()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3]);
        _system.CollectDirtyEntities();

        var delta = _system.ComputeDelta(entityId);
        Assert.That(delta.Length, Is.EqualTo(0));
    }

    [Test]
    public void ApplyDelta_应用增量后恢复完整状态()
    {
        var entityId = EntityId.New();
        var original = new byte[] { 1, 2, 3, 4, 5 };
        _system.RegisterEntity(entityId, ReplicationGroup.Default, original);
        _system.CollectDirtyEntities();

        var modified = new byte[] { 1, 2, 9, 8, 5 };
        _system.UpdateState(entityId, modified);
        var delta = _system.ComputeDelta(entityId);

        var otherSystem = new ReplicationSystem(_serializer);
        otherSystem.RegisterEntity(entityId, ReplicationGroup.Default, original);
        otherSystem.ApplyDelta(entityId, delta.Span);

        var restored = otherSystem.GetEntityState(entityId);
        Assert.That(restored!.Value.StateData.ToArray(), Is.EqualTo(modified));
    }

    [Test]
    public void CollectDirtyEntities_返回有变化的实体()
    {
        var e1 = EntityId.New();
        var e2 = EntityId.New();
        _system.RegisterEntity(e1, ReplicationGroup.Default, [1, 2, 3]);
        _system.RegisterEntity(e2, ReplicationGroup.Default, [4, 5, 6]);
        _system.CollectDirtyEntities();

        _system.UpdateState(e1, [9, 9, 9]);

        var dirty = _system.CollectDirtyEntities();
        Assert.That(dirty.Count, Is.EqualTo(1));
        Assert.That(dirty[0].EntityId, Is.EqualTo(e1));
    }

    [Test]
    public void GetOwner_返回注册时的所有者()
    {
        var entityId = EntityId.New();
        var owner = new ConnectionId(42);
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3], owner);

        Assert.That(_system.GetOwner(entityId), Is.EqualTo(owner));
    }

    [Test]
    public void IsOwner_正确判断所有权()
    {
        var entityId = EntityId.New();
        var owner = new ConnectionId(42);
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3], owner);

        Assert.That(_system.IsOwner(entityId, owner), Is.True);
        Assert.That(_system.IsOwner(entityId, new ConnectionId(99)), Is.False);
    }

    [Test]
    public void TransferOwnership_转移后所有者变更()
    {
        var entityId = EntityId.New();
        var oldOwner = new ConnectionId(1);
        var newOwner = new ConnectionId(2);
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3], oldOwner);

        _system.TransferOwnership(entityId, newOwner);

        Assert.That(_system.GetOwner(entityId), Is.EqualTo(newOwner));
    }

    [Test]
    public void TransferOwnership_转移时触发OnOwnershipChanged事件()
    {
        var entityId = EntityId.New();
        var oldOwner = new ConnectionId(1);
        var newOwner = new ConnectionId(2);
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3], oldOwner);

        ConnectionId eventOldOwner = default;
        ConnectionId eventNewOwner = default;
        _system.OnOwnershipChanged += (_, oldId, newId) => { eventOldOwner = oldId; eventNewOwner = newId; };

        _system.TransferOwnership(entityId, newOwner);

        Assert.That(eventOldOwner, Is.EqualTo(oldOwner));
        Assert.That(eventNewOwner, Is.EqualTo(newOwner));
    }

    [Test]
    public void GetOwnedEntities_返回指定连接拥有的所有实体()
    {
        var owner = new ConnectionId(1);
        _system.RegisterEntity(EntityId.New(), ReplicationGroup.Default, [1], owner);
        _system.RegisterEntity(EntityId.New(), ReplicationGroup.Default, [2], owner);
        _system.RegisterEntity(EntityId.New(), ReplicationGroup.Default, [3], new ConnectionId(2));

        var owned = _system.GetOwnedEntities(owner);
        Assert.That(owned.Count, Is.EqualTo(2));
    }

    [Test]
    public void RemoveOwnedEntities_移除指定连接拥有的所有实体()
    {
        var owner = new ConnectionId(1);
        var keepEntityId = EntityId.New();
        _system.RegisterEntity(EntityId.New(), ReplicationGroup.Default, [1], owner);
        _system.RegisterEntity(EntityId.New(), ReplicationGroup.Default, [2], owner);
        _system.RegisterEntity(keepEntityId, ReplicationGroup.Default, [3], new ConnectionId(2));

        _system.RemoveOwnedEntities(owner);

        Assert.That(_system.ReplicatedEntityCount, Is.EqualTo(1));
        Assert.That(_system.IsRegistered(keepEntityId), Is.True);
    }

    [Test]
    public void MarkForDestroy_标记后ProcessPendingDestroys销毁实体()
    {
        var entityId = EntityId.New();
        _system.RegisterEntity(entityId, ReplicationGroup.Default, [1, 2, 3]);

        _system.MarkForDestroy(entityId);
        _system.ProcessPendingDestroys();

        Assert.That(_system.IsRegistered(entityId), Is.False);
    }

    [Test]
    public void Clear_清除所有状态()
    {
        _system.RegisterEntity(EntityId.New(), ReplicationGroup.Default, [1]);
        _system.RegisterEntity(EntityId.New(), ReplicationGroup.Default, [2]);

        _system.Clear();

        Assert.That(_system.ReplicatedEntityCount, Is.EqualTo(0));
    }
}
