namespace Gnosis.Core.Thread;

public interface IScheduler
{
    void Update(float deltaTime);

    void FixedUpdate(float fixedDeltaTime);
}
