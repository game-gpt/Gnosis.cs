using Gnosis.Navigation.Path;
using Gnosis.Navigation.Query;

namespace Gnosis.Navigation.NavMesh;

public class StubNavigationSystem : INavigationSystem
{
    public INavMeshBuilder Builder => throw new NotImplementedException("AI 系统尚未实现");
    public IPathfinder Pathfinder => throw new NotImplementedException("AI 系统尚未实现");
    public void BuildNavMesh(string name, NavMeshBuildSettings settings) { throw new NotImplementedException("AI 系统尚未实现"); }
    public INavMesh? GetNavMesh(string name) { throw new NotImplementedException("AI 系统尚未实现"); }
    public INavMeshQuery CreateQuery(INavMesh navMesh) { throw new NotImplementedException("AI 系统尚未实现"); }
    public void Update(float delta) { throw new NotImplementedException("AI 系统尚未实现"); }
}
