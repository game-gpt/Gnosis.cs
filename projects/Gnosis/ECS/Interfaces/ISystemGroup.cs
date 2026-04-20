using Gnosis.Core.Interfaces;

namespace Gnosis.ECS.Interfaces;

public interface ISystemGroup
{
    string Name { get; }
    IReadOnlyList<ISystem> Systems { get; }
    
    void AddSystem(ISystem system);
    void RemoveSystem(ISystem system);
    
    void Update(float delta);
}
