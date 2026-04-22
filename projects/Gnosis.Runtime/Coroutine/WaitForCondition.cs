namespace Gnosis.Runtime.Coroutine;

public class WaitForCondition : IYieldInstruction
{
    private readonly Func<bool> _condition;

    public WaitForCondition(Func<bool> condition)
    {
        _condition = condition;
    }

    public bool IsDone => _condition();

    public void Update(float delta)
    {
    }
}
