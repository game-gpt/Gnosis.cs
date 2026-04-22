namespace Gnosis.Infrastructure.Coroutine;

public class StubTween : ITween
{
    public bool IsPlaying => throw new NotImplementedException("协程系统尚未实现");
    public bool IsComplete => throw new NotImplementedException("协程系统尚未实现");
    public float Duration => throw new NotImplementedException("协程系统尚未实现");
    public float Elapsed => throw new NotImplementedException("协程系统尚未实现");
    public float Progress => throw new NotImplementedException("协程系统尚未实现");
    public void Kill() { throw new NotImplementedException("协程系统尚未实现"); }
    public void Pause() { throw new NotImplementedException("协程系统尚未实现"); }
    public void Play() { throw new NotImplementedException("协程系统尚未实现"); }
    public void Restart() { throw new NotImplementedException("协程系统尚未实现"); }
    public ITween OnComplete(Action callback) { throw new NotImplementedException("协程系统尚未实现"); }
    public ITween OnKill(Action callback) { throw new NotImplementedException("协程系统尚未实现"); }
    public ITween OnStart(Action callback) { throw new NotImplementedException("协程系统尚未实现"); }
    public ITween OnUpdate(Action<float> callback) { throw new NotImplementedException("协程系统尚未实现"); }
    public ITween SetDelay(float delay) { throw new NotImplementedException("协程系统尚未实现"); }
    public ITween SetEase(Easing ease) { throw new NotImplementedException("协程系统尚未实现"); }
    public ITween SetLoops(int loops) { throw new NotImplementedException("协程系统尚未实现"); }
}
