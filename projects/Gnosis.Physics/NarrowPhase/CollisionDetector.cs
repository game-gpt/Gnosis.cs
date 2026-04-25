using System.Numerics;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.NarrowPhase;

public readonly struct ContactPoint
{
    public readonly Vector3 Point;
    public readonly Vector3 Normal;
    public readonly float Penetration;

    public ContactPoint(Vector3 point, Vector3 normal, float penetration)
    {
        Point = point;
        Normal = normal;
        Penetration = penetration;
    }
}

public sealed class CollisionDetector
{
    #region 公开方法

    public bool Detect(IRigidBody bodyA, IRigidBody bodyB, List<ContactPoint> contacts)
    {
        var collidersA = bodyA.Colliders;
        var collidersB = bodyB.Colliders;

        if (collidersA == null || collidersB == null || collidersA.Count == 0 || collidersB.Count == 0)
        {
            return false;
        }

        var found = false;

        foreach (var colliderA in collidersA)
        {
            foreach (var colliderB in collidersB)
            {
                if (DetectPair(bodyA.Position, colliderA, bodyB.Position, colliderB, contacts))
                {
                    found = true;
                }
            }
        }

        return found;
    }

    #endregion

    #region 私有方法 - 碰撞对分发

    private static bool DetectPair(
        Vector3 posA, ICollider colA,
        Vector3 posB, ICollider colB,
        List<ContactPoint> contacts)
    {
        return (colA, colB) switch
        {
            (ISphereCollider sa, ISphereCollider sb) => SphereVsSphere(posA, sa, posB, sb, contacts),
            (ISphereCollider s, IBoxCollider b) => SphereVsBox(posA, s, posB, b, contacts),
            (IBoxCollider b, ISphereCollider s) => SphereVsBox(posB, s, posA, b, contacts, true),
            (IBoxCollider ba, IBoxCollider bb) => BoxVsBox(posA, ba, posB, bb, contacts),
            (ISphereCollider s, ICapsuleCollider c) => SphereVsCapsule(posA, s, posB, c, contacts),
            (ICapsuleCollider c, ISphereCollider s) => SphereVsCapsule(posB, s, posA, c, contacts, true),
            _ => false
        };
    }

    #endregion

    #region 私有方法 - Sphere vs Sphere

    private static bool SphereVsSphere(
        Vector3 posA, ISphereCollider sphereA,
        Vector3 posB, ISphereCollider sphereB,
        List<ContactPoint> contacts)
    {
        var centerA = posA + sphereA.Center;
        var centerB = posB + sphereB.Center;

        var diff = centerB - centerA;
        var distSq = diff.LengthSquared();
        var radiusSum = sphereA.Radius + sphereB.Radius;

        if (distSq > radiusSum * radiusSum)
        {
            return false;
        }

        var distance = MathF.Sqrt(distSq);
        Vector3 normal;

        if (distance < 0.0001f)
        {
            normal = Vector3.UnitY;
        }
        else
        {
            normal = diff / distance;
        }

        var penetration = radiusSum - distance;
        var point = centerA + normal * (sphereA.Radius - penetration * 0.5f);

        contacts.Add(new ContactPoint(point, normal, penetration));
        return true;
    }

    #endregion

    #region 私有方法 - Sphere vs Box

    private static bool SphereVsBox(
        Vector3 spherePos, ISphereCollider sphere,
        Vector3 boxPos, IBoxCollider box,
        List<ContactPoint> contacts,
        bool flipNormal = false)
    {
        var sphereCenter = spherePos + sphere.Center;
        var boxCenter = boxPos + box.Center;
        var halfExtents = new Vector3(box.HalfExtentsX, box.HalfExtentsY, box.HalfExtentsZ);

        var local = sphereCenter - boxCenter;
        var clamped = Vector3.Clamp(local, -halfExtents, halfExtents);
        var diff = local - clamped;
        var distSq = diff.LengthSquared();

        if (distSq > sphere.Radius * sphere.Radius)
        {
            return false;
        }

        Vector3 normal;
        float penetration;

        if (distSq < 0.0001f)
        {
            var absLocal = Vector3.Abs(local);
            var minAxis = FindMinAxis(absLocal, halfExtents);

            normal = -Vector3.Normalize(local);
            normal = SetAxis(normal, minAxis, Math.Sign(GetAxis(local, minAxis)));

            var distToEdge = GetAxis(halfExtents, minAxis) - Math.Abs(GetAxis(local, minAxis));
            penetration = distToEdge + sphere.Radius;
        }
        else
        {
            var distance = MathF.Sqrt(distSq);
            normal = diff / distance;
            penetration = sphere.Radius - distance;
        }

        if (flipNormal)
        {
            normal = -normal;
        }

        var contactPoint = sphereCenter - normal * (sphere.Radius - penetration * 0.5f);
        contacts.Add(new ContactPoint(contactPoint, normal, penetration));
        return true;
    }

    #endregion

    #region 私有方法 - Box vs Box (SAT)

