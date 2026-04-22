namespace Gnosis.Runtime.Coroutine;

public interface ITween
{
    bool IsPlaying { get; }
    bool IsComplete { get; }
    float Duration { get; }
    float Elapsed { get; }
    float Progress { get; }
    ITween SetDelay(float delay);
    ITween SetEase(Easing ease);
    ITween SetLoops(int loops);
    ITween OnStart(Action callback);
    ITween OnComplete(Action callback);
    ITween OnUpdate(Action<float> callback);
    ITween OnKill(Action callback);
    void Play();
    void Pause();
    void Restart();
    void Kill();
}
