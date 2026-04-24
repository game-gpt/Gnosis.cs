using System.Numerics;
using Gnosis.Geometry.Cluster;
using Gnosis.Geometry.Culling;
using Gnosis.Geometry.Lod;

namespace Gnosis.Geometry.VirtualGeometry;

/// <summary>
/// 虚拟网格构建器，从原始网格数据构建 VirtualMesh
/// 封装了簇生成、LOD 构建和剔除器初始化的完整流程
/// </summary>
public sealed class VirtualMeshBuilder
{
    #region 属性

    /// <summary>
    /// 构建配置
    /// </summary>
    public VirtualGeometryConfig Config { get; }

    #endregion

    #region 字段

    private readonly ClusterLodGenerator _lodGenerator;
    private OcclusionCuller? _culler;

    #endregion

    #region 构造函数

    public VirtualMeshBuilder(VirtualGeometryConfig? config = null)
    {
        Config = config ?? new VirtualGeometryConfig();
        _lodGenerator = new ClusterLodGenerator(Config.ToLodConfig());
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 从顶点位置和索引缓冲区构建虚拟网格
    /// </summary>
    /// <param name="name">网格名称</param>
    /// <param name="positions">顶点位置数组</param>
    /// <param name="indices">索引缓冲区（uint）</param>
    /// <returns>构建完成的虚拟网格</returns>
    public VirtualMesh Build(string name, Vector3[] positions, uint[] indices)
    {
        var lodDag = _lodGenerator.Generate(positions, indices);
        return new VirtualMesh(name, lodDag);
    }

    /// <summary>
    /// 从顶点位置和索引缓冲区构建虚拟网格（int 索引）
    /// </summary>
    public VirtualMesh Build(string name, Vector3[] positions, int[] indices)
    {
        var uintIndices = new uint[indices.Length];
        for (var i = 0; i < indices.Length; i++)
        {
            uintIndices[i] = (uint)indices[i];
        }

        return Build(name, positions, uintIndices);
    }

    /// <summary>
    /// 从已有簇列表构建虚拟网格
    /// </summary>
    public VirtualMesh BuildFromClusters(string name, List<MeshCluster> clusters)
    {
        var lodDag = _lodGenerator.GenerateFromClusters(clusters);
        return new VirtualMesh(name, lodDag);
    }

    /// <summary>
    /// 创建与此构建器配置匹配的遮挡剔除器
    /// </summary>
    public OcclusionCuller CreateCuller()
    {
        return new OcclusionCuller(Config.ToCullerConfig());
    }

    /// <summary>
    /// 获取或创建共享的遮挡剔除器
    /// </summary>
    public OcclusionCuller GetOrCreateCuller()
    {
        return _culler ??= CreateCuller();
    }

    #endregion
}
