using System.Collections;

namespace Gnosis.Infrastructure.Coroutine;

public interface ICoroutineSystem
{
    ICoroutine Start(IEnumerator routine);
    void Stop(ICoroutine coroutine);
    void StopAll();
    void Update(float delta);
}
