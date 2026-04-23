using Gnosis.Core.Math;

namespace Gnosis.Animation.IK;

public interface IBonePose
{
    string BoneName { get; }
    Vector3 Position { get; }
    Quaternion Rotation { get; }
    Vector3 Scale { get; }
}
