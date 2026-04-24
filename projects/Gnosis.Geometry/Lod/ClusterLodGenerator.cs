using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Geometry.Cluster;

namespace Gnosis.Geometry.Lod;

/// <summary>
/// 簇 LOD 生成器，基于边折叠简化算法构建 LOD DAG
/// 从最高精度的簇开始，逐级简化生成更粗略的 LOD 级别
/// </summary>
public sealed class ClusterLodGenerator
{
    #region 嵌套类型

    /// <summary>
    /// LOD 生成配置
    /// </summary>
    public sealed class Config
    {
        /// <summary>
        /// 最大 LOD 级别数量
        /// </summary>
        public int MaxLodLevels { get; init; } = 8;

        /// <summary>
        /// 每级 LOD 的简化比例（0.0 - 1.0）
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
    }

    private sealed class Edge : IComparable<Edge>
    {
        public int V0 { get; }
        public int V1 { get; }
        public float Cost { get; }

        public Edge(int v0, int v1, float cost)
        {
            V0 = Math.Min(v0, v1);
            V1 = Math.Max(v0, v1);
            Cost = cost;
        }

        public int CompareTo(Edge? other)
        {
            if (other is null)
            {
                return 1;
            }

            return Cost.CompareTo(other.Cost);
        }
    }

    #endregion

    #region 字段

    private readonly Config _config;

    #endregion

    #region 构造函数

