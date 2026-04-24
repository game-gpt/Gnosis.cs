using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Graphic.Light;

public interface IPointLight : ILight
{
    Vector3 Position { get; set; }
    float Range { get; set; }
    float Attenuation { get; set; }
}
