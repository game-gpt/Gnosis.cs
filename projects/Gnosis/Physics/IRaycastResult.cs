namespace Gnosis.Physics;

public interface IRaycastResult
{
    bool HasHit { get; }
    ICollider? Collider { get; }
    float[] Point { get; }
    float[] Normal { get; }
    float Distance { get; }
}

public interface IOverlapResult
{
    IReadOnlyList<ICollider> Colliders { get; }
    int Count { get; }
}