    public ClusterLodGenerator(Config? config = null)
    {
        _config = config ?? new Config();
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 从原始网格数据生成完整的 LOD DAG
    /// </summary>
    /// <param name="positions">顶点位置数组</param>
    /// <param name="indices">索引缓冲区</param>
    /// <returns>LOD DAG</returns>
    public LodDag Generate(Vector3[] positions, uint[] indices)
    {
        var clusters = ClusterGenerator.Generate(positions, indices);
        return GenerateFromClusters(clusters);
    }

    /// <summary>
    /// 从已有簇列表生成 LOD DAG
    /// </summary>
    public LodDag GenerateFromClusters(List<MeshCluster> clusters)
    {
        var allClusters = new List<MeshCluster>(clusters);
        var groups = new List<MeshClusterGroup>();
        var nodes = new List<LodNode>();

        var currentClusters = clusters;
        var lodLevel = 0;
        var nodeId = 0;

        var leafNodeIndices = new List<int>();

        while (currentClusters.Count > 0 && lodLevel < _config.MaxLodLevels)
        {
            var clusterGroups = CreateClusterGroups(currentClusters, groups.Count, lodLevel);
            groups.AddRange(clusterGroups);

            foreach (var group in clusterGroups)
            {
                var clusterIndices = group.ClusterIndices;
                var node = new LodNode(
                    nodeId++,
                    lodLevel,
                    clusterIndices,
                    ComputeScreenSpaceErrorThreshold(lodLevel));

                nodes.Add(node);

                if (lodLevel == 0)
                {
                    leafNodeIndices.Add(node.Id);
                }
            }

            if (currentClusters.Count <= _config.ClusterGroupSize &&
                lodLevel > 0)
            {
                break;
            }

            var simplifiedClusters = SimplifyClusters(currentClusters, lodLevel + 1);
            if (simplifiedClusters.Count == 0 ||
                simplifiedClusters.Sum(c => c.TriangleCount) < _config.MinTriangles)
            {
                break;
            }

            currentClusters = simplifiedClusters;
            lodLevel++;
        }

        LinkParentChildRelations(nodes, groups);

        var rootIndex = FindRootIndex(nodes);

        return new LodDag(nodes, allClusters, groups, rootIndex, lodLevel + 1);
    }

    #endregion

    #region 私有方法 - 簇组创建

    private List<MeshClusterGroup> CreateClusterGroups(
        List<MeshCluster> clusters,
        int groupIdOffset,
        int lodLevel)
    {
        var groups = new List<MeshClusterGroup>();
        var groupSize = _config.ClusterGroupSize;

        for (var i = 0; i < clusters.Count; i += groupSize)
        {
            var count = Math.Min(groupSize, clusters.Count - i);
            var clusterIndices = new int[count];

            for (var j = 0; j < count; j++)
            {
                clusterIndices[j] = i + j;
                clusters[i + j].GroupIndex = groups.Count + groupIdOffset;
            }

            var threshold = ComputeScreenSpaceErrorThreshold(lodLevel);
            var group = MeshClusterGroup.FromClusters(
                groups.Count + groupIdOffset,
                lodLevel,
                clusters,
                clusterIndices,
                threshold);

            groups.Add(group);
        }

        return groups;
    }

    #endregion

    #region 私有方法 - 边折叠简化

    private List<MeshCluster> SimplifyClusters(List<MeshCluster> clusters, int newLodLevel)
    {
        var simplifiedClusters = new List<MeshCluster>();
        var clusterId = 0;

        foreach (var cluster in clusters)
        {
            var simplified = SimplifySingleCluster(cluster, clusterId++, newLodLevel);
            if (simplified != null)
            {
                simplifiedClusters.Add(simplified);
            }
        }

        return MergeSmallClusters(simplifiedClusters, newLodLevel);
    }

    private MeshCluster? SimplifySingleCluster(MeshCluster cluster, int newId, int newLodLevel)
    {
        var targetTriCount = (int)(cluster.TriangleCount * _config.SimplificationRatio);

        if (targetTriCount < _config.MinTriangles)
        {
            return null;
        }

        if (cluster.TriangleCount <= targetTriCount)
        {
            return new MeshCluster(newId, newLodLevel, cluster.Positions, cluster.Indices, cluster.Bounds);
        }

        var (newPositions, newIndices) = SimplifyMesh(
            cluster.Positions,
            cluster.Indices,
            (float)targetTriCount / cluster.TriangleCount);

        if (newPositions.Length == 0 || newIndices.Length == 0)
        {
            return null;
        }

        var bounds = ClusterBounds.FromPositions(newPositions);

        return new MeshCluster(newId, newLodLevel, newPositions, newIndices, bounds);
    }

    private static (Vector3[] Positions, uint[] Indices) SimplifyMesh(
        Vector3[] positions,
        uint[] indices,
        float targetRatio)
    {
        var triangleCount = indices.Length / 3;
        var targetTriangleCount = Math.Max(1, (int)(triangleCount * targetRatio));

        if (triangleCount <= targetTriangleCount)
        {
            return (positions, indices);
        }

        var vertexAlive = new bool[positions.Length];
        Array.Fill(vertexAlive, true);

        var triangleVertices = new int[triangleCount][];
        var activeTriangles = new HashSet<int>();

        for (var i = 0; i < triangleCount; i++)
        {
            triangleVertices[i] =
            [
                (int)indices[i * 3],
                (int)indices[i * 3 + 1],
                (int)indices[i * 3 + 2]
            ];
            activeTriangles.Add(i);
        }

        var vertexTriangles = new HashSet<int>[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            vertexTriangles[i] = [];
        }

        for (var i = 0; i < triangleCount; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                vertexTriangles[triangleVertices[i][j]].Add(i);
            }
        }

        var edges = new PriorityQueue<Edge, float>();
        var edgeSet = new HashSet<(int, int)>();

        for (var tri = 0; tri < triangleCount; tri++)
        {
            for (var j = 0; j < 3; j++)
            {
                var v0 = triangleVertices[tri][j];
                var v1 = triangleVertices[tri][(j + 1) % 3];

                var eMin = Math.Min(v0, v1);
                var eMax = Math.Max(v0, v1);

                if (edgeSet.Add((eMin, eMax)))
                {
                    var cost = ComputeEdgeCost(positions, v0, v1);
                    edges.Enqueue(new Edge(v0, v1, cost), cost);
                }
            }
        }

        var currentTriangleCount = triangleCount;

        while (currentTriangleCount > targetTriangleCount && edges.Count > 0)
        {
            var edge = edges.Dequeue();

            if (!vertexAlive[edge.V0] || !vertexAlive[edge.V1])
            {
                continue;
            }

            if (WouldCreateDegenerate(triangleVertices, vertexAlive, activeTriangles, edge.V0, edge.V1))
            {
                continue;
            }

            var sharedTriangles = new List<int>();
            foreach (var tri in vertexTriangles[edge.V0])
            {
                if (activeTriangles.Contains(tri) && vertexTriangles[edge.V1].Contains(tri))
                {
                    sharedTriangles.Add(tri);
                }
            }

            if (currentTriangleCount - sharedTriangles.Count < targetTriangleCount)
            {
                continue;
            }

            foreach (var tri in sharedTriangles)
            {
                activeTriangles.Remove(tri);
                currentTriangleCount--;

                for (var j = 0; j < 3; j++)
                {
                    vertexTriangles[triangleVertices[tri][j]].Remove(tri);
                }
            }

            positions[edge.V0] = (positions[edge.V0] + positions[edge.V1]) * 0.5f;

            var v1Triangles = vertexTriangles[edge.V1].ToList();
            foreach (var tri in v1Triangles)
            {
                if (!activeTriangles.Contains(tri))
                {
                    continue;
                }

                for (var j = 0; j < 3; j++)
                {
                    if (triangleVertices[tri][j] == edge.V1)
                    {
                        triangleVertices[tri][j] = edge.V0;
                    }
                }

                vertexTriangles[edge.V0].Add(tri);
            }

            vertexTriangles[edge.V1].Clear();
            vertexAlive[edge.V1] = false;

            var neighborVertices = new HashSet<int>();
            foreach (var tri in vertexTriangles[edge.V0])
            {
                if (!activeTriangles.Contains(tri))
                {
                    continue;
                }

                for (var j = 0; j < 3; j++)
                {
                    var v = triangleVertices[tri][j];
                    if (v != edge.V0 && vertexAlive[v])
                    {
                        neighborVertices.Add(v);
                    }
                }
            }

            foreach (var v in neighborVertices)
            {
                var cost = ComputeEdgeCost(positions, edge.V0, v);
                edges.Enqueue(new Edge(edge.V0, v, cost), cost);
            }
        }

        return RebuildMesh(positions, triangleVertices, activeTriangles, vertexAlive);
    }

