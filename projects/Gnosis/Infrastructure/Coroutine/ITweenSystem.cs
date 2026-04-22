namespace Gnosis.Infrastructure.Coroutine;

public interface ITweenSystem
{
    ITween To(Action<float> setter, float startValue, float endValue, float duration);
    ITween To(Action<float[]> setter, float[] startValue, float[] endValue, float duration);
    ITweenSequence CreateSequence();
    void KillAll();
    void Update(float delta);
}
