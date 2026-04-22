using Gnosis.Navigation.Path;
using Gnosis.Navigation.Query;

namespace Gnosis.Navigation.NavMesh;

/// <summary>
/// 导航系统实现类
/// </summary>
public class NavigationSystem : INavigationSystem
{
    #region 字段

    private readonly NavMeshBuilder _builder = new();
    private readonly Pathfinder _pathfinder = new();
    private readonly Dictionary<string, INavMesh> _navMeshes = new();

    #endregion

    #region INavigationSystem 实现

    /// <summary>
    /// 导航网格构建器
    /// </summary>
    public INavMeshBuilder Builder => _builder;

    /// <summary>
    /// 寻路器
    /// </summary>
    public IPathfinder Pathfinder => _pathfinder;

    /// <summary>
    /// 获取指定名称的导航网格
    /// </summary>
    public INavMesh? GetNavMesh(string name)
    {
        return _navMeshes.GetValueOrDefault(name);
    }

    /// <summary>
    /// 构建导航网格
    /// </summary>
    public void BuildNavMesh(string name, NavMeshBuildSettings settings)
    {
        var navMesh = _builder.Build(name, settings);
        _navMeshes[name] = navMesh;
        _pathfinder.UpdateNavMesh(navMesh);
    }

    /// <summary>
    /// 创建导航网格查询器
    /// </summary>
    public INavMeshQuery CreateQuery(INavMesh navMesh)
    {
        return new NavMeshQuery(navMesh);
    }

    /// <summary>
    /// 更新导航系统
    /// </summary>
    public void Update(float delta)
    {
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 从场景几何体构建导航网格
    /// </summary>
    public NavMesh BuildFromGeometry(string name, NavMeshBuildSettings settings, float[] vertices, int[] triangles, int triangleCount)
    {
        var navMesh = _builder.BuildFromGeometry(name, settings, vertices, triangles, triangleCount);
        _navMeshes[name] = navMesh;
        _pathfinder.UpdateNavMesh(navMesh);
        return navMesh;
    }

    #endregion
}
