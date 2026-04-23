using Gnosis.Core.Math;

namespace Gnosis.Navigation.NavMesh;

public interface INavMesh
{
    string Name { get; }
    bool IsBuilt { get; }
    NavMeshBuildSettings Settings { get; }
    void Build(NavMeshBuildSettings settings);
    void Rebuild();
    void Clear();
    bool IsPointWalkable(Vector3 point);
    Vector3 GetClosestPoint(Vector3 point);

    /// <summary>
    /// 导航网格中的多边形列表
    /// </summary>
    IReadOnlyList<NavMeshPolygon> Polygons { get; }

    /// <summary>
    /// 根据位置查找所在多边形
    /// </summary>
    NavMeshPolygon? FindPolygon(Vector3 point);
}
