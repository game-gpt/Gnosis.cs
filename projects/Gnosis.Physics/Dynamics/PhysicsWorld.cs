using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Physics.BroadPhase;
using Gnosis.Physics.Collision;
using Gnosis.Physics.NarrowPhase;
using Gnosis.Physics.Query;
using Gnosis.Physics.Shape;
using Gnosis.Physics.Solver;

namespace Gnosis.Physics.Dynamics;

public sealed class PhysicsWorld : IPhysicsWorld
{
    #region 字段

    private readonly List<IRigidBody> _bodies = new();
    private readonly Dictionary<string, IRigidBody> _bodiesByName = new();
    private readonly SpatialHashGrid _broadPhase;
    private readonly CollisionDetector _narrowPhase = new();
    private readonly ImpulseSolver _solver = new();
    private readonly List<(IRigidBody, IRigidBody)> _broadPhasePairs = new();
    private readonly List<ContactPoint> _contactPoints = new();
    private readonly List<ICollisionEvent> _collisionEvents = new();
    private readonly List<ITriggerEvent> _triggerEvents = new();
    private readonly Dictionary<(IRigidBody, IRigidBody), bool> _previousCollisionState = new();

    #endregion

    #region 属性

    public float FixedDeltaTime { get; set; } = 1f / 60f;

    public Vector3 Gravity { get; set; } = new(0f, -9.81f, 0f);

    public int BodyCount => _bodies.Count;

    public IReadOnlyList<ICollisionEvent> CollisionEvents => _collisionEvents;

    public IReadOnlyList<ITriggerEvent> TriggerEvents => _triggerEvents;

    #endregion

    #region 构造函数

    public PhysicsWorld()
    {
        _broadPhase = new SpatialHashGrid(cellSize: 4.0f);
    }

    public PhysicsWorld(float cellSize)
    {
        _broadPhase = new SpatialHashGrid(cellSize);
    }

    #endregion

    #region IPhysicsWorld 实现

    public IRigidBody CreateRigidBody(string name, RigidBodyType type)
    {
        var body = new RigidBody(name, type);
        _bodies.Add(body);
        _bodiesByName[name] = body;
        _broadPhase.Insert(body);
        return body;
    }

    public void DestroyRigidBody(IRigidBody body)
    {
        _broadPhase.Remove(body);
        _bodies.Remove(body);

        if (body is RigidBody rb)
        {
            _bodiesByName.Remove(rb.Name);
        }

        CleanupCollisionState(body);
    }

    public IBoxCollider CreateBoxCollider(string name)
    {
        return new BoxCollider(name);
    }

    public ISphereCollider CreateSphereCollider(string name)
    {
        return new SphereCollider(name);
    }

    public ICapsuleCollider CreateCapsuleCollider(string name)
    {
        return new CapsuleCollider(name);
    }

    public IMeshCollider CreateMeshCollider(string name)
    {
        return new MeshCollider(name);
    }

    public void AttachCollider(IRigidBody body, ICollider collider)
    {
        if (body is RigidBody rb)
        {
            rb.AddCollider(collider);
            _broadPhase.Update(body);
        }
    }

    public void DetachCollider(IRigidBody body, ICollider collider)
    {
        if (body is RigidBody rb)
        {
            rb.RemoveCollider(collider);
            _broadPhase.Update(body);
        }
    }

    public IRaycastResult Raycast(Vector3 origin, Vector3 direction, float maxDistance)
    {
        var candidates = new List<IRigidBody>();
        _broadPhase.QueryRay(origin, direction, maxDistance, candidates);

        IRaycastResult? closest = null;
        var closestDist = maxDistance;

        foreach (var body in candidates)
        {
            var result = RaycastBody(body, origin, direction, maxDistance);

            if (result != null && result.Distance < closestDist)
            {
                closest = result;
                closestDist = result.Distance;
            }
        }

        return closest ?? new RaycastResult();
    }

    public IRaycastResult[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance)
    {
        var candidates = new List<IRigidBody>();
        _broadPhase.QueryRay(origin, direction, maxDistance, candidates);

        var results = new List<IRaycastResult>();

        foreach (var body in candidates)
        {
            var result = RaycastBody(body, origin, direction, maxDistance);

            if (result != null)
            {
                results.Add(result);
            }
        }

        results.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return results.ToArray();
    }

