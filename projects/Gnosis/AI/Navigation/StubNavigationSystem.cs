namespace Gnosis.AI.Navigation;

public class StubNavigationSystem : INavigationSystem
{
    public INavMeshBuilder Builder => throw new NotImplementedException("AI 系统尚未实现");
    public IPathfinder Pathfinder => throw new NotImplementedException("AI 系统尚未实现");
    public void BuildNavMesh(string name, NavMeshBuildSettings settings) { throw new NotImplementedException("AI 系统尚未实现"); }
    public INavMesh? GetNavMesh(string name) { throw new NotImplementedException("AI 系统尚未实现"); }
    public void Update(float delta) { throw new NotImplementedException("AI 系统尚未实现"); }
}
