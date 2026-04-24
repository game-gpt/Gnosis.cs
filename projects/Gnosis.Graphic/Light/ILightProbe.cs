using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Graphic.Light;

public interface ILightProbe
{
    Vector3 Position { get; set; }
    float[] ShCoefficients { get; }
    void Bake();
    void Update();
}
