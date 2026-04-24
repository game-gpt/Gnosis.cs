using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Graphic.Capture;

public interface ICameraFollow
{
    Vector3 TargetPosition { get; set; }
    Vector3 Offset { get; set; }
    float FollowSpeed { get; set; }
    float Damping { get; set; }
    bool IsFollowing { get; }
    void StartFollowing();
    void StopFollowing();
    void Update(float delta);
}
