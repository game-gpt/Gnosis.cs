using Gnosis.Navigation.Path;
using Gnosis.Navigation.Query;

namespace Gnosis.Navigation.NavMesh;

public interface INavigationSystem
{
    INavMeshBuilder Builder { get; }
    IPathfinder Pathfinder { get; }
    INavMesh? GetNavMesh(string name);
    void BuildNavMesh(string name, NavMeshBuildSettings settings);

    /// <summary>
    /// 创建导航网格查询器
    /// </summary>
    INavMeshQuery CreateQuery(INavMesh navMesh);

    void Update(float delta);
}
