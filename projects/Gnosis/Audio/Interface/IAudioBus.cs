namespace Gnosis.Audio.Interface;

public interface IAudioBus
{
    string Name { get; }
    float Volume { get; set; }
    bool Mute { get; set; }
    bool BypassEffects { get; set; }
    IAudioBus? Parent { get; }
    IReadOnlyList<IAudioBus> Children { get; }
    IReadOnlyList<IAudioEffect> Effects { get; }
    void AddEffect(IAudioEffect effect);
    void RemoveEffect(int index);
    void AddChildBus(IAudioBus bus);
    void RemoveChildBus(string name);
}
