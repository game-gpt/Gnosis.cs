namespace Gnosis.Audio.Mixer;

public sealed class AudioEffect : IAudioEffect
{
    #region 字段

    private readonly Dictionary<string, float> _parameters = new();

    #endregion

    #region 属性

    public string Name { get; }

    public AudioEffectType EffectType { get; }

    public bool IsEnabled { get; set; } = true;

    #endregion

    #region 构造函数

    public AudioEffect(string name, AudioEffectType effectType)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        EffectType = effectType;
    }

    #endregion

    #region IAudioEffect 实现

    public float GetParameter(string name)
    {
        return _parameters.GetValueOrDefault(name, 0f);
    }

    public void SetParameter(string name, float value)
    {
        _parameters[name] = value;
    }

    #endregion
}
