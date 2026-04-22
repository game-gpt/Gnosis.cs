namespace Gnosis.Graphic.FX;

public interface IParticleEmitter
{
    float Rate { get; set; }
    int BurstCount { get; set; }
    float BurstInterval { get; set; }
    ParticleEmitterShape Shape { get; set; }
    float[] Position { get; set; }
    float[] Direction { get; set; }
    float Angle { get; set; }
    float Radius { get; set; }
    float[] BoxSize { get; set; }
    float MinLifetime { get; set; }
    float MaxLifetime { get; set; }
    float MinSpeed { get; set; }
    float MaxSpeed { get; set; }
    int MaxParticles { get; set; }
    bool UseGpuSimulation { get; set; }
    void Emit(int count);
    void Reset();
}
