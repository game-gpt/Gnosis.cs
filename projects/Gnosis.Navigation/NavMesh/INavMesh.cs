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

    /// <summary>
    /// 获取指定 ID 的多边形
    /// </summary>
    NavMeshPolygon? GetPolygonById(int id);

    /// <summary>
    /// 标记多边形为阻塞状态
    /// </summary>
    /// <param name="polygonId">多边形 ID</param>
    /// <param name="blocked">是否阻塞</param>
    /// <returns>是否成功标记</returns>
    bool SetPolygonBlocked(int polygonId, bool blocked);

    /// <summary>
    /// 检测多边形是否被阻塞
    /// </summary>
    /// <param name="polygonId">多边形 ID</param>
    /// <returns>是否被阻塞</returns>
    bool IsPolygonBlocked(int polygonId);

    /// <summary>
    /// 获取所有被阻塞的多边形 ID
    /// </summary>
    IReadOnlySet<int> BlockedPolygonIds { get; }

    /// <summary>
    /// 清除所有阻塞标记
    /// </summary>
    void ClearAllBlocked();

    /// <summary>
    /// 查找与指定区域相交的所有多边形
    /// </summary>
    /// <param name="center">区域中心</param>
    /// <param name="halfExtents">区域半尺寸</param>
    /// <returns>相交的多边形 ID 列表</returns>
    List<int> FindPolygonsInArea(Vector3 center, Vector3 halfExtents);
}
