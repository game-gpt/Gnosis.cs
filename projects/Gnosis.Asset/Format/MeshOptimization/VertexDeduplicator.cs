namespace Gnosis.Asset.Format.MeshOptimization;

/// <summary>
/// 顶点去重工具，合并位置和属性相同的顶点，减少顶点缓冲区大小
/// </summary>
public static class VertexDeduplicator
{
    #region 公开方法

    /// <summary>
    /// 去除重复顶点，合并索引缓冲区中引用相同位置的顶点
    /// </summary>
    public static (List<MeshVertex> Vertices, List<int> Indices) Deduplicate(
        IReadOnlyList<MeshVertex> vertices,
        IReadOnlyList<int> indices)
    {
        if (vertices.Count == 0 || indices.Count == 0)
        {
            return (vertices.ToList(), indices.ToList());
        }

        var vertexMap = new Dictionary<int, int>();
        var newVertices = new List<MeshVertex>();
        var newIndices = new int[indices.Count];

        for (int i = 0; i < indices.Count; i++)
        {
            int oldIdx = indices[i];

            if (vertexMap.TryGetValue(oldIdx, out int newIdx))
            {
                newIndices[i] = newIdx;
                continue;
            }

            int matchedIdx = FindMatchingVertex(vertices, oldIdx, newVertices);

            if (matchedIdx >= 0)
            {
                vertexMap[oldIdx] = matchedIdx;
                newIndices[i] = matchedIdx;
            }
            else
            {
                int addedIdx = newVertices.Count;
                newVertices.Add(vertices[oldIdx]);
                vertexMap[oldIdx] = addedIdx;
                newIndices[i] = addedIdx;
            }
        }

        return (newVertices, newIndices.ToList());
    }

    #endregion

    #region 私有方法

    private static int FindMatchingVertex(IReadOnlyList<MeshVertex> vertices, int sourceIdx, List<MeshVertex> newVertices)
    {
        var source = vertices[sourceIdx];

        for (int i = 0; i < newVertices.Count; i++)
        {
            if (VerticesEqual(source, newVertices[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool VerticesEqual(MeshVertex a, MeshVertex b)
    {
        if (!ArraysEqual(a.Position, b.Position)) return false;
        if (!ArraysEqual(a.Normal, b.Normal)) return false;
        if (!ArraysEqual(a.Tangent, b.Tangent)) return false;
        if (!ArraysEqual(a.Uv, b.Uv)) return false;
        if (!ArraysEqual(a.Uv2, b.Uv2)) return false;
        if (!ArraysEqual(a.Color, b.Color)) return false;
        if (!ArraysEqual(a.BoneIndices, b.BoneIndices)) return false;
        if (!ArraysEqual(a.BoneWeights, b.BoneWeights)) return false;
        return true;
    }

    private static bool ArraysEqual(float[]? a, float[]? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (MathF.Abs(a[i] - b[i]) > 1e-6f) return false;
        }

        return true;
    }

    private static bool ArraysEqual(byte[]? a, byte[]? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }

        return true;
    }

    #endregion
}
