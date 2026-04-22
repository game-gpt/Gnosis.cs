namespace Gnosis.Navigation.Query;

/// <summary>
/// 导航网格查询接口，提供射线投射、最近点查询等能力
/// </summary>
public interface INavMeshQuery
{
    /// <summary>
    /// 查询最近可行走点
    /// </summary>
    float[] FindClosestPoint(float[] point, float maxDistance);

    /// <summary>
    /// 射线投射检测
    /// </summary>
    bool Raycast(float[] start, float[] end, out float[] hitPoint, out float hitDistance);

    /// <summary>
    /// 检测点是否在导航网格上
    /// </summary>
    bool IsPointOnNavMesh(float[] point);

    /// <summary>
    /// 获取指定半径内的可行走点
    /// </summary>
    IReadOnlyList<float[]> FindPointsInRadius(float[] center, float radius, int maxResults = 16);

    /// <summary>
    /// 获取多边形面积
    /// </summary>
    float GetPolygonArea(int polygonId);
}
