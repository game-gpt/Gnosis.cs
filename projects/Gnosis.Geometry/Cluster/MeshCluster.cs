using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Geometry.Cluster;

/// <summary>
/// 网格簇，Nanite 风格虚拟几何的基本单元
/// 每个簇包含固定数量的三角形（默认 128）及其包围信息
/// </summary>
public sealed class MeshCluster
{
    #region 常量

    /// <summary>
    /// 每个簇的默认三角形数量
    /// </summary>
    public const int DefaultTrianglesPerCluster = 128;

    #endregion

    #region 属性

    /// <summary>
    /// 簇的唯一标识
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// 簇所属的 LOD 级别（0 为最高精度）
    /// </summary>
    public int LodLevel { get; }

    /// <summary>
    /// 簇的包围信息
    /// </summary>
    public ClusterBounds Bounds { get; }

    /// <summary>
    /// 簇内顶点位置列表
    /// </summary>
    public Vector3[] Positions { get; }

    /// <summary>
    /// 簇内索引缓冲区（每 3 个索引组成一个三角形）
    /// </summary>
    public uint[] Indices { get; }

    /// <summary>
    /// 簇内三角形数量
    /// </summary>
    public int TriangleCount => Indices.Length / 3;

    /// <summary>
    /// 父簇组标识（-1 表示根节点）
    /// </summary>
    public int ParentGroupIndex { get; internal set; } = -1;

    /// <summary>
    /// 此簇所属的簇组标识（-1 表示未分组）
    /// </summary>
    public int GroupIndex { get; internal set; } = -1;

    #endregion

    #region 构造函数

    public MeshCluster(int id, int lodLevel, Vector3[] positions, uint[] indices, ClusterBounds bounds)
    {
        Id = id;
        LodLevel = lodLevel;
        Positions = positions;
        Indices = indices;
        Bounds = bounds;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 计算簇在给定变换下的世界空间包围盒
    /// </summary>
    public BoundingBox GetWorldBounds(Matrix4x4 transform)
    {
        if (Positions.Length == 0)
        {
            return BoundingBox.Empty;
        }

        var worldPositions = new Vector3[Positions.Length];
        for (var i = 0; i < Positions.Length; i++)
        {
            worldPositions[i] = Vector3.Transform(Positions[i], transform);
        }

        return BoundingBox.CreateFromPoints(worldPositions);
    }

    #endregion
}
