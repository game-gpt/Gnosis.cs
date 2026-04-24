namespace Gnosis.Asset.Format.MeshOptimization;

/// <summary>
/// 基于二次误差度量（QEM）的网格简化算法
/// 实现 Garland-Heckbert 算法，使用二次误差矩阵计算边折叠代价
/// </summary>
public static class QemSimplifier
{
    #region 内部类型

    private struct Quadric
    {
        public double A2, AB, AC, AD;
        public double B2, BC, BD;
        public double C2, CD;
        public double D2;

        public static Quadric Zero => new();

        public static Quadric operator +(Quadric a, Quadric b)
        {
            return new Quadric
            {
                A2 = a.A2 + b.A2,
                AB = a.AB + b.AB,
                AC = a.AC + b.AC,
                AD = a.AD + b.AD,
                B2 = a.B2 + b.B2,
                BC = a.BC + b.BC,
                BD = a.BD + b.BD,
                C2 = a.C2 + b.C2,
                CD = a.CD + b.CD,
                D2 = a.D2 + b.D2
            };
        }

        public double Evaluate(float x, float y, float z)
        {
            double dx = x, dy = y, dz = z;
            return A2 * dx * dx + 2 * AB * dx * dy + 2 * AC * dx * dz + 2 * AD * dx
                   + B2 * dy * dy + 2 * BC * dy * dz + 2 * BD * dy
                   + C2 * dz * dz + 2 * CD * dz
                   + D2;
        }

        public bool TryComputeOptimalPosition(out float x, out float y, out float z)
        {
            double det = A2 * (B2 * C2 - BC * BC)
                         - AB * (AB * C2 - AC * BC)
                         + AC * (AB * BC - AC * B2);

            if (Math.Abs(det) < 1e-12)
            {
                x = y = z = 0;
                return false;
            }

            double invDet = 1.0 / det;

            double rx = (AD * (BC * BC - B2 * C2) + BD * (AC * BC - AB * C2) + CD * (AB * BC - AC * B2)) * invDet;
            double ry = (AD * (AC * BC - AB * C2) + BD * (A2 * C2 - AC * AC) + CD * (AB * AC - A2 * BC)) * invDet;
            double rz = (AD * (AB * BC - AC * B2) + BD * (AB * AC - A2 * BC) + CD * (A2 * B2 - AB * AB)) * invDet;

            if (!float.IsFinite((float)rx) || !float.IsFinite((float)ry) || !float.IsFinite((float)rz))
            {
                x = y = z = 0;
                return false;
            }

            x = (float)rx;
            y = (float)ry;
            z = (float)rz;
            return true;
        }
    }

    private sealed class Edge : IComparable<Edge>
    {
        public int V0 { get; }
        public int V1 { get; }
        public float Cost { get; }
        public float TargetX { get; }
        public float TargetY { get; }
        public float TargetZ { get; }

        public Edge(int v0, int v1, float cost, float tx, float ty, float tz)
        {
            V0 = Math.Min(v0, v1);
            V1 = Math.Max(v0, v1);
            Cost = cost;
            TargetX = tx;
            TargetY = ty;
            TargetZ = tz;
        }

        public int CompareTo(Edge? other)
        {
            if (other is null) return 1;
            return Cost.CompareTo(other.Cost);
        }
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 使用 QEM 算法对网格执行边折叠简化
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
        int targetTriangleCount = Math.Max(4, (int)(originalTriangleCount * targetRatio));

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

        var quadrics = ComputeVertexQuadrics(positions, indices);

        var activeTriangles = new HashSet<int>();
        for (int i = 0; i < originalTriangleCount; i++)
        {
            activeTriangles.Add(i);
        }

        var triangleVertices = new int[originalTriangleCount][];
        for (int i = 0; i < originalTriangleCount; i++)
        {
            triangleVertices[i] = [indices[i * 3], indices[i * 3 + 1], indices[i * 3 + 2]];
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
                    var edge = ComputeEdgeCost(positions, quadrics, v0, v1);
                    edges.Enqueue(edge, edge.Cost);
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

            positions[edge.V0][0] = edge.TargetX;
            positions[edge.V0][1] = edge.TargetY;
            positions[edge.V0][2] = edge.TargetZ;

            quadrics[edge.V0] = quadrics[edge.V0] + quadrics[edge.V1];

            var v1Triangles = vertexTriangles[edge.V1].ToList();
            foreach (int tri in v1Triangles)
            {
                if (!activeTriangles.Contains(tri)) continue;

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
                if (!activeTriangles.Contains(tri)) continue;

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
                var newEdge = ComputeEdgeCost(positions, quadrics, edge.V0, v);
                edges.Enqueue(newEdge, newEdge.Cost);
            }
        }

        return RebuildMesh(vertices, positions, triangleVertices, activeTriangles, vertexAlive);
    }

    #endregion

    #region QEM 计算