    public IOverlapResult OverlapSphere(Vector3 center, float radius)
    {
        var aabb = new BoundingBox(
            center - new Vector3(radius),
            center + new Vector3(radius));

        var candidates = new List<IRigidBody>();
        _broadPhase.QueryAabb(aabb, candidates);

        var result = new OverlapResult();
        var sphere = new BoundingSphere(center, radius);

        foreach (var body in candidates)
        {
            if (body.Colliders == null)
            {
                continue;
            }

            foreach (var collider in body.Colliders)
            {
                if (OverlapCollider(sphere, body.Position, collider))
                {
                    result.AddCollider(collider);
                    break;
                }
            }
        }

        return result;
    }

    public IOverlapResult OverlapBox(Vector3 center, Vector3 halfExtents)
    {
        var aabb = new BoundingBox(center - halfExtents, center + halfExtents);

        var candidates = new List<IRigidBody>();
        _broadPhase.QueryAabb(aabb, candidates);

        var result = new OverlapResult();

        foreach (var body in candidates)
        {
            if (body.Colliders == null)
            {
                continue;
            }

            foreach (var collider in body.Colliders)
            {
                if (OverlapCollider(aabb, body.Position, collider))
                {
                    result.AddCollider(collider);
                    break;
                }
            }
        }

        return result;
    }

    public void Step(float delta)
    {
        _collisionEvents.Clear();
        _triggerEvents.Clear();

        foreach (var body in _bodies)
        {
            if (body is RigidBody rb)
            {
                rb.Integrate(delta, Gravity);
            }
        }

        _broadPhase.UpdateAll();

        _broadPhasePairs.Clear();
        _broadPhase.QueryPairs(_broadPhasePairs);

        var currentCollisionState = new Dictionary<(IRigidBody, IRigidBody), bool>();

        foreach (var (bodyA, bodyB) in _broadPhasePairs)
        {
            _contactPoints.Clear();

            if (!_narrowPhase.Detect(bodyA, bodyB, _contactPoints))
            {
                continue;
            }

            var hasTrigger = HasTriggerCollider(bodyA) || HasTriggerCollider(bodyB);

            if (hasTrigger)
            {
                var triggerA = FindTriggerCollider(bodyA) ?? bodyA.Colliders![0];
                var triggerB = FindTriggerCollider(bodyB) ?? bodyB.Colliders![0];

                var key = MakeKey(bodyA, bodyB);
                currentCollisionState[key] = true;

                if (!_previousCollisionState.TryGetValue(key, out var wasColliding) || !wasColliding)
                {
                    _triggerEvents.Add(new TriggerEvent(triggerA, triggerB));
                }
            }
            else
            {
                foreach (var contact in _contactPoints)
                {
                    _solver.Solve(contact, bodyA, bodyB);
                }

                var primaryContact = _contactPoints[0];
                var colliderA = bodyA.Colliders![0];
                var colliderB = bodyB.Colliders![0];
                var relativeVel = Vector3.Dot(bodyA.Velocity - bodyB.Velocity, primaryContact.Normal);

                var key = MakeKey(bodyA, bodyB);
                currentCollisionState[key] = true;

                if (!_previousCollisionState.TryGetValue(key, out var wasColliding) || !wasColliding)
                {
                    _collisionEvents.Add(new CollisionEvent(
                        colliderA, colliderB,
                        primaryContact.Point,
                        primaryContact.Normal,
                        primaryContact.Penetration,
                        MathF.Abs(relativeVel)));
                }
            }
        }

        _previousCollisionState.Clear();
        foreach (var kvp in currentCollisionState)
        {
            _previousCollisionState[kvp.Key] = kvp.Value;
        }
    }

    public void SyncTransforms()
    {
    }

