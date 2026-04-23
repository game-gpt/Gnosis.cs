using Gnosis.Core.Math;

namespace Gnosis.Graphic.Light;

public interface IAreaLight : ILight
{
    Vector3 Position { get; set; }
    Vector3 Direction { get; set; }
    float Width { get; set; }
    float Height { get; set; }
    float Range { get; set; }
}
