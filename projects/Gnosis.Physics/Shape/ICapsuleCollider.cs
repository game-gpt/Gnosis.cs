namespace Gnosis.Physics.Shape;

public interface ICapsuleCollider : ICollider
{
    float Radius { get; set; }
    float Height { get; set; }
    int Direction { get; set; }
}