    private static float ComputeEdgeCost(Vector3[] positions, int v0, int v1)
    {
        return Vector3.Distance(positions[v0], positions[v1]);
    }

    private static bool WouldCreateDegenerate(
        int[][] triangleVertices,
        bool[] vertexAlive,
        HashSet<int> activeTriangles,
        int v0,
        int v1)
    {
        var v0NeighborSet = new HashSet<int>();
        var v1NeighborSet = new HashSet<int>();

        foreach (var tri in activeTriangles)
        {
            var hasV0 = false;
            var hasV1 = false;

            for (var j = 0; j < 3; j++)
            {
                if (triangleVertices[tri][j] == v0)
                {
                    hasV0 = true;
                }

                if (triangleVertices[tri][j] == v1)
                {
                    hasV1 = true;
                }
            }

            if (hasV0 && !hasV1)
            {
                for (var j = 0; j < 3; j++)
                {
                    var v = triangleVertices[tri][j];
                    if (v != v0 && vertexAlive[v])
                    {
                        v0NeighborSet.Add(v);
                    }
                }
            }

            if (hasV1 && !hasV0)
            {
                for (var j = 0; j < 3; j++)
                {
                    var v = triangleVertices[tri][j];
                    if (v != v1 && vertexAlive[v])
                    {
                        v1NeighborSet.Add(v);
                    }
                }
            }
        }

        var sharedNeighborCount = 0;
        foreach (var v in v0NeighborSet)
        {
            if (v1NeighborSet.Contains(v))
            {
                sharedNeighborCount++;
            }
        }

        return sharedNeighborCount > 2;
    }

