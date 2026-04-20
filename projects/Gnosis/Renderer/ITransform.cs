using Gnosis.Core.ValueObjects;

namespace Gnosis.Renderer;

public interface ITransform
{
    Position Position { get; set; }
    (float X, float Y, float Z, float W) Rotation { get; set; }
    (float X, float Y, float Z) Scale { get; set; }
}
