namespace Gnosis.Physics.Shape;

public interface IBoxCollider : ICollider
{
    float HalfExtentsX { get; set; }
    float HalfExtentsY { get; set; }
    float HalfExtentsZ { get; set; }
}
