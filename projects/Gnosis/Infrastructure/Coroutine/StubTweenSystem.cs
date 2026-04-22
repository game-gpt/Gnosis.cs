namespace Gnosis.Infrastructure.Coroutine;

public class StubTweenSystem : ITweenSystem
{
    public ITween To(Action<float> setter, float startValue, float endValue, float duration) { throw new NotImplementedException("协程系统尚未实现"); }
    public ITween To(Action<float[]> setter, float[] startValue, float[] endValue, float duration) { throw new NotImplementedException("协程系统尚未实现"); }
    public ITweenSequence CreateSequence() { throw new NotImplementedException("协程系统尚未实现"); }
    public void KillAll() { throw new NotImplementedException("协程系统尚未实现"); }
    public void Update(float delta) { throw new NotImplementedException("协程系统尚未实现"); }
}
