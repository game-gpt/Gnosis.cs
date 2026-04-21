using Gnosis.Core;

namespace Gnosis.ECS;

public interface ISystemScheduler
{
    void RegisterSystem(ISystem system);
    void UnregisterSystem(ISystem system);

    void Update(float delta);

    void EnableSystem(ISystem system);
    void DisableSystem(ISystem system);
}
