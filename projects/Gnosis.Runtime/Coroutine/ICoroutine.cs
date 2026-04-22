namespace Gnosis.Runtime.Coroutine;

public interface ICoroutine
{
    bool IsRunning { get; }
    bool IsPaused { get; }
    bool IsComplete { get; }
    void Pause();
    void Resume();
    void Stop();
}
