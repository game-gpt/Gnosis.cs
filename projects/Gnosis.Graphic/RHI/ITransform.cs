using Gnosis.Core;

namespace Gnosis.Rendering.RHI;

public interface ITransform
{
    Position Position { get; set; }
    (float X, float Y, float Z, float W) Rotation { get; set; }
    (float X, float Y, float Z) Scale { get; set; }
}
