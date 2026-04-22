namespace Gnosis.Runtime.Coroutine;

public interface IYieldInstruction
{
    bool IsDone { get; }
    void Update(float delta);
}
