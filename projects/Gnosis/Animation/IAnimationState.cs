namespace Gnosis.Animation;

public interface IAnimationState
{
    string Name { get; }
    string ClipName { get; }
    float Speed { get; set; }
    bool IsLooping { get; set; }
    float Duration { get; }
    float NormalizedTime { get; set; }
    bool IsPlaying { get; }
    void Play();
    void Stop();
    void Pause();
}
