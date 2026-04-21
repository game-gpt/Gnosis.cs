using Gnosis.Audio.Interface;

namespace Gnosis.Audio.Implementation;

public class StubAudioSystem : IAudioSystem
{
    public IAudioListener Listener => throw new NotImplementedException("音频系统尚未实现");
    public IAudioBus MasterBus => throw new NotImplementedException("音频系统尚未实现");
    public float GlobalVolume { get => throw new NotImplementedException("音频系统尚未实现"); set => throw new NotImplementedException("音频系统尚未实现"); }

    public IAudioBus CreateBus(string name, IAudioBus? parent = null)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public IAudioSource CreateSource()
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void DestroyBus(string name)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void DestroySource(IAudioSource source)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public IAudioBus? GetBus(string name)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public IAudioClip LoadClip(string path)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void UnloadClip(string path)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }

    public void Update(float delta)
    {
        throw new NotImplementedException("音频系统尚未实现");
    }
}
