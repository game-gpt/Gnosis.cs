using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Sky;

public interface ISkySettings
{
    bool EnableAtmosphere { get; set; }
    bool EnableSkybox { get; set; }
    float SkyDistance { get; set; }
    Vector3 SunDirection { get; set; }
    float SunSize { get; set; }
    Vector3 SunColor { get; set; }
    float StarIntensity { get; set; }
}
