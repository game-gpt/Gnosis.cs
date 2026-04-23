using Gnosis.Core.Math;

namespace Gnosis.Navigation.Path;

public interface IPath
{
    bool IsComplete { get; }
    float Length { get; }
    IReadOnlyList<Vector3> Waypoints { get; }
    int CurrentWaypointIndex { get; }
    Vector3 CurrentWaypoint { get; }
    Vector3 NextWaypoint { get; }
    void Advance();
    bool IsAtPathEnd();
}
