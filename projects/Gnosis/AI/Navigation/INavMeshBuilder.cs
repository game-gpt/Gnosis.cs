namespace Gnosis.AI.Navigation;

public interface INavMeshBuilder
{
    INavMesh Build(string name, NavMeshBuildSettings settings);
    void Rebuild(INavMesh navMesh);
    void Destroy(string name);
}
