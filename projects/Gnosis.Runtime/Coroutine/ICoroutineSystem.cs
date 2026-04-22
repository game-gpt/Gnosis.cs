using System.Collections;

namespace Gnosis.Runtime.Coroutine;

public interface ICoroutineSystem
{
    ICoroutine Start(IEnumerator routine);
    void Stop(ICoroutine coroutine);
    void StopAll();
    void Update(float delta);
}
