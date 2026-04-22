namespace Gnosis.Infrastructure.Coroutine;

public class StubCoroutine : ICoroutine
{
    public bool IsRunning => throw new NotImplementedException("协程系统尚未实现");
    public bool IsPaused => throw new NotImplementedException("协程系统尚未实现");
    public bool IsComplete => throw new NotImplementedException("协程系统尚未实现");
    public void Pause() { throw new NotImplementedException("协程系统尚未实现"); }
    public void Resume() { throw new NotImplementedException("协程系统尚未实现"); }
    public void Stop() { throw new NotImplementedException("协程系统尚未实现"); }
}
