namespace Gnosis.ECS.Core;

public interface ISystem
{
    SystemPhase Phase { get; }
    void Initialize();
    void Update(float delta);
    void Shutdown();
}