    private static bool BoxVsBox(
        Vector3 posA, IBoxCollider boxA,
        Vector3 posB, IBoxCollider boxB,
        List<ContactPoint> contacts)
    {
        var centerA = posA + boxA.Center;
        var centerB = posB + boxB.Center;
        var halfA = new Vector3(boxA.HalfExtentsX, boxA.HalfExtentsY, boxA.HalfExtentsZ);
        var halfB = new Vector3(boxB.HalfExtentsX, boxB.HalfExtentsY, boxB.HalfExtentsZ);

        var diff = centerB - centerA;
        var overlapX = halfA.X + halfB.X - MathF.Abs(diff.X);
        var overlapY = halfA.Y + halfB.Y - MathF.Abs(diff.Y);
        var overlapZ = halfA.Z + halfB.Z - MathF.Abs(diff.Z);

        if (overlapX <= 0f || overlapY <= 0f || overlapZ <= 0f)
        {
            return false;
        }

        Vector3 normal;
        float penetration;

        if (overlapX <= overlapY && overlapX <= overlapZ)
        {
            penetration = overlapX;
            normal = diff.X > 0f ? Vector3.UnitX : -Vector3.UnitX;
        }
        else if (overlapY <= overlapX && overlapY <= overlapZ)
        {
            penetration = overlapY;
            normal = diff.Y > 0f ? Vector3.UnitY : -Vector3.UnitY;
        }
        else
        {
            penetration = overlapZ;
            normal = diff.Z > 0f ? Vector3.UnitZ : -Vector3.UnitZ;
        }

        var contactPoint = (centerA + centerB) * 0.5f;
        contacts.Add(new ContactPoint(contactPoint, normal, penetration));
        return true;
    }

    #endregion

    #region 私有方法 - Sphere vs Capsule

    private static bool SphereVsCapsule(
        Vector3 spherePos, ISphereCollider sphere,
        Vector3 capsulePos, ICapsuleCollider capsule,
        List<ContactPoint> contacts,
        bool flipNormal = false)
    {
        var sphereCenter = spherePos + sphere.Center;
        var capsuleCenter = capsulePos + capsule.Center;

        var (capsuleA, capsuleB) = GetCapsuleEndpoints(capsule, capsuleCenter);

        var closest = ClosestPointOnSegment(sphereCenter, capsuleA, capsuleB);
        var diff = sphereCenter - closest;
        var distSq = diff.LengthSquared();
        var radiusSum = sphere.Radius + capsule.Radius;

        if (distSq > radiusSum * radiusSum)
        {
            return false;
        }

        var distance = MathF.Sqrt(distSq);
        Vector3 normal;

        if (distance < 0.0001f)
        {
            normal = Vector3.UnitY;
        }
        else
        {
            normal = diff / distance;
        }

        var penetration = radiusSum - distance;

        if (flipNormal)
        {
            normal = -normal;
        }

        var contactPoint = sphereCenter - normal * (sphere.Radius - penetration * 0.5f);
        contacts.Add(new ContactPoint(contactPoint, normal, penetration));
        return true;
    }

    #endregion

    #region 私有方法 - 辅助

    private static (Vector3, Vector3) GetCapsuleEndpoints(ICapsuleCollider capsule, Vector3 center)
    {
        var halfHeight = capsule.Height * 0.5f - capsule.Radius;

        if (halfHeight < 0f)
        {
            halfHeight = 0f;
        }

        return capsule.Direction switch
        {
            0 => (center - Vector3.UnitX * halfHeight, center + Vector3.UnitX * halfHeight),
            2 => (center - Vector3.UnitZ * halfHeight, center + Vector3.UnitZ * halfHeight),
            _ => (center - Vector3.UnitY * halfHeight, center + Vector3.UnitY * halfHeight)
        };
    }

    private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        var ab = b - a;
        var abSq = ab.LengthSquared();

        if (abSq < 0.0001f)
        {
            return a;
        }

        var t = Vector3.Dot(point - a, ab) / abSq;
        t = Math.Clamp(t, 0f, 1f);
        return a + ab * t;
    }

    private static int FindMinAxis(Vector3 absLocal, Vector3 halfExtents)
    {
        var distX = halfExtents.X - absLocal.X;
        var distY = halfExtents.Y - absLocal.Y;
        var distZ = halfExtents.Z - absLocal.Z;

        if (distX <= distY && distX <= distZ)
        {
            return 0;
        }

        return distY <= distZ ? 1 : 2;
    }

    private static float GetAxis(Vector3 v, int axis)
    {
        return axis switch
        {
            0 => v.X,
            1 => v.Y,
            _ => v.Z
        };
    }

    private static Vector3 SetAxis(Vector3 v, int axis, float value)
    {
        return axis switch
        {
            0 => new Vector3(value, v.Y, v.Z),
            1 => new Vector3(v.X, value, v.Z),
            _ => new Vector3(v.X, v.Y, value)
        };
    }

    #endregion
}
