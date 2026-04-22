namespace Gnosis.Navigation.NavMesh;

public struct NavMeshBuildSettings
{
    public float AgentRadius { get; init; }
    public float AgentHeight { get; init; }
    public float StepHeight { get; init; }
    public float SlopeAngle { get; init; }
    public float VoxelSize { get; init; }
    public float RegionMinArea { get; init; }
}
