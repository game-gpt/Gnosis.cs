namespace Gnosis.Graphic.Capture;

public interface ICameraShake
{
    float Intensity { get; set; }
    float Duration { get; set; }
    float Decay { get; set; }
    bool IsShaking { get; }
    void Shake(float intensity, float duration);
    void Shake(float intensity, float duration, float decay);
    void StopShake();
    void Update(float delta);
}
