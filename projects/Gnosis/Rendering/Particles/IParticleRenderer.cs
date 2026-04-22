namespace Gnosis.Rendering.Particles;

public interface IParticleRenderer
{
    ParticleRenderMode RenderMode { get; set; }
    string? MaterialPath { get; set; }
    float CameraVelocityScale { get; set; }
    float VelocityScale { get; set; }
    float LengthScale { get; set; }
    bool RenderAlignment { get; set; }
    void Render();
}