    /// <summary>
    /// 为每个顶点计算二次误差矩阵
    /// </summary>
    private static Quadric[] ComputeVertexQuadrics(float[][] positions, IReadOnlyList<int> indices)
    {
        var quadrics = new Quadric[positions.Length];
        int triangleCount = indices.Count / 3;

        for (int tri = 0; tri < triangleCount; tri++)
        {
            int i0 = indices[tri * 3];
            int i1 = indices[tri * 3 + 1];
            int i2 = indices[tri * 3 + 2];

            float ax = positions[i1][0] - positions[i0][0];
            float ay = positions[i1][1] - positions[i0][1];
            float az = positions[i1][2] - positions[i0][2];

            float bx = positions[i2][0] - positions[i0][0];
            float by = positions[i2][1] - positions[i0][1];
            float bz = positions[i2][2] - positions[i0][2];

            float nx = ay * bz - az * by;
            float ny = az * bx - ax * bz;
            float nz = ax * by - ay * bx;

            float len = MathF.Sqrt(nx * nx + ny * ny + nz * nz);
            if (len < 1e-10f) continue;

            nx /= len;
            ny /= len;
            nz /= len;

            float d = -(nx * positions[i0][0] + ny * positions[i0][1] + nz * positions[i0][2]);

            var faceQuadric = new Quadric
            {
                A2 = nx * nx,
                AB = nx * ny,
                AC = nx * nz,
                AD = nx * d,
                B2 = ny * ny,
                BC = ny * nz,
                BD = ny * d,
                C2 = nz * nz,
                CD = nz * d,
                D2 = d * d
            };

            quadrics[i0] = quadrics[i0] + faceQuadric;
            quadrics[i1] = quadrics[i1] + faceQuadric;
            quadrics[i2] = quadrics[i2] + faceQuadric;
        }

        return quadrics;
    }

    /// <summary>
    /// 计算边折叠代价和最优折叠位置
    /// </summary>
    private static Edge ComputeEdgeCost(float[][] positions, Quadric[] quadrics, int v0, int v1)
    {
        var combined = quadrics[v0] + quadrics[v1];

        float targetX, targetY, targetZ;

        if (combined.TryComputeOptimalPosition(out targetX, out targetY, out targetZ))
        {
            float dx0 = targetX - positions[v0][0];
            float dy0 = targetY - positions[v0][1];
            float dz0 = targetZ - positions[v0][2];
            float dist0Sq = dx0 * dx0 + dy0 * dy0 + dz0 * dz0;

            float dx1 = targetX - positions[v1][0];
            float dy1 = targetY - positions[v1][1];
            float dz1 = targetZ - positions[v1][2];
            float dist1Sq = dx1 * dx1 + dy1 * dy1 + dz1 * dz1;

            float maxDistSq = Math.Max(dist0Sq, dist1Sq);
            if (maxDistSq > (positions[v0][0] - positions[v1][0]) * (positions[v0][0] - positions[v1][0])
                          + (positions[v0][1] - positions[v1][1]) * (positions[v0][1] - positions[v1][1])
                          + (positions[v0][2] - positions[v1][2]) * (positions[v0][2] - positions[v1][2]) * 4)
            {
                targetX = (positions[v0][0] + positions[v1][0]) * 0.5f;
                targetY = (positions[v0][1] + positions[v1][1]) * 0.5f;
                targetZ = (positions[v0][2] + positions[v1][2]) * 0.5f;
            }
        }
        else
        {
            float costV0 = (float)quadrics[v0].Evaluate(positions[v0][0], positions[v0][1], positions[v0][2]);
            float costV1 = (float)quadrics[v1].Evaluate(positions[v1][0], positions[v1][1], positions[v1][2]);

            if (costV0 <= costV1)
            {
                targetX = positions[v0][0];
                targetY = positions[v0][1];
                targetZ = positions[v0][2];
            }
            else
            {
                targetX = positions[v1][0];
                targetY = positions[v1][1];
                targetZ = positions[v1][2];
            }
        }

        double error = combined.Evaluate(targetX, targetY, targetZ);
        float cost = MathF.Max(0f, (float)error);

        return new Edge(v0, v1, cost, targetX, targetY, targetZ);
    }

    #endregion

    #region 退化检测

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
                if (triangleVertices[tri][j] == v0) hasV0 = true;
                if (triangleVertices[tri][j] == v1) hasV1 = true;
            }

            if (hasV0 && !hasV1)
            {
                for (int j = 0; j < 3; j++)
                {
                    int v = triangleVertices[tri][j];
                    if (v != v0 && vertexAlive[v]) v0NeighborSet.Add(v);
                }
            }

            if (hasV1 && !hasV0)
            {
                for (int j = 0; j < 3; j++)
                {
                    int v = triangleVertices[tri][j];
                    if (v != v1 && vertexAlive[v]) v1NeighborSet.Add(v);
                }
            }
        }

        int sharedNeighborCount = 0;
        foreach (int v in v0NeighborSet)
        {
            if (v1NeighborSet.Contains(v)) sharedNeighborCount++;
        }

        return sharedNeighborCount > 2;
    }

    #endregion

    #region 网格重建

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
