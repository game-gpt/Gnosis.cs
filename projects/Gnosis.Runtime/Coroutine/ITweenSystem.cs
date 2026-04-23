using System.Numerics;

namespace Gnosis.Runtime.Coroutine;

public interface ITweenSystem
{
    ITween To(Action<float> setter, float startValue, float endValue, float duration);
    ITween To(Action<Vector3> setter, Vector3 startValue, Vector3 endValue, float duration);
    ITweenSequence CreateSequence();
    void KillAll();
    void Update(float delta);
}
