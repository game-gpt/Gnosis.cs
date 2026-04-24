using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Graphic.Capture;

public interface ICameraBoundary
{
    Vector3 MinBounds { get; set; }
    Vector3 MaxBounds { get; set; }
    bool UseSoftBounds { get; set; }
    float SoftBoundElasticity { get; set; }
    Vector3 ClampPosition(Vector3 position);
    bool IsWithinBounds(Vector3 position);
}
