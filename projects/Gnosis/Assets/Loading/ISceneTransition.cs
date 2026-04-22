namespace Gnosis.Assets.Loading;

public interface ISceneTransition
{
    string Name { get; }
    float Duration { get; set; }
    bool IsPlaying { get; }
    void Play();
    void Update(float delta);
}
