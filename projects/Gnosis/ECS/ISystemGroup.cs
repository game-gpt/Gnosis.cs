using Gnosis.Core;

namespace Gnosis.ECS;

public interface ISystemGroup
{
    string Name { get; }
    IReadOnlyList<ISystem> Systems { get; }
    
    void AddSystem(ISystem system);
    void RemoveSystem(ISystem system);
    
    void Update(float delta);
}
