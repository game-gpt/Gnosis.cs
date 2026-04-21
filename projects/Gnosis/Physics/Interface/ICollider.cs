namespace Gnosis.Physics.Interface;

public interface ICollider
{
    string Name { get; }
    bool IsTrigger { get; set; }
    IPhysicsMaterial? Material { get; set; }
    float[] Center { get; set; }
    float[] Size { get; }
}
