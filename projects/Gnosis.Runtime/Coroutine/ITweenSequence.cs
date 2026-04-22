namespace Gnosis.Runtime.Coroutine;

public interface ITweenSequence
{
    bool IsPlaying { get; }
    ITweenSequence Append(ITween tween);
    ITweenSequence Join(ITween tween);
    ITweenSequence Insert(float time, ITween tween);
    ITweenSequence OnComplete(Action callback);
    void Play();
    void Pause();
    void Restart();
    void Kill();
}