    private static (Vector3[] Positions, uint[] Indices) RebuildMesh(
        Vector3[] positions,
        int[][] triangleVertices,
        HashSet<int> activeTriangles,
        bool[] vertexAlive)
    {
        var indexRemap = new int[positions.Length];
        Array.Fill(indexRemap, -1);

        var newPositions = new List<Vector3>();
        var newIndices = new List<uint>();

        foreach (var tri in activeTriangles)
        {
            for (var j = 0; j < 3; j++)
            {
                var oldIdx = triangleVertices[tri][j];

                if (indexRemap[oldIdx] < 0)
                {
                    var newIdx = newPositions.Count;
                    indexRemap[oldIdx] = newIdx;
                    newPositions.Add(positions[oldIdx]);
                }

                newIndices.Add((uint)indexRemap[oldIdx]);
            }
        }

        return (newPositions.ToArray(), newIndices.ToArray());
    }

    #endregion

    #region 私有方法 - 簇合并

    private List<MeshCluster> MergeSmallClusters(List<MeshCluster> clusters, int lodLevel)
    {
        if (clusters.Count <= 1)
        {
            return clusters;
        }

        var merged = new List<MeshCluster>();
        var clusterId = 0;

        var i = 0;
        while (i < clusters.Count)
        {
            if (clusters[i].TriangleCount >= MeshCluster.DefaultTrianglesPerCluster / 2 ||
                i + 1 >= clusters.Count)
            {
                merged.Add(clusters[i]);
                i++;
                continue;
            }

            var nextIdx = i + 1;
            var combinedPositions = clusters[i].Positions.Concat(clusters[nextIdx].Positions).ToArray();
            var offset = (uint)clusters[i].Positions.Length;
            var combinedIndices = clusters[i].Indices
                .Concat(clusters[nextIdx].Indices.Select(idx => idx + offset))
                .ToArray();

            var bounds = ClusterBounds.FromPositions(combinedPositions);

            merged.Add(new MeshCluster(clusterId++, lodLevel, combinedPositions, combinedIndices, bounds));
            i += 2;
        }

        return merged;
    }

    #endregion

    #region 私有方法 - DAG 构建

    private float ComputeScreenSpaceErrorThreshold(int lodLevel)
    {
        return _config.BaseScreenSpaceErrorThreshold *
               MathF.Pow(_config.ScreenSpaceErrorMultiplier, lodLevel);
    }

    private static void LinkParentChildRelations(List<LodNode> nodes, List<MeshClusterGroup> groups)
    {
        if (nodes.Count <= 1)
        {
            return;
        }

        var nodesByLevel = new Dictionary<int, List<int>>();
        foreach (var node in nodes)
        {
            if (!nodesByLevel.TryGetValue(node.LodLevel, out var list))
            {
                list = [];
                nodesByLevel[node.LodLevel] = list;
            }

            list.Add(node.Id);
        }

        var sortedLevels = nodesByLevel.Keys.OrderBy(l => l).ToList();

        for (var levelIdx = 1; levelIdx < sortedLevels.Count; levelIdx++)
        {
            var currentLevel = sortedLevels[levelIdx];
            var parentLevel = sortedLevels[levelIdx - 1];

            var currentNodes = nodesByLevel[currentLevel];
            var parentNodes = nodesByLevel[parentLevel];

            var parentPerChild = Math.Max(1, parentNodes.Count / Math.Max(1, currentNodes.Count));

            for (var i = 0; i < currentNodes.Count; i++)
            {
                var parentNodeIdx = Math.Min(i / parentPerChild, parentNodes.Count - 1);
                var parentId = parentNodes[parentNodeIdx];

                nodes[currentNodes[i]].ParentIndex = parentId;
                nodes[parentId].ChildIndices.Add(currentNodes[i]);
            }
        }
    }

    private static int FindRootIndex(List<LodNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.ParentIndex == -1)
            {
                return node.Id;
            }
        }

        return 0;
    }

    #endregion
}
