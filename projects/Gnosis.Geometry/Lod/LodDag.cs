using Gnosis.Geometry.Cluster;

namespace Gnosis.Geometry.Lod;

/// <summary>
/// LOD DAG（有向无环图），描述网格的 LOD 层级结构
/// 叶子节点是最高精度的簇组，根节点是最粗略的簇组
/// 运行时根据屏幕空间误差在 DAG 中选择合适的 LOD 节点
/// </summary>
public sealed class LodDag
{
    #region 属性

    /// <summary>
    /// DAG 中所有节点
    /// </summary>
    public IReadOnlyList<LodNode> Nodes => _nodes;

    /// <summary>
    /// 根节点索引（最粗略的 LOD）
    /// </summary>
    public int RootIndex { get; }

    /// <summary>
    /// LOD 级别数量
    /// </summary>
    public int LodLevelCount { get; }

    /// <summary>
    /// 所有簇的引用（跨 LOD 级别共享）
    /// </summary>
    public IReadOnlyList<MeshCluster> Clusters => _clusters;

    /// <summary>
    /// 所有簇组的引用
    /// </summary>
    public IReadOnlyList<MeshClusterGroup> Groups => _groups;

    #endregion

    #region 字段

    private readonly List<LodNode> _nodes;
    private readonly List<MeshCluster> _clusters;
    private readonly List<MeshClusterGroup> _groups;

    #endregion

    #region 构造函数

    public LodDag(
        List<LodNode> nodes,
        List<MeshCluster> clusters,
        List<MeshClusterGroup> groups,
        int rootIndex,
        int lodLevelCount)
    {
        _nodes = nodes;
        _clusters = clusters;
        _groups = groups;
        RootIndex = rootIndex;
        LodLevelCount = lodLevelCount;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 根据屏幕空间误差选择可见的簇集合
    /// 从根节点开始遍历 DAG，当屏幕空间误差低于阈值时停止细分
    /// </summary>
    /// <param name="errorCalculator">屏幕空间误差计算委托</param>
    /// <returns>需要渲染的簇索引集合</returns>
    public HashSet<int> SelectVisibleClusters(Func<LodNode, float> errorCalculator)
    {
        var visibleClusters = new HashSet<int>();
        var stack = new Stack<int>();

        stack.Push(RootIndex);

        while (stack.Count > 0)
        {
            var nodeIndex = stack.Pop();
            var node = _nodes[nodeIndex];
            var error = errorCalculator(node);

            if (node.IsLeaf || !ScreenSpaceError.ShouldRefine(error, node.ScreenSpaceErrorThreshold))
            {
                foreach (var clusterIdx in node.ClusterIndices)
                {
                    visibleClusters.Add(clusterIdx);
                }
            }
            else
            {
                foreach (var childIdx in node.ChildIndices)
                {
                    stack.Push(childIdx);
                }
            }
        }

        return visibleClusters;
    }

    /// <summary>
    /// 获取指定 LOD 级别的所有节点
    /// </summary>
    public List<LodNode> GetNodesAtLevel(int lodLevel)
    {
        var result = new List<LodNode>();

        foreach (var node in _nodes)
        {
            if (node.LodLevel == lodLevel)
            {
                result.Add(node);
            }
        }

        return result;
    }

    /// <summary>
    /// 获取叶子节点列表
    /// </summary>
    public List<LodNode> GetLeafNodes()
    {
        var result = new List<LodNode>();

        foreach (var node in _nodes)
        {
            if (node.IsLeaf)
            {
                result.Add(node);
            }
        }

        return result;
    }

    #endregion
}
