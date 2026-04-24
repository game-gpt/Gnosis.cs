using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Geometry.Cluster;

/// <summary>
/// 簇组，将多个相邻簇聚合为一个 LOD 节点
/// 簇组是 LOD DAG 的基本节点，每个簇组可以简化为更粗粒度的簇组
/// </summary>
public sealed class MeshClusterGroup
{
    #region 属性

    /// <summary>
    /// 簇组的唯一标识
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// 簇组的 LOD 级别
    /// </summary>
    public int LodLevel { get; }

    /// <summary>
    /// 簇组包含的簇索引列表
    /// </summary>
    public int[] ClusterIndices { get; }

    /// <summary>
    /// 簇组的合并包围信息
    /// </summary>
    public ClusterBounds Bounds { get; }

    /// <summary>
    /// 父簇组索引（-1 表示根节点）
    /// </summary>
    public int ParentIndex { get; internal set; } = -1;

    /// <summary>
    /// 子簇组索引列表（更精细的 LOD）
    /// </summary>
    public List<int> ChildIndices { get; } = [];

    /// <summary>
    /// 此簇组简化后对应的簇索引（-1 表示无简化版本）
    /// </summary>
    public int SimplifiedClusterIndex { get; internal set; } = -1;

    /// <summary>
    /// 屏幕空间误差阈值，当屏幕空间误差低于此值时应切换到此 LOD
    /// </summary>
    public float ScreenSpaceErrorThreshold { get; }

    #endregion

    #region 构造函数

    public MeshClusterGroup(
        int id,
        int lodLevel,
        int[] clusterIndices,
        ClusterBounds bounds,
        float screenSpaceErrorThreshold)
    {
        Id = id;
        LodLevel = lodLevel;
        ClusterIndices = clusterIndices;
        Bounds = bounds;
        ScreenSpaceErrorThreshold = screenSpaceErrorThreshold;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 从一组簇创建簇组
    /// </summary>
    public static MeshClusterGroup FromClusters(
        int id,
        int lodLevel,
        IReadOnlyList<MeshCluster> clusters,
        int[] clusterIndices,
        float screenSpaceErrorThreshold)
    {
        var allPositions = new List<Vector3>();

        foreach (var clusterIdx in clusterIndices)
        {
            if (clusterIdx >= 0 && clusterIdx < clusters.Count)
            {
                allPositions.AddRange(clusters[clusterIdx].Positions);
            }
        }

        var bounds = ClusterBounds.FromPositions(allPositions.ToArray(), screenSpaceErrorThreshold);

        return new MeshClusterGroup(id, lodLevel, clusterIndices, bounds, screenSpaceErrorThreshold);
    }

    #endregion
}
