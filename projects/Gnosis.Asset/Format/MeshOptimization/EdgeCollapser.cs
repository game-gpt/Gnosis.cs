namespace Gnosis.Asset.Format.MeshOptimization;

/// <summary>
/// 边折叠网格简化算法
/// 通过反复折叠代价最小的边来减少三角形数量
/// </summary>
public static class EdgeCollapser
{
    #region 内部类型

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

    #region 公开方法

    /// <summary>
    /// 对网格执行边折叠简化
    /// </summary>
    /// <param name="vertices">顶点列表</param>
    /// <param name="indices">索引缓冲区</param>
    /// <param name="targetRatio">目标三角形数量比例（0.0 - 1.0）</param>
    /// <returns>简化后的顶点列表和索引缓冲区</returns>
    public static (List<MeshVertex> Vertices, List<int> Indices) Simplify(
        IReadOnlyList<MeshVertex> vertices,
        IReadOnlyList<int> indices,
        float targetRatio)
    {
        int originalTriangleCount = indices.Count / 3;
        int targetTriangleCount = Math.Max(1, (int)(originalTriangleCount * targetRatio));

        if (originalTriangleCount <= targetTriangleCount)
        {
            return (vertices.ToList(), indices.ToList());
        }

        var positions = new float[vertices.Count][];
        for (int i = 0; i < vertices.Count; i++)
        {
            positions[i] = vertices[i].Position is { Length: >= 3 }
                ? [vertices[i].Position[0], vertices[i].Position[1], vertices[i].Position[2]]
                : new float[3];
        }

        var activeTriangles = new HashSet<int>();
        for (int i = 0; i < originalTriangleCount; i++)
        {
            activeTriangles.Add(i);
        }

        var triangleVertices = new int[originalTriangleCount][];
        for (int i = 0; i < originalTriangleCount; i++)
        {
            triangleVertices[i] =
            [
                indices[i * 3],
                indices[i * 3 + 1],
                indices[i * 3 + 2]
            ];
        }

        var vertexAlive = new bool[vertices.Count];
        Array.Fill(vertexAlive, true);

        var vertexTriangles = new HashSet<int>[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            vertexTriangles[i] = [];
        }

        for (int i = 0; i < originalTriangleCount; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                vertexTriangles[triangleVertices[i][j]].Add(i);
            }
        }

        var edges = new PriorityQueue<Edge, float>();
        var edgeSet = new HashSet<(int, int)>();

        for (int tri = 0; tri < originalTriangleCount; tri++)
        {
            for (int j = 0; j < 3; j++)
            {
                int v0 = triangleVertices[tri][j];
                int v1 = triangleVertices[tri][(j + 1) % 3];

                int eMin = Math.Min(v0, v1);
                int eMax = Math.Max(v0, v1);

                if (edgeSet.Add((eMin, eMax)))
                {
                    float cost = ComputeEdgeCost(positions, v0, v1);
                    edges.Enqueue(new Edge(v0, v1, cost), cost);
                }
            }
        }

        int currentTriangleCount = originalTriangleCount;

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
            foreach (int tri in vertexTriangles[edge.V0])
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

            foreach (int tri in sharedTriangles)
            {
                activeTriangles.Remove(tri);
                currentTriangleCount--;

                for (int j = 0; j < 3; j++)
                {
                    vertexTriangles[triangleVertices[tri][j]].Remove(tri);
                }
            }

            positions[edge.V0][0] = (positions[edge.V0][0] + positions[edge.V1][0]) * 0.5f;
            positions[edge.V0][1] = (positions[edge.V0][1] + positions[edge.V1][1]) * 0.5f;
            positions[edge.V0][2] = (positions[edge.V0][2] + positions[edge.V1][2]) * 0.5f;

            var v1Triangles = vertexTriangles[edge.V1].ToList();
            foreach (int tri in v1Triangles)
            {
                if (!activeTriangles.Contains(tri))
                {
                    continue;
                }

                for (int j = 0; j < 3; j++)
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
            foreach (int tri in vertexTriangles[edge.V0])
            {
                if (!activeTriangles.Contains(tri))
                {
                    continue;
                }

                for (int j = 0; j < 3; j++)
                {
                    int v = triangleVertices[tri][j];
                    if (v != edge.V0 && vertexAlive[v])
                    {
                        neighborVertices.Add(v);
                    }
                }
            }

            foreach (int v in neighborVertices)
            {
                int eMin = Math.Min(edge.V0, v);
                int eMax = Math.Max(edge.V0, v);

                float cost = ComputeEdgeCost(positions, edge.V0, v);
                edges.Enqueue(new Edge(edge.V0, v, cost), cost);
            }
        }

        return RebuildMesh(vertices, positions, triangleVertices, activeTriangles, vertexAlive);
    }

    #endregion

    #region 私有方法

    private static float ComputeEdgeCost(float[][] positions, int v0, int v1)
    {
        float dx = positions[v1][0] - positions[v0][0];
        float dy = positions[v1][1] - positions[v0][1];
        float dz = positions[v1][2] - positions[v0][2];

        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
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

        foreach (int tri in activeTriangles)
        {
            bool hasV0 = false;
            bool hasV1 = false;

            for (int j = 0; j < 3; j++)
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
                for (int j = 0; j < 3; j++)
                {
                    int v = triangleVertices[tri][j];
                    if (v != v0 && vertexAlive[v])
                    {
                        v0NeighborSet.Add(v);
                    }
                }
            }

            if (hasV1 && !hasV0)
            {
                for (int j = 0; j < 3; j++)
                {
                    int v = triangleVertices[tri][j];
                    if (v != v1 && vertexAlive[v])
                    {
                        v1NeighborSet.Add(v);
                    }
                }
            }
        }

        int sharedNeighborCount = 0;
        foreach (int v in v0NeighborSet)
        {
            if (v1NeighborSet.Contains(v))
            {
                sharedNeighborCount++;
            }
        }

        return sharedNeighborCount > 2;
    }

    private static (List<MeshVertex> Vertices, List<int> Indices) RebuildMesh(
        IReadOnlyList<MeshVertex> originalVertices,
        float[][] positions,
        int[][] triangleVertices,
        HashSet<int> activeTriangles,
        bool[] vertexAlive)
    {
        var indexRemap = new int[originalVertices.Count];
        Array.Fill(indexRemap, -1);

        var newVertices = new List<MeshVertex>();
        var newIndices = new List<int>();

        foreach (int tri in activeTriangles)
        {
            for (int j = 0; j < 3; j++)
            {
                int oldIdx = triangleVertices[tri][j];

                if (indexRemap[oldIdx] < 0)
                {
                    int newIdx = newVertices.Count;
                    indexRemap[oldIdx] = newIdx;

                    var original = originalVertices[oldIdx];
                    var newVertex = original with
                    {
                        Position = [positions[oldIdx][0], positions[oldIdx][1], positions[oldIdx][2]]
                    };
                    newVertices.Add(newVertex);
                }

                newIndices.Add(indexRemap[oldIdx]);
            }
        }

        return (newVertices, newIndices);
    }

    #endregion
}
