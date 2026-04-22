namespace Gnosis.AI.Navigation;

public interface IPath
{
    bool IsComplete { get; }
    float Length { get; }
    IReadOnlyList<float[]> Waypoints { get; }
    int CurrentWaypointIndex { get; }
    float[] CurrentWaypoint { get; }
    float[] NextWaypoint { get; }
    void Advance();
    bool IsAtPathEnd();
}