    internal void StepSimd(float delta, SimdIntegrationBatch batch)
    {
        _collisionEvents.Clear();
        _triggerEvents.Clear();

        batch.Prepare(_bodies);
        batch.Integrate(delta, Gravity);
        batch.WriteBack(_bodies);

        _broadPhase.UpdateAll();

        _broadPhasePairs.Clear();
        _broadPhase.QueryPairs(_broadPhasePairs);

        var currentCollisionState = new Dictionary<(IRigidBody, IRigidBody), bool>();

        foreach (var (bodyA, bodyB) in _broadPhasePairs)
        {
            _contactPoints.Clear();

            if (!_narrowPhase.Detect(bodyA, bodyB, _contactPoints))
            {
                continue;
            }

            var hasTrigger = HasTriggerCollider(bodyA) || HasTriggerCollider(bodyB);

            if (hasTrigger)
            {
                var triggerA = FindTriggerCollider(bodyA) ?? bodyA.Colliders![0];
                var triggerB = FindTriggerCollider(bodyB) ?? bodyB.Colliders![0];

                var key = MakeKey(bodyA, bodyB);
                currentCollisionState[key] = true;

                if (!_previousCollisionState.TryGetValue(key, out var wasColliding) || !wasColliding)
                {
                    _triggerEvents.Add(new TriggerEvent(triggerA, triggerB));
                }
            }
            else
            {
                foreach (var contact in _contactPoints)
                {
                    _solver.Solve(contact, bodyA, bodyB);
                }

                var primaryContact = _contactPoints[0];
                var colliderA = bodyA.Colliders![0];
                var colliderB = bodyB.Colliders![0];
                var relativeVel = Vector3.Dot(bodyA.Velocity - bodyB.Velocity, primaryContact.Normal);

                var key = MakeKey(bodyA, bodyB);
                currentCollisionState[key] = true;

                if (!_previousCollisionState.TryGetValue(key, out var wasColliding) || !wasColliding)
                {
                    _collisionEvents.Add(new CollisionEvent(
                        colliderA, colliderB,
                        primaryContact.Point,
                        primaryContact.Normal,
                        primaryContact.Penetration,
                        MathF.Abs(relativeVel)));
                }
            }
        }

        _previousCollisionState.Clear();
        foreach (var kvp in currentCollisionState)
        {
            _previousCollisionState[kvp.Key] = kvp.Value;
        }
    }

    #endregion

    #region 私有方法 - 射线检测

    private static IRaycastResult? RaycastBody(IRigidBody body, Vector3 origin, Vector3 direction, float maxDistance)
    {
        if (body.Colliders == null || body.Colliders.Count == 0)
        {
            return null;
        }

        IRaycastResult? closest = null;
        var closestDist = maxDistance;

        foreach (var collider in body.Colliders)
        {
            var result = RaycastCollider(body.Position, collider, origin, direction, maxDistance);

            if (result != null && result.Distance < closestDist)
            {
                closest = result;
                closestDist = result.Distance;
            }
        }

        return closest;
    }

    private static IRaycastResult? RaycastCollider(
        Vector3 bodyPos, ICollider collider,
        Vector3 origin, Vector3 direction, float maxDistance)
    {
        var ray = new Ray(origin, direction);

        return collider switch
        {
            ISphereCollider sphere => RaycastSphere(bodyPos + sphere.Center, sphere.Radius, ray, maxDistance, collider),
            IBoxCollider box => RaycastBox(bodyPos + box.Center, new Vector3(box.HalfExtentsX, box.HalfExtentsY, box.HalfExtentsZ), ray, maxDistance, collider),
            _ => null
        };
    }

    private static IRaycastResult? RaycastSphere(Vector3 center, float radius, Ray ray, float maxDistance, ICollider collider)
    {
        var diff = ray.Origin - center;
        var b = Vector3.Dot(diff, ray.Direction);
        var c = Vector3.Dot(diff, diff) - radius * radius;

        if (c > 0f && b > 0f)
        {
            return null;
        }

        var discriminant = b * b - c;
        if (discriminant < 0f)
        {
            return null;
        }

        var sqrtD = MathF.Sqrt(discriminant);
        var t = -b - sqrtD;

        if (t < 0f)
        {
            t = -b + sqrtD;
        }

        if (t < 0f || t > maxDistance)
        {
            return null;
        }

        var point = ray.GetPoint(t);
        var normal = Vector3.Normalize(point - center);
        return new RaycastResult(collider, point, normal, t);
    }

