using Gnosis.Core.Event;
using Gnosis.Core.Math;

namespace Gnosis.Graphic.RHI;

public interface ITransform
{
    Position Position { get; set; }
    Quaternion Rotation { get; set; }
    Vector3 Scale { get; set; }
}
