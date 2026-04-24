using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Geometry.Cluster;
using Gnosis.Geometry.Lod;

namespace Gnosis.Geometry.Culling;

/// <summary>
/// 遮挡剔除器，结合 HZB 和视锥剔除对簇进行可见性判断
/// </summary>
public sealed class OcclusionCuller
{
    #region 嵌套类型

    /// <summary>
    /// 剔除结果
    /// </summary>
    public sealed class CullingResult
    {
        /// <summary>
        /// 通过遮挡剔除的簇索引集合
        /// </summary>
        public HashSet<int> VisibleClusterIndices { get; } = [];

        /// <summary>
        /// 被视锥剔除的簇数量
        /// </summary>
        public int FrustumCulledCount { get; init; }

        /// <summary>
        /// 被遮挡剔除的簇数量
        /// </summary>
        public int OcclusionCulledCount { get; init; }

        /// <summary>
        /// 总共处理的簇数量
        /// </summary>
        public int TotalClusterCount { get; init; }
    }

    /// <summary>
    /// 剔除配置
    /// </summary>
    public sealed class Config
    {
        /// <summary>
        /// 是否启用视锥剔除
        /// </summary>
        public bool EnableFrustumCulling { get; init; } = true;

        /// <summary>
        /// 是否启用遮挡剔除
        /// </summary>
        public bool EnableOcclusionCulling { get; init; } = true;

        /// <summary>
        /// 遮挡剔除的深度偏移，用于避免自遮挡
        /// </summary>
        public float DepthBias { get; init; } = 0.001f;
    }

    #endregion

    #region 字段

    private readonly HierarchicalZBuffer _hzb;
    private readonly Config _config;

    #endregion

    #region 属性

    /// <summary>
    /// 层次 Z-Buffer 引用
    /// </summary>
    public HierarchicalZBuffer Hzb => _hzb;

    #endregion

    #region 构造函数

    public OcclusionCuller(Config? config = null)
    {
        _config = config ?? new Config();
        _hzb = new HierarchicalZBuffer();
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 更新 HZB，从深度缓冲区构建层次结构
    /// </summary>
    /// <param name="depthBuffer">深度缓冲区数据</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    public void UpdateHzb(ReadOnlySpan<float> depthBuffer, int width, int height)
    {
        _hzb.Build(depthBuffer, width, height);
    }

    /// <summary>
    /// 对簇列表执行完整的剔除流程（视锥 + 遮挡）
    /// </summary>
    /// <param name="clusters">待剔除的簇列表</param>
    /// <param name="frustum">视锥体</param>
    /// <param name="viewProjection">视图投影矩阵</param>
    /// <returns>剔除结果</returns>
    public CullingResult Cull(IReadOnlyList<MeshCluster> clusters, Frustum frustum, Matrix4x4 viewProjection)
    {
        var result = new CullingResult
        {
            TotalClusterCount = clusters.Count
        };

        var frustumCulled = 0;
        var occlusionCulled = 0;

        for (var i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];
            var bounds = cluster.Bounds.Bounds;

            if (_config.EnableFrustumCulling)
            {
                if (!IsInFrustum(bounds, frustum))
                {
                    frustumCulled++;
                    continue;
                }
            }

            if (_config.EnableOcclusionCulling && _hzb.IsInitialized)
            {
                var depthMin = ComputeClusterDepthMin(bounds, viewProjection);

                if (_hzb.IsOccluded(bounds, viewProjection))
                {
                    occlusionCulled++;
                    continue;
                }
            }

            result.VisibleClusterIndices.Add(i);
        }

        return new CullingResult
        {
            TotalClusterCount = clusters.Count,
            FrustumCulledCount = frustumCulled,
            OcclusionCulledCount = occlusionCulled
        };
    }

    /// <summary>
    /// 对 LOD DAG 选出的簇执行遮挡剔除
    /// </summary>
    /// <param name="dag">LOD DAG</param>
    /// <param name="frustum">视锥体</param>
    /// <param name="viewProjection">视图投影矩阵</param>
    /// <param name="errorCalculator">屏幕空间误差计算委托</param>
    /// <returns>剔除结果</returns>
    public CullingResult CullWithLod(
        LodDag dag,
        Frustum frustum,
        Matrix4x4 viewProjection,
        Func<LodNode, float> errorCalculator)
    {
        var selectedClusters = dag.SelectVisibleClusters(errorCalculator);

        var clustersToCull = new List<(int Index, MeshCluster Cluster)>();
        foreach (var clusterIdx in selectedClusters)
        {
            if (clusterIdx >= 0 && clusterIdx < dag.Clusters.Count)
            {
                clustersToCull.Add((clusterIdx, dag.Clusters[clusterIdx]));
            }
        }

        var result = new CullingResult
        {
            TotalClusterCount = clustersToCull.Count
        };

        var frustumCulled = 0;
        var occlusionCulled = 0;

        foreach (var (index, cluster) in clustersToCull)
        {
            var bounds = cluster.Bounds.Bounds;

            if (_config.EnableFrustumCulling)
            {
                if (!IsInFrustum(bounds, frustum))
                {
                    frustumCulled++;
                    continue;
                }
            }

            if (_config.EnableOcclusionCulling && _hzb.IsInitialized)
            {
                if (_hzb.IsOccluded(bounds, viewProjection))
                {
                    occlusionCulled++;
                    continue;
                }
            }

            result.VisibleClusterIndices.Add(index);
        }

        return new CullingResult
        {
            TotalClusterCount = clustersToCull.Count,
            FrustumCulledCount = frustumCulled,
            OcclusionCulledCount = occlusionCulled
        };
    }

    /// <summary>
    /// 仅执行视锥剔除
    /// </summary>
    public HashSet<int> FrustumCull(IReadOnlyList<MeshCluster> clusters, Frustum frustum)
    {
        var visible = new HashSet<int>();

        for (var i = 0; i < clusters.Count; i++)
        {
            if (IsInFrustum(clusters[i].Bounds.Bounds, frustum))
            {
                visible.Add(i);
            }
        }

        return visible;
    }

    #endregion

    #region 私有方法

    private static bool IsInFrustum(BoundingBox bounds, Frustum frustum)
    {
        return frustum.Intersects(bounds);
    }

    private static float ComputeClusterDepthMin(BoundingBox bounds, Matrix4x4 viewProjection)
    {
        var corners = new Vector3[]
        {
            new(bounds.Min.X, bounds.Min.Y, bounds.Min.Z),
            new(bounds.Max.X, bounds.Min.Y, bounds.Min.Z),
            new(bounds.Min.X, bounds.Max.Y, bounds.Min.Z),
            new(bounds.Max.X, bounds.Max.Y, bounds.Min.Z),
            new(bounds.Min.X, bounds.Min.Y, bounds.Max.Z),
            new(bounds.Max.X, bounds.Min.Y, bounds.Max.Z),
            new(bounds.Min.X, bounds.Max.Y, bounds.Max.Z),
            new(bounds.Max.X, bounds.Max.Y, bounds.Max.Z)
        };

        var depthMin = float.MaxValue;

        foreach (var corner in corners)
        {
            var clipPos = Vector4.Transform(new Vector4(corner, 1.0f), viewProjection);

            if (clipPos.W > 0.0f)
            {
                var ndcZ = clipPos.Z / clipPos.W;
                if (ndcZ < depthMin)
                {
                    depthMin = ndcZ;
                }
            }
        }

        return depthMin;
    }

    #endregion
}
