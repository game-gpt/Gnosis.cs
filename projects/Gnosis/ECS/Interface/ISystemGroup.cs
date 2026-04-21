using Gnosis.ECS.Core;

namespace Gnosis.ECS.Interface;

public interface ISystemGroup
{
    string Name { get; }
    IReadOnlyList<ISystem> Systems { get; }
    
    void AddSystem(ISystem system);
    void RemoveSystem(ISystem system);
    
    void Update(float delta);
}
