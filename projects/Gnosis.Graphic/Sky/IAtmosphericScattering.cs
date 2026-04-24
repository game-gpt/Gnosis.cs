using System.Numerics;

namespace Gnosis.Graphic.Sky;

public interface IAtmosphericScattering
{
    Vector3 SunDirection { get; set; }
    Vector3 SunIntensity { get; set; }
    float RayleighCoefficient { get; set; }
    float MieCoefficient { get; set; }
    float MieDirectionalG { get; set; }
    float PlanetRadius { get; set; }
    float AtmosphereRadius { get; set; }
    Vector3 RayleighScattering { get; }
    Vector3 MieScattering { get; }
    int NumSamples { get; set; }
    int NumLightSamples { get; set; }
    float HeightScale { get; set; }
}
