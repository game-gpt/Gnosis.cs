using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Graphic.RHI;

public interface ICamera2D
{
    Vector3 Position { get; set; }
    float Zoom { get; set; }
    float Rotation { get; set; }
    Vector2 ViewportSize { get; set; }
    Vector3 ScreenToWorld(Vector3 screenPoint);
    Vector3 WorldToScreen(Vector3 worldPoint);
}
