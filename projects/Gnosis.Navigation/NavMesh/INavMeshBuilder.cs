namespace Gnosis.Navigation.NavMesh;

public interface INavMeshBuilder
{
    INavMesh Build(string name, NavMeshBuildSettings settings);
    void Rebuild(INavMesh navMesh);
    void Destroy(string name);
}