    private static IRaycastResult? RaycastBox(Vector3 center, Vector3 halfExtents, Ray ray, float maxDistance, ICollider collider)
    {
        var min = center - halfExtents;
        var max = center + halfExtents;
        var box = new BoundingBox(min, max);

        var hit = ray.Intersects(box);
        if (hit == null || hit.Value > maxDistance || hit.Value < 0f)
        {
            return null;
        }

        var t = hit.Value;
        var point = ray.GetPoint(t);

        var local = point - center;
        var normal = ComputeBoxNormal(local, halfExtents);

        return new RaycastResult(collider, point, normal, t);
    }

    private static Vector3 ComputeBoxNormal(Vector3 localPoint, Vector3 halfExtents)
    {
        var ratio = new Vector3(
            halfExtents.X > 0f ? localPoint.X / halfExtents.X : 0f,
            halfExtents.Y > 0f ? localPoint.Y / halfExtents.Y : 0f,
            halfExtents.Z > 0f ? localPoint.Z / halfExtents.Z : 0f);

        var absRatio = Vector3.Abs(ratio);

        if (absRatio.X >= absRatio.Y && absRatio.X >= absRatio.Z)
        {
            return new Vector3(Math.Sign(localPoint.X), 0f, 0f);
        }

        if (absRatio.Y >= absRatio.X && absRatio.Y >= absRatio.Z)
        {
            return new Vector3(0f, Math.Sign(localPoint.Y), 0f);
        }

        return new Vector3(0f, 0f, Math.Sign(localPoint.Z));
    }

    #endregion

    #region 私有方法 - 重叠检测

    private static bool OverlapCollider(BoundingSphere sphere, Vector3 bodyPos, ICollider collider)
    {
        return collider switch
        {
            ISphereCollider s => sphere.Intersects(new BoundingSphere(bodyPos + s.Center, s.Radius)),
            IBoxCollider b => sphere.Intersects(new BoundingBox(
                bodyPos + b.Center - new Vector3(b.HalfExtentsX, b.HalfExtentsY, b.HalfExtentsZ),
                bodyPos + b.Center + new Vector3(b.HalfExtentsX, b.HalfExtentsY, b.HalfExtentsZ))),
            _ => false
        };
    }

    private static bool OverlapCollider(BoundingBox box, Vector3 bodyPos, ICollider collider)
    {
        return collider switch
        {
            ISphereCollider s => new BoundingSphere(bodyPos + s.Center, s.Radius).Intersects(box),
            IBoxCollider b => box.Intersects(new BoundingBox(
                bodyPos + b.Center - new Vector3(b.HalfExtentsX, b.HalfExtentsY, b.HalfExtentsZ),
                bodyPos + b.Center + new Vector3(b.HalfExtentsX, b.HalfExtentsY, b.HalfExtentsZ))),
            _ => false
        };
    }

    #endregion

    #region 私有方法 - 碰撞状态

    private static bool HasTriggerCollider(IRigidBody body)
    {
        if (body.Colliders == null)
        {
            return false;
        }

        foreach (var collider in body.Colliders)
        {
            if (collider.IsTrigger)
            {
                return true;
            }
        }

        return false;
    }

    private static ICollider? FindTriggerCollider(IRigidBody body)
    {
        if (body.Colliders == null)
        {
            return null;
        }

        foreach (var collider in body.Colliders)
        {
            if (collider.IsTrigger)
            {
                return collider;
            }
        }

        return null;
    }

    private static (IRigidBody, IRigidBody) MakeKey(IRigidBody a, IRigidBody b)
    {
        return ReferenceEquals(a, b) ? (a, b) :
            string.CompareOrdinal(a.Name, b.Name) < 0 ? (a, b) : (b, a);
    }

    private void CleanupCollisionState(IRigidBody body)
    {
        var keysToRemove = new List<(IRigidBody, IRigidBody)>();

        foreach (var key in _previousCollisionState.Keys)
        {
            if (ReferenceEquals(key.Item1, body) || ReferenceEquals(key.Item2, body))
            {
                keysToRemove.Add(key);
            }
        }

        foreach (var key in keysToRemove)
        {
            _previousCollisionState.Remove(key);
        }
    }

    #endregion
}
