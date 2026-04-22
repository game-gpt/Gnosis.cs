using Gnosis.Audio.Clip;

namespace Gnosis.Audio.Bank;

public sealed class AudioBank
{
    #region 字段

    private readonly Dictionary<string, IAudioClip> _clips = new();
    private bool _isLoaded;

    #endregion

    #region 属性

    public string Name { get; }

    public bool IsLoaded => _isLoaded;

    public int ClipCount => _clips.Count;

    #endregion

    #region 构造函数

    public AudioBank(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    #endregion

    #region 公开方法

    public void AddClip(string clipName, IAudioClip clip)
    {
        if (clip is null)
        {
            throw new ArgumentNullException(nameof(clip));
        }

        _clips[clipName] = clip;
    }

    public IAudioClip? GetClip(string clipName)
    {
        return _clips.GetValueOrDefault(clipName);
    }

    public void RemoveClip(string clipName)
    {
        _clips.Remove(clipName);
    }

    public void LoadAll()
    {
        foreach (var clip in _clips.Values)
        {
            clip.Load();
        }

        _isLoaded = true;
    }

    public void UnloadAll()
    {
        foreach (var clip in _clips.Values)
        {
            clip.Unload();
        }

        _isLoaded = false;
    }

    #endregion
}
