using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Geometry.Cluster;

/// <summary>
/// 网格簇生成器，将网格分割为固定大小的簇
/// 采用基于连通性的分割策略，优先将空间相邻的三角形分配到同一簇中
/// </summary>
public static class ClusterGenerator
{
    #region 公开方法

    /// <summary>
    /// 将网格分割为固定大小的簇
    /// </summary>
    /// <param name="positions">顶点位置数组</param>
    /// <param name="indices">索引缓冲区（每 3 个索引组成一个三角形）</param>
    /// <param name="trianglesPerCluster">每个簇的三角形数量，默认 128</param>
    /// <returns>生成的簇列表</returns>
    public static List<MeshCluster> Generate(
        Vector3[] positions,
        uint[] indices,
        int trianglesPerCluster = MeshCluster.DefaultTrianglesPerCluster)
    {
        if (positions.Length == 0 || indices.Length == 0)
        {
            return [];
        }

        var triangleCount = indices.Length / 3;

        if (triangleCount <= trianglesPerCluster)
        {
            return [CreateCluster(0, 0, positions, indices, 0, (uint)indices.Length)];
        }

        var adjacency = BuildAdjacency(indices, positions.Length);
        var triangleOrder = ComputeConnectivityOrder(triangleCount, adjacency);
        return CreateClustersFromOrder(triangleOrder, positions, indices, trianglesPerCluster);
    }

    /// <summary>
    /// 将网格分割为固定大小的簇（使用 int 索引）
    /// </summary>
    public static List<MeshCluster> Generate(
        Vector3[] positions,
        int[] indices,
        int trianglesPerCluster = MeshCluster.DefaultTrianglesPerCluster)
    {
        var uintIndices = new uint[indices.Length];
        for (var i = 0; i < indices.Length; i++)
        {
            uintIndices[i] = (uint)indices[i];
        }

        return Generate(positions, uintIndices, trianglesPerCluster);
    }

    #endregion

    #region 私有方法 - 邻接构建

    private sealed class Edge : IEquatable<Edge>
    {
        public uint V0 { get; }
        public uint V1 { get; }

        public Edge(uint v0, uint v1)
        {
            V0 = Math.Min(v0, v1);
            V1 = Math.Max(v0, v1);
        }

        public bool Equals(Edge? other)
        {
            if (other is null)
            {
                return false;
            }

            return V0 == other.V0 && V1 == other.V1;
        }

        public override bool Equals(object? obj) => Equals(obj as Edge);

        public override int GetHashCode() => HashCode.Combine(V0, V1);
    }

    private static Dictionary<Edge, List<int>> BuildAdjacency(uint[] indices, int vertexCount)
    {
        var adjacency = new Dictionary<Edge, List<int>>();
        var triangleCount = indices.Length / 3;

        for (var tri = 0; tri < triangleCount; tri++)
        {
            var i0 = indices[tri * 3];
            var i1 = indices[tri * 3 + 1];
            var i2 = indices[tri * 3 + 2];

            AddEdgeTriangle(adjacency, new Edge(i0, i1), tri);
            AddEdgeTriangle(adjacency, new Edge(i1, i2), tri);
            AddEdgeTriangle(adjacency, new Edge(i2, i0), tri);
        }

        return adjacency;
    }

    private static void AddEdgeTriangle(Dictionary<Edge, List<int>> adjacency, Edge edge, int triangleIndex)
    {
        if (!adjacency.TryGetValue(edge, out var list))
        {
            list = [];
            adjacency[edge] = list;
        }

        list.Add(triangleIndex);
    }

    #endregion

    #region 私有方法 - 连通性排序

