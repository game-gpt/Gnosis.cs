namespace Gnosis.Audio.Clip;

public sealed class AudioClip : IAudioClip
{
    #region 字段

    private AudioData? _data;

    #endregion

    #region 属性

    public string Name { get; }

    public float Duration { get; private set; }

    public int Channels { get; private set; }

    public int SampleRate { get; private set; }

    public bool IsLoaded { get; private set; }

    public AudioData? Data => _data;

    #endregion

    #region 构造函数

    public AudioClip(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public AudioClip(string name, AudioData data)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _data = data;
        Channels = data.Channels;
        SampleRate = data.SampleRate;
        Duration = data.SampleCount > 0 ? (float)data.SampleCount / (data.SampleRate * data.Channels) : 0f;
        IsLoaded = true;
    }

    #endregion

    #region IAudioClip 实现

    public void Load()
    {
        if (IsLoaded)
        {
            return;
        }

        IsLoaded = true;
    }

    public void Unload()
    {
        _data = null;
        IsLoaded = false;
    }

    #endregion
}
