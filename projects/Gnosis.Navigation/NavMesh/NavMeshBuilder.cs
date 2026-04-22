namespace Gnosis.Navigation.NavMesh;

/// <summary>
/// 导航网格构建器实现类，执行体素化→区域生成→多边形化完整流程
/// </summary>
public class NavMeshBuilder : INavMeshBuilder
{
    #region 字段

    private readonly Dictionary<string, NavMesh> _navMeshes = new();

    #endregion

    #region INavMeshBuilder 实现

    /// <summary>
    /// 构建导航网格
    /// </summary>
    public INavMesh Build(string name, NavMeshBuildSettings settings)
    {
        var navMesh = new NavMesh(name);
        navMesh.Build(settings);
        _navMeshes[name] = navMesh;
        return navMesh;
    }

    /// <summary>
    /// 重新构建导航网格
    /// </summary>
    public void Rebuild(INavMesh navMesh)
    {
        navMesh.Rebuild();
    }

    /// <summary>
    /// 销毁导航网格
    /// </summary>
    public void Destroy(string name)
    {
        _navMeshes.Remove(name);
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 从场景几何体构建导航网格
    /// </summary>
    public NavMesh BuildFromGeometry(string name, NavMeshBuildSettings settings, float[] vertices, int[] triangles, int triangleCount)
    {
        var voxelizer = new Voxelizer();
        voxelizer.Voxelize(vertices, triangles, triangleCount, settings);
        voxelizer.MarkWalkable(settings.SlopeAngle, settings.AgentHeight, settings.StepHeight);

        var regionGenerator = new RegionGenerator();
        regionGenerator.Generate(voxelizer, settings.RegionMinArea);

        var polygonizer = new Polygonizer();
        polygonizer.Polygonize(regionGenerator.RegionCount, regionGenerator, voxelizer, settings.VoxelSize * 2.0f);

        var navMesh = new NavMesh(name);
        navMesh.Build(settings);
        navMesh.SetPolygons(polygonizer.Polygons);

        _navMeshes[name] = navMesh;
        return navMesh;
    }

    /// <summary>
    /// 获取已构建的导航网格
    /// </summary>
    public NavMesh? GetBuiltNavMesh(string name)
    {
        return _navMeshes.GetValueOrDefault(name);
    }

    #endregion
}
