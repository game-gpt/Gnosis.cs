using System.Numerics;
using Gnosis.Core;
using Gnosis.ECS.System;
using Gnosis.ECS.World;
using Gnosis.Physics.Collision;
using Gnosis.Physics.Dynamics;
using Gnosis.Physics.Query;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.ECS;

public sealed class PhysicsEcsSystem : IWorldSystem
{
    #region 字段

    private World? _world;
    private readonly IPhysicsSystem _physicsSystem;
    private readonly Dictionary<EntityId, IRigidBody> _entityToBody = new();
    private readonly Dictionary<IRigidBody, EntityId> _bodyToEntity = new();
    private readonly List<ICollisionEvent> _collisionEvents = new();
    private readonly List<ITriggerEvent> _triggerEvents = new();

    #endregion

    #region 属性

    public SystemPhase Phase => SystemPhase.Update;

    public IPhysicsWorld PhysicsWorld => _physicsSystem.DefaultWorld;

    #endregion

    #region 构造函数

    public PhysicsEcsSystem()
    {
        _physicsSystem = new PhysicsSystem();
    }

    public PhysicsEcsSystem(IPhysicsSystem physicsSystem)
    {
        _physicsSystem = physicsSystem;
    }

    #endregion

    #region ISystem 实现

    public void Initialize()
    {
    }

    public void Shutdown()
    {
        foreach (var body in _entityToBody.Values)
        {
            _physicsSystem.DefaultWorld.DestroyRigidBody(body);
        }

        _entityToBody.Clear();
        _bodyToEntity.Clear();
        _collisionEvents.Clear();
        _triggerEvents.Clear();
    }

    public void Update(float delta)
    {
        if (_world == null)
        {
            return;
        }

        SyncRigidBodies();
        SyncColliders();

        _physicsSystem.Update(delta);

        SyncTransformsFromPhysics();
        DispatchCollisionEvents();
    }

    public void SetWorld(World world)
    {
        _world = world;
    }

    #endregion

    #region 公开方法 - 查询

    public IRaycastResult Raycast(Vector3 origin, Vector3 direction, float maxDistance)
    {
        return _physicsSystem.DefaultWorld.Raycast(origin, direction, maxDistance);
    }

    public IRaycastResult[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance)
    {
        return _physicsSystem.DefaultWorld.RaycastAll(origin, direction, maxDistance);
    }

    public IOverlapResult OverlapSphere(Vector3 center, float radius)
    {
        return _physicsSystem.DefaultWorld.OverlapSphere(center, radius);
    }

    public IOverlapResult OverlapBox(Vector3 center, Vector3 halfExtents)
    {
        return _physicsSystem.DefaultWorld.OverlapBox(center, halfExtents);
    }

    public EntityId? GetEntityFromBody(IRigidBody body)
    {
        return _bodyToEntity.TryGetValue(body, out var entityId) ? entityId : null;
    }

    #endregion

    #region 事件

    public event Action<EntityId, EntityId, ICollisionEvent>? OnCollision;

    public event Action<EntityId, EntityId, ITriggerEvent>? OnTrigger;

    #endregion

    #region 私有方法 - RigidBody 同步

    private void SyncRigidBodies()
    {
        if (_world == null)
        {
            return;
        }

        var query = _world.CreateQuery()
            .All<RigidBodyComponent>()
            .Build();

        var currentEntities = new HashSet<EntityId>(query);

        var removedEntities = new List<EntityId>();
        foreach (var entityId in _entityToBody.Keys)
        {
            if (!currentEntities.Contains(entityId))
            {
                removedEntities.Add(entityId);
            }
        }

        foreach (var entityId in removedEntities)
        {
            if (_entityToBody.Remove(entityId, out var body))
            {
                _bodyToEntity.Remove(body);
                _physicsSystem.DefaultWorld.DestroyRigidBody(body);
            }
        }

        foreach (var entityId in currentEntities)
        {
            var rbComp = _world.GetComponent<RigidBodyComponent>(entityId);

            if (_entityToBody.TryGetValue(entityId, out var existingBody))
            {
                SyncRigidBodyProperties(existingBody, rbComp);
            }
            else
            {
                var body = _physicsSystem.DefaultWorld.CreateRigidBody(
                    $"entity_{entityId.Index}",
                    rbComp.BodyType);
                body.Mass = rbComp.Mass;
                body.UseGravity = rbComp.UseGravity;

                _entityToBody[entityId] = body;
                _bodyToEntity[body] = entityId;

                rbComp.RigidBody = body;
                _world.SetComponent(entityId, rbComp);
            }
        }
    }

    private void SyncRigidBodyProperties(IRigidBody body, RigidBodyComponent component)
    {
        if (Math.Abs(body.Mass - component.Mass) > 0.0001f)
        {
            body.Mass = component.Mass;
        }

        if (body.UseGravity != component.UseGravity)
        {
            body.UseGravity = component.UseGravity;
        }

        if (body.BodyType != component.BodyType)
        {
            body.BodyType = component.BodyType;
        }
    }

    #endregion

    #region 私有方法 - Collider 同步

    private void SyncColliders()
    {
        if (_world == null)
        {
            return;
        }

        var query = _world.CreateQuery()
            .All<RigidBodyComponent>()
            .All<ColliderComponent>()
            .Build();

        foreach (var entityId in query)
        {
            var rbComp = _world.GetComponent<RigidBodyComponent>(entityId);
            var colComp = _world.GetComponent<ColliderComponent>(entityId);

            if (rbComp.RigidBody == null || colComp.Collider == null)
            {
                continue;
            }

            if (colComp.Collider.IsTrigger != colComp.IsTrigger)
            {
                colComp.Collider.IsTrigger = colComp.IsTrigger;
            }
        }
    }

    #endregion

    #region 私有方法 - 变换同步

    private void SyncTransformsFromPhysics()
    {
        if (_world == null)
        {
            return;
        }

        _physicsSystem.DefaultWorld.SyncTransforms();

        foreach (var (entityId, body) in _entityToBody)
        {
            if (!_world.HasComponent<RigidBodyComponent>(entityId))
            {
                continue;
            }

            var rbComp = _world.GetComponent<RigidBodyComponent>(entityId);
            rbComp.RigidBody = body;
            _world.SetComponent(entityId, rbComp);
        }
    }

    #endregion

    #region 私有方法 - 碰撞事件分发

    private void DispatchCollisionEvents()
    {
        foreach (var collisionEvent in _collisionEvents)
        {
            var thisEntity = GetEntityFromCollider(collisionEvent.ThisCollider);
            var otherEntity = GetEntityFromCollider(collisionEvent.OtherCollider);

            if (thisEntity.HasValue && otherEntity.HasValue)
            {
                OnCollision?.Invoke(thisEntity.Value, otherEntity.Value, collisionEvent);
            }
        }

        foreach (var triggerEvent in _triggerEvents)
        {
            var thisEntity = GetEntityFromCollider(triggerEvent.ThisCollider);
            var otherEntity = GetEntityFromCollider(triggerEvent.OtherCollider);

            if (thisEntity.HasValue && otherEntity.HasValue)
            {
                OnTrigger?.Invoke(thisEntity.Value, otherEntity.Value, triggerEvent);
            }
        }

        _collisionEvents.Clear();
        _triggerEvents.Clear();
    }

    private EntityId? GetEntityFromCollider(ICollider collider)
    {
        foreach (var (entityId, body) in _entityToBody)
        {
            if (body is RigidBody rb && rb.Colliders.Contains(collider))
            {
                return entityId;
            }
        }

        return null;
    }

    #endregion
}
