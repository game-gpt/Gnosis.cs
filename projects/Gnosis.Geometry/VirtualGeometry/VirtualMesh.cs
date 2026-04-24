using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Geometry.Cluster;
using Gnosis.Geometry.Culling;
using Gnosis.Geometry.Lod;

namespace Gnosis.Geometry.VirtualGeometry;

/// <summary>
/// 虚拟网格，Nanite 风格虚拟几何系统的运行时表示
/// 包含 LOD DAG、簇数据和剔除信息，支持基于屏幕空间的动态 LOD 选择
/// </summary>
public sealed class VirtualMesh : IDisposable
{
    #region 属性

    /// <summary>
    /// 虚拟网格名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// LOD DAG
    /// </summary>
    public LodDag LodDag { get; }

    /// <summary>
    /// 所有簇的列表（跨 LOD 级别）
    /// </summary>
    public IReadOnlyList<MeshCluster> Clusters => LodDag.Clusters;

    /// <summary>
    /// 所有簇组的列表
    /// </summary>
    public IReadOnlyList<MeshClusterGroup> Groups => LodDag.Groups;

    /// <summary>
    /// 世界空间变换矩阵
    /// </summary>
    public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;

    /// <summary>
    /// 世界空间包围盒（所有 LOD 的并集）
    /// </summary>
    public BoundingBox WorldBounds { get; private set; }

    /// <summary>
    /// 总三角形数量（最高 LOD）
    /// </summary>
    public int TotalTriangleCount { get; }

    /// <summary>
    /// LOD 级别数量
    /// </summary>
    public int LodLevelCount => LodDag.LodLevelCount;

    /// <summary>
    /// 簇总数
    /// </summary>
    public int ClusterCount => LodDag.Clusters.Count;

    /// <summary>
    /// 是否可见
    /// </summary>
    public bool Visible { get; set; } = true;

    #endregion

    #region 字段

    private bool _isDisposed;

    #endregion

    #region 构造函数

    public VirtualMesh(string name, LodDag lodDag)
    {
        Name = name;
        LodDag = lodDag;

        WorldBounds = ComputeWorldBounds();
        TotalTriangleCount = ComputeTotalTriangleCount();
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 根据相机参数选择需要渲染的簇集合
    /// 结合 LOD 选择和剔除
    /// </summary>
    /// <param name="cameraPosition">相机世界位置</param>
    /// <param name="cameraForward">相机前方向</param>
    /// <param name="fovY">垂直视场角（弧度）</param>
    /// <param name="screenHeight">屏幕高度（像素）</param>
    /// <returns>需要渲染的簇索引集合</returns>
    public HashSet<int> SelectVisibleClusters(
        Vector3 cameraPosition,
        Vector3 cameraForward,
        float fovY,
        float screenHeight)
    {
        return LodDag.SelectVisibleClusters(node =>
        {
            var clusterBounds = GetNodeBounds(node);
            return ScreenSpaceError.Calculate(
                clusterBounds.SphereCenter,
                clusterBounds.SphereRadius,
                cameraPosition,
                cameraForward,
                fovY,
                screenHeight);
        });
    }

    /// <summary>
    /// 使用遮挡剔除器执行完整的可见性判断
    /// </summary>
    /// <param name="culler">遮挡剔除器</param>
    /// <param name="frustum">视锥体</param>
    /// <param name="viewProjection">视图投影矩阵</param>
    /// <param name="cameraPosition">相机世界位置</param>
    /// <param name="cameraForward">相机前方向</param>
    /// <param name="fovY">垂直视场角（弧度）</param>
    /// <param name="screenHeight">屏幕高度（像素）</param>
    /// <returns>剔除结果</returns>
    public OcclusionCuller.CullingResult Cull(
        OcclusionCuller culler,
        Frustum frustum,
        Matrix4x4 viewProjection,
        Vector3 cameraPosition,
        Vector3 cameraForward,
        float fovY,
        float screenHeight)
    {
        return culler.CullWithLod(
            LodDag,
            frustum,
            viewProjection,
            node =>
            {
                var clusterBounds = GetNodeBounds(node);
                return ScreenSpaceError.Calculate(
                    clusterBounds.SphereCenter,
                    clusterBounds.SphereRadius,
                    cameraPosition,
                    cameraForward,
                    fovY,
                    screenHeight);
            });
    }

    /// <summary>
    /// 更新世界空间包围盒
    /// </summary>
    public void UpdateWorldBounds()
    {
        WorldBounds = ComputeWorldBounds();
    }

    /// <summary>
    /// 获取指定 LOD 级别的三角形总数
    /// </summary>
    public int GetTriangleCountAtLod(int lodLevel)
    {
        var nodes = LodDag.GetNodesAtLevel(lodLevel);
        var count = 0;

        foreach (var node in nodes)
        {
            foreach (var clusterIdx in node.ClusterIndices)
            {
                if (clusterIdx >= 0 && clusterIdx < Clusters.Count)
                {
                    count += Clusters[clusterIdx].TriangleCount;
                }
            }
        }

        return count;
    }

    #endregion

    #region 私有方法

    private ClusterBounds GetNodeBounds(LodNode node)
    {
        if (node.ClusterIndices.Length == 0)
        {
            return default;
        }

        var firstCluster = Clusters[node.ClusterIndices[0]];
        var mergedBounds = firstCluster.Bounds.Bounds;

        for (var i = 1; i < node.ClusterIndices.Length; i++)
        {
            if (node.ClusterIndices[i] >= 0 && node.ClusterIndices[i] < Clusters.Count)
            {
                mergedBounds = BoundingBox.Merge(mergedBounds, Clusters[node.ClusterIndices[i]].Bounds.Bounds);
            }
        }

        var center = mergedBounds.Center;
        var extents = mergedBounds.Extents;
        var radius = extents.Length();

        return new ClusterBounds
        {
            Bounds = mergedBounds,
            SphereCenter = center,
            SphereRadius = radius,
            ScreenSpaceError = node.ScreenSpaceErrorThreshold
        };
    }

    private BoundingBox ComputeWorldBounds()
    {
        if (Clusters.Count == 0)
        {
            return BoundingBox.Empty;
        }

        var bounds = Clusters[0].GetWorldBounds(Transform);

        for (var i = 1; i < Clusters.Count; i++)
        {
            bounds = BoundingBox.Merge(bounds, Clusters[i].GetWorldBounds(Transform));
        }

        return bounds;
    }

    private int ComputeTotalTriangleCount()
    {
        var count = 0;
        var leafNodes = LodDag.GetLeafNodes();

        foreach (var node in leafNodes)
        {
            foreach (var clusterIdx in node.ClusterIndices)
            {
                if (clusterIdx >= 0 && clusterIdx < Clusters.Count)
                {
                    count += Clusters[clusterIdx].TriangleCount;
                }
            }
        }

        return count;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
    }

    #endregion
}
