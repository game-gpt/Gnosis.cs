using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.BroadPhase;

internal struct SpatialProxy : IEquatable<SpatialProxy>
{
    public IRigidBody Body;
    public BoundingBox Aabb;

    public SpatialProxy(IRigidBody body, BoundingBox aabb)
    {
        Body = body;
        Aabb = aabb;
    }

    public bool Equals(SpatialProxy other)
    {
        return ReferenceEquals(Body, other.Body);
    }

    public override bool Equals(object? obj)
    {
        return obj is SpatialProxy other && Equals(other);
    }

    public override int GetHashCode()
    {
        return RuntimeHelpers.GetHashCode(Body);
    }
}
