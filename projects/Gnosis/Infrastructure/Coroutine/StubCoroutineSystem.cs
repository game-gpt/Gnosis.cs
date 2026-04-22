using System.Collections;

namespace Gnosis.Infrastructure.Coroutine;

public class StubCoroutineSystem : ICoroutineSystem
{
    public ICoroutine Start(IEnumerator routine) { throw new NotImplementedException("协程系统尚未实现"); }
    public void Stop(ICoroutine coroutine) { throw new NotImplementedException("协程系统尚未实现"); }
    public void StopAll() { throw new NotImplementedException("协程系统尚未实现"); }
    public void Update(float delta) { throw new NotImplementedException("协程系统尚未实现"); }
}
