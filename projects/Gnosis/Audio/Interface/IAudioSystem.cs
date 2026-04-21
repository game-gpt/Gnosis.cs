namespace Gnosis.Audio.Interface;

public interface IAudioSystem
{
    IAudioListener Listener { get; }
    IAudioBus MasterBus { get; }
    float GlobalVolume { get; set; }
    IAudioSource CreateSource();
    void DestroySource(IAudioSource source);
    IAudioClip LoadClip(string path);
    void UnloadClip(string path);
    IAudioBus CreateBus(string name, IAudioBus? parent = null);
    void DestroyBus(string name);
    IAudioBus? GetBus(string name);
    void Update(float delta);
}
