using Gnosis.Audio.Clip;
using Gnosis.Audio.Listener;
using Gnosis.Audio.Mixer;
using Gnosis.Audio.Source;

namespace Gnosis.Audio.Driver;

public sealed class AudioSystem : IAudioSystem
{
    #region 字段

    private readonly List<IAudioSource> _sources = new();
    private readonly Dictionary<string, IAudioBus> _buses = new();
    private readonly Dictionary<string, IAudioClip> _clips = new();

    #endregion

    #region 属性

    public IAudioListener Listener { get; } = new AudioListener();

    public IAudioBus MasterBus { get; }

    public float GlobalVolume { get; set; } = 1.0f;

    #endregion

    #region 构造函数

    public AudioSystem()
    {
        MasterBus = new AudioBus("Master");
        _buses["Master"] = MasterBus;
    }

    #endregion

    #region IAudioSystem 实现

    public IAudioSource CreateSource()
    {
        var source = new AudioSource();
        _sources.Add(source);
        return source;
    }

    public void DestroySource(IAudioSource source)
    {
        if (source is AudioSource audioSource)
        {
            audioSource.Stop();
        }

        _sources.Remove(source);
    }

    public IAudioClip LoadClip(string path)
    {
        if (_clips.TryGetValue(path, out var existing))
        {
            return existing;
        }

        var clip = new AudioClip(path);
        _clips[path] = clip;
        return clip;
    }

    public void UnloadClip(string path)
    {
        if (_clips.TryGetValue(path, out var clip))
        {
            clip.Unload();
            _clips.Remove(path);
        }
    }

    public IAudioBus CreateBus(string name, IAudioBus? parent = null)
    {
        if (_buses.ContainsKey(name))
        {
            throw new ArgumentException($"音频总线已存在：{name}", nameof(name));
        }

        var bus = new AudioBus(name, parent);
        _buses[name] = bus;

        if (parent is AudioBus parentBus)
        {
            parentBus.AddChildBus(bus);
        }

        return bus;
    }

    public void DestroyBus(string name)
    {
        if (name == "Master")
        {
            throw new ArgumentException("不能销毁主音频总线", nameof(name));
        }

        if (_buses.TryGetValue(name, out var bus))
        {
            if (bus.Parent is AudioBus parentBus)
            {
                parentBus.RemoveChildBus(name);
            }

            _buses.Remove(name);
        }
    }

    public IAudioBus? GetBus(string name)
    {
        return _buses.GetValueOrDefault(name);
    }

    public void Update(float delta)
    {
        foreach (var source in _sources)
        {
            if (source is AudioSource audioSource)
            {
                audioSource.Update(delta);
            }
        }
    }

    #endregion
}