    private static int[] ComputeConnectivityOrder(int triangleCount, Dictionary<Edge, List<int>> adjacency)
    {
        var visited = new bool[triangleCount];
        var order = new int[triangleCount];
        var orderIndex = 0;

        var edgeToNeighbors = BuildTriangleNeighborMap(triangleCount, adjacency);

        for (var startTri = 0; startTri < triangleCount; startTri++)
        {
            if (visited[startTri])
            {
                continue;
            }

            var queue = new Queue<int>();
            queue.Enqueue(startTri);
            visited[startTri] = true;

            while (queue.Count > 0)
            {
                var tri = queue.Dequeue();
                order[orderIndex++] = tri;

                if (edgeToNeighbors.TryGetValue(tri, out var neighbors))
                {
                    foreach (var neighbor in neighbors)
                    {
                        if (!visited[neighbor])
                        {
                            visited[neighbor] = true;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
        }

        return order;
    }

    private static Dictionary<int, List<int>> BuildTriangleNeighborMap(
        int triangleCount,
        Dictionary<Edge, List<int>> adjacency)
    {
        var neighborMap = new Dictionary<int, List<int>>();

        for (var tri = 0; tri < triangleCount; tri++)
        {
            neighborMap[tri] = [];
        }

        foreach (var kvp in adjacency)
        {
            var triangleList = kvp.Value;

            for (var i = 0; i < triangleList.Count; i++)
            {
                for (var j = i + 1; j < triangleList.Count; j++)
                {
                    neighborMap[triangleList[i]].Add(triangleList[j]);
                    neighborMap[triangleList[j]].Add(triangleList[i]);
                }
            }
        }

        return neighborMap;
    }

    #endregion

    #region 私有方法 - 簇创建

    private static List<MeshCluster> CreateClustersFromOrder(
        int[] triangleOrder,
        Vector3[] positions,
        uint[] indices,
        int trianglesPerCluster)
    {
        var clusters = new List<MeshCluster>();
        var triangleCount = triangleOrder.Length;
        var clusterId = 0;

        var triIndex = 0;

        while (triIndex < triangleCount)
        {
            var remaining = triangleCount - triIndex;
            var clusterTriCount = Math.Min(remaining, trianglesPerCluster);

            var clusterIndices = new uint[clusterTriCount * 3];
            var vertexRemap = new Dictionary<uint, uint>();
            var clusterPositions = new List<Vector3>();
            var nextVertex = 0u;

            for (var t = 0; t < clusterTriCount; t++)
            {
                var srcTri = triangleOrder[triIndex + t];

                for (var v = 0; v < 3; v++)
                {
                    var srcVertex = indices[srcTri * 3 + v];

                    if (!vertexRemap.TryGetValue(srcVertex, out var localVertex))
                    {
                        localVertex = nextVertex++;
                        vertexRemap[srcVertex] = localVertex;
                        clusterPositions.Add(positions[srcVertex]);
                    }

                    clusterIndices[t * 3 + v] = localVertex;
                }
            }

            var bounds = ClusterBounds.FromPositions(clusterPositions.ToArray());

            clusters.Add(new MeshCluster(
                clusterId++,
                0,
                clusterPositions.ToArray(),
                clusterIndices,
                bounds));

            triIndex += clusterTriCount;
        }

        return clusters;
    }

    private static MeshCluster CreateCluster(
        int id,
        int lodLevel,
        Vector3[] positions,
        uint[] indices,
        uint indexStart,
        uint indexCount)
    {
        var clusterIndices = new uint[indexCount];
        var vertexRemap = new Dictionary<uint, uint>();
        var clusterPositions = new List<Vector3>();
        var nextVertex = 0u;

        for (var i = 0u; i < indexCount; i++)
        {
            var srcVertex = indices[indexStart + i];

            if (!vertexRemap.TryGetValue(srcVertex, out var localVertex))
            {
                localVertex = nextVertex++;
                vertexRemap[srcVertex] = localVertex;
                clusterPositions.Add(positions[srcVertex]);
            }

            clusterIndices[i] = localVertex;
        }

        var bounds = ClusterBounds.FromPositions(clusterPositions.ToArray());

        return new MeshCluster(id, lodLevel, clusterPositions.ToArray(), clusterIndices, bounds);
    }

    #endregion
}
