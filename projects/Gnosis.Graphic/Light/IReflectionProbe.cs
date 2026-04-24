using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Graphic.Light;

public interface IReflectionProbe
{
    Vector3 Position { get; set; }
    Vector3 Size { get; set; }
    bool IsRealtime { get; set; }
    int Resolution { get; set; }
    float Importance { get; set; }
    float Intensity { get; set; }
    void Bake();
    void Update();
}
