using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Physics.Shape;

namespace Gnosis.Physics.Query;

public interface IRaycastResult
{
    bool HasHit { get; }
    ICollider? Collider { get; }
    Vector3 Point { get; }
    Vector3 Normal { get; }
    float Distance { get; }
}

public interface IOverlapResult
{
    IReadOnlyList<ICollider> Colliders { get; }
    int Count { get; }
}
