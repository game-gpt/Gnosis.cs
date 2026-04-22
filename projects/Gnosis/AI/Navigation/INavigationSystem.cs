namespace Gnosis.AI.Navigation;

public interface INavigationSystem
{
    INavMeshBuilder Builder { get; }
    IPathfinder Pathfinder { get; }
    INavMesh? GetNavMesh(string name);
    void BuildNavMesh(string name, NavMeshBuildSettings settings);
    void Update(float delta);
}
