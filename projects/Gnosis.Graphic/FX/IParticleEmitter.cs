using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Graphic.FX;

public interface IParticleEmitter
{
    float Rate { get; set; }
    int BurstCount { get; set; }
    float BurstInterval { get; set; }
    ParticleEmitterShape Shape { get; set; }
    Vector3 Position { get; set; }
    Vector3 Direction { get; set; }
    float Angle { get; set; }
    float Radius { get; set; }
    Vector3 BoxSize { get; set; }
    float MinLifetime { get; set; }
    float MaxLifetime { get; set; }
    float MinSpeed { get; set; }
    float MaxSpeed { get; set; }
    int MaxParticles { get; set; }
    bool UseGpuSimulation { get; set; }
    void Emit(int count);
    void Reset();
}
