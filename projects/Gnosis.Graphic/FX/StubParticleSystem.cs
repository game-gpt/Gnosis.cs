namespace Gnosis.Graphic.FX;

public class StubParticleSystem : IParticleSystem
{
    public string Name => throw new NotImplementedException("粒子系统尚未实现");
    public IParticleEmitter Emitter => throw new NotImplementedException("粒子系统尚未实现");
    public IParticleRenderer Renderer => throw new NotImplementedException("粒子系统尚未实现");
    public IReadOnlyList<IParticleModule> Modules => throw new NotImplementedException("粒子系统尚未实现");
    public bool IsPlaying => throw new NotImplementedException("粒子系统尚未实现");
    public int ActiveParticleCount => throw new NotImplementedException("粒子系统尚未实现");
    public float Duration { get => throw new NotImplementedException("粒子系统尚未实现"); set => throw new NotImplementedException("粒子系统尚未实现"); }
    public bool IsLooping { get => throw new NotImplementedException("粒子系统尚未实现"); set => throw new NotImplementedException("粒子系统尚未实现"); }
    public float PlaybackSpeed { get => throw new NotImplementedException("粒子系统尚未实现"); set => throw new NotImplementedException("粒子系统尚未实现"); }

    public void AddModule(IParticleModule module) { throw new NotImplementedException("粒子系统尚未实现"); }
    public T? GetModule<T>() where T : IParticleModule { throw new NotImplementedException("粒子系统尚未实现"); }
    public void Pause() { throw new NotImplementedException("粒子系统尚未实现"); }
    public void Play() { throw new NotImplementedException("粒子系统尚未实现"); }
    public void RemoveModule(string name) { throw new NotImplementedException("粒子系统尚未实现"); }
    public void Stop() { throw new NotImplementedException("粒子系统尚未实现"); }
    public void Update(float delta) { throw new NotImplementedException("粒子系统尚未实现"); }
}
