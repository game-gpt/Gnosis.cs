namespace Gnosis.Infrastructure.Coroutine;

public interface IYieldInstruction
{
    bool IsDone { get; }
    void Update(float delta);
}
