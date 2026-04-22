namespace Gnosis.Runtime.Coroutine;

public class WaitForSeconds : IYieldInstruction
{
    private readonly float _duration;
    private float _elapsed;

    public WaitForSeconds(float duration)
    {
        _duration = duration;
        _elapsed = 0f;
    }

    public bool IsDone => _elapsed >= _duration;

    public void Update(float delta)
    {
        _elapsed += delta;
    }
}
