namespace Gnosis.Rendering.Particles;

public interface IParticleModule
{
    string Name { get; }
    bool IsEnabled { get; set; }
    ParticleModuleType ModuleType { get; }
}

public enum ParticleModuleType
{
    ColorOverLifetime = 0,
    SizeOverLifetime = 1,
    SpeedOverLifetime = 2,
    ForceOverLifetime = 3,
    RotationOverLifetime = 4,
    Noise = 5,
    Collision = 6,
    SubEmitter = 7,
    TextureSheetAnimation = 8,
    Trail = 9,
    Custom = 10
}
