namespace Gnosis.Infrastructure.Coroutine;

public class WaitForFrame : IYieldInstruction
{
    private readonly int _frames;
    private int _elapsed;

    public WaitForFrame(int frames = 1)
    {
        _frames = frames;
        _elapsed = 0;
    }

    public bool IsDone => _elapsed >= _frames;

    public void Update(float delta)
    {
        _elapsed++;
    }
}
