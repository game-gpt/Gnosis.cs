namespace Gnosis.Audio.Mixer;

public sealed class AudioBus : IAudioBus
{
    #region 字段

    private readonly List<IAudioBus> _children = new();
    private readonly List<IAudioEffect> _effects = new();

    #endregion

    #region 属性

    public string Name { get; }

    public float Volume { get; set; } = 1.0f;

    public bool Mute { get; set; }

    public bool BypassEffects { get; set; }

    public IAudioBus? Parent { get; }

    public IReadOnlyList<IAudioBus> Children => _children;

    public IReadOnlyList<IAudioEffect> Effects => _effects;

    #endregion

    #region 构造函数

    public AudioBus(string name, IAudioBus? parent = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Parent = parent;
    }

    #endregion

    #region IAudioBus 实现

    public void AddEffect(IAudioEffect effect)
    {
        if (effect is null)
        {
            throw new ArgumentNullException(nameof(effect));
        }

        _effects.Add(effect);
    }

    public void RemoveEffect(int index)
    {
        if (index < 0 || index >= _effects.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"效果器索引超出范围：{index}");
        }

        _effects.RemoveAt(index);
    }

    public void AddChildBus(IAudioBus bus)
    {
        if (bus is null)
        {
            throw new ArgumentNullException(nameof(bus));
        }

        _children.Add(bus);
    }

    public void RemoveChildBus(string name)
    {
        for (var i = 0; i < _children.Count; i++)
        {
            if (_children[i].Name == name)
            {
                _children.RemoveAt(i);
                return;
            }
        }
    }

    #endregion
}
