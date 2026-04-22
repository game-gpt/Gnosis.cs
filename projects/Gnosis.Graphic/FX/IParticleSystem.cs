namespace Gnosis.Graphic.FX;

public interface IParticleSystem
{
    string Name { get; }
    IParticleEmitter Emitter { get; }
    IParticleRenderer Renderer { get; }
    IReadOnlyList<IParticleModule> Modules { get; }
    bool IsPlaying { get; }
    int ActiveParticleCount { get; }
    float Duration { get; set; }
    bool IsLooping { get; set; }
    float PlaybackSpeed { get; set; }
    void AddModule(IParticleModule module);
    void RemoveModule(string name);
    T? GetModule<T>() where T : IParticleModule;
    void Play();
    void Stop();
    void Pause();
    void Update(float delta);
}
