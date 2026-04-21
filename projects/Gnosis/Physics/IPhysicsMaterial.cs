namespace Gnosis.Physics;

public interface IPhysicsMaterial
{
    string Name { get; }
    float Friction { get; set; }
    float Restitution { get; set; }
    float FrictionCombine { get; set; }
    float RestitutionCombine { get; set; }
}
