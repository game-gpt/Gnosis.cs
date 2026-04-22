namespace Gnosis.AI.Navigation;

public interface INavMesh
{
    string Name { get; }
    bool IsBuilt { get; }
    NavMeshBuildSettings Settings { get; }
    void Build(NavMeshBuildSettings settings);
    void Rebuild();
    void Clear();
    bool IsPointWalkable(float[] point);
    float[] GetClosestPoint(float[] point);
}
