using Gnosis.Core.Math;

namespace Gnosis.Physics.Shape;

public interface ICollider
{
    string Name { get; }
    bool IsTrigger { get; set; }
    IPhysicsMaterial? Material { get; set; }
    Vector3 Center { get; set; }
    Vector3 Size { get; }
}
