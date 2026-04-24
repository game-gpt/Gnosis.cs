using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Geometry.Cluster;
using Gnosis.Geometry.Culling;
using Gnosis.Geometry.Lod;

namespace Gnosis.Geometry.VirtualGeometry;

/// <summary>
/// 虚拟几何系统配置
/// </summary>
public sealed class VirtualGeometryConfig
{
    /// <summary>
    /// 每个簇的三角形数量，默认 128（与 Nanite 一致）
    /// </summary>
    public int TrianglesPerCluster { get; init; } = MeshCluster.DefaultTrianglesPerCluster;

    /// <summary>
    /// 最大 LOD 级别数量
    /// </summary>
    public int MaxLodLevels { get; init; } = 8;

    /// <summary>
    /// 每级 LOD 的简化比例
    /// </summary>
    public float SimplificationRatio { get; init; } = 0.5f;

    /// <summary>
    /// 最小三角形数量，低于此值不再简化
    /// </summary>
    public int MinTriangles { get; init; } = 4;

    /// <summary>
    /// 簇组合并的目标簇数量
    /// </summary>
    public int ClusterGroupSize { get; init; } = 4;

    /// <summary>
    /// LOD 切换的基础屏幕空间误差阈值（像素）
    /// </summary>
    public float BaseScreenSpaceErrorThreshold { get; init; } = 1.0f;

    /// <summary>
    /// 每级 LOD 的屏幕空间误差倍增因子
    /// </summary>
    public float ScreenSpaceErrorMultiplier { get; init; } = 2.0f;

    /// <summary>
    /// 是否启用遮挡剔除
    /// </summary>
    public bool EnableOcclusionCulling { get; init; } = true;

    /// <summary>
    /// 是否启用视锥剔除
    /// </summary>
    public bool EnableFrustumCulling { get; init; } = true;

    /// <summary>
    /// 遮挡剔除深度偏移
    /// </summary>
    public float OcclusionDepthBias { get; init; } = 0.001f;

    /// <summary>
    /// 转换为 LOD 生成器配置
    /// </summary>
    public ClusterLodGenerator.Config ToLodConfig()
    {
        return new ClusterLodGenerator.Config
        {
            MaxLodLevels = MaxLodLevels,
            SimplificationRatio = SimplificationRatio,
            MinTriangles = MinTriangles,
            ClusterGroupSize = ClusterGroupSize,
            BaseScreenSpaceErrorThreshold = BaseScreenSpaceErrorThreshold,
            ScreenSpaceErrorMultiplier = ScreenSpaceErrorMultiplier
        };
    }

    /// <summary>
    /// 转换为遮挡剔除器配置
    /// </summary>
    public OcclusionCuller.Config ToCullerConfig()
    {
        return new OcclusionCuller.Config
        {
            EnableFrustumCulling = EnableFrustumCulling,
            EnableOcclusionCulling = EnableOcclusionCulling,
            DepthBias = OcclusionDepthBias
        };
    }
}
