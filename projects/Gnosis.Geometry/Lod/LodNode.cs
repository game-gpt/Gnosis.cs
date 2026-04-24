namespace Gnosis.Geometry.Lod;

/// <summary>
/// LOD DAG 节点，表示一个 LOD 级别的簇组
/// DAG 的叶子节点是最高精度的簇组，根节点是最粗略的簇组
/// </summary>
public sealed class LodNode
{
    #region 属性

    /// <summary>
    /// 节点唯一标识
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// LOD 级别（0 为最高精度）
    /// </summary>
    public int LodLevel { get; }

    /// <summary>
    /// 此节点包含的簇索引列表
    /// </summary>
    public int[] ClusterIndices { get; }

    /// <summary>
    /// 此节点简化后对应的簇索引（-1 表示叶子节点或无简化版本）
    /// </summary>
    public int SimplifiedClusterIndex { get; internal set; } = -1;

    /// <summary>
    /// 父节点索引（-1 表示根节点）
    /// </summary>
    public int ParentIndex { get; internal set; } = -1;

    /// <summary>
    /// 子节点索引列表（更精细的 LOD）
    /// </summary>
    public List<int> ChildIndices { get; } = [];

    /// <summary>
    /// 屏幕空间误差阈值
    /// </summary>
    public float ScreenSpaceErrorThreshold { get; }

    /// <summary>
    /// 是否为叶子节点
    /// </summary>
    public bool IsLeaf => ChildIndices.Count == 0;

    #endregion

    #region 构造函数

    public LodNode(int id, int lodLevel, int[] clusterIndices, float screenSpaceErrorThreshold)
    {
        Id = id;
        LodLevel = lodLevel;
        ClusterIndices = clusterIndices;
        ScreenSpaceErrorThreshold = screenSpaceErrorThreshold;
    }

    #endregion
}
