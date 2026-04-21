namespace Gnosis.Assets.Formats.MeshOptimization;

/// <summary>
/// Tom Forsyth 顶点缓存优化算法实现
/// 通过重排三角形索引顺序来提升 GPU 顶点缓存命中率
/// </summary>
public static class ForsythVertexCacheOptimizer
{
    #region 常量

    private const int VertexCacheSize = 24;

    private const int MaxPrecomputedCachePosition = VertexCacheSize;

    private const float LastTriScore = 0.6f;

    private const float CacheDecayPower = 1.5f;

    private const float ValenceBoostScale = 2.0f;

    private const float ValenceBoostPower = 0.5f;

    #endregion

    #region 公开方法

    /// <summary>
    /// 对索引缓冲区执行 Forsyth 顶点缓存优化
    /// </summary>
    /// <param name="indices">原始索引缓冲区</param>
    /// <returns>优化后的索引缓冲区</returns>
    public static int[] Optimize(int[] indices)
    {
        if (indices.Length < 3)
        {
            return (int[])indices.Clone();
        }

        var triangleCount = indices.Length / 3;
        var maxVertexIndex = 0;

        for (var i = 0; i < indices.Length; i++)
        {
            if (indices[i] > maxVertexIndex)
            {
                maxVertexIndex = indices[i];
            }
        }

        var vertexCount = maxVertexIndex + 1;

        var cachePosition = new int[vertexCount];
        var vertexScore = new float[vertexCount];
        var remainingTriangleCount = new int[vertexCount];
        var vertexTriangleList = new List<int>[vertexCount];

        for (var i = 0; i < vertexCount; i++)
        {
            cachePosition[i] = -1;
            vertexTriangleList[i] = new List<int>();
        }

        for (var i = 0; i < triangleCount; i++)
        {
            var i0 = indices[i * 3];
            var i1 = indices[i * 3 + 1];
            var i2 = indices[i * 3 + 2];

            vertexTriangleList[i0].Add(i);
            vertexTriangleList[i1].Add(i);
            vertexTriangleList[i2].Add(i);

            remainingTriangleCount[i0]++;
            remainingTriangleCount[i1]++;
            remainingTriangleCount[i2]++;
        }

        for (var i = 0; i < vertexCount; i++)
        {
            vertexScore[i] = ComputeVertexScore(cachePosition[i], remainingTriangleCount[i]);
        }

        var result = new int[indices.Length];
        var resultIndex = 0;

        var cache = new int[VertexCacheSize + 3];
        var cacheSize = 0;

        var triangleAdded = new bool[triangleCount];
        var triangleScore = new float[triangleCount];

        for (var i = 0; i < triangleCount; i++)
        {
            triangleScore[i] = ComputeTriangleScore(indices, i, vertexScore);
        }

        var bestTriangleScore = float.MaxValue;
        var bestTriangleIndex = -1;

        for (var i = 0; i < triangleCount; i++)
        {
            var score = triangleScore[i];
            if (score > bestTriangleScore || bestTriangleIndex == -1)
            {
                bestTriangleScore = score;
                bestTriangleIndex = i;
            }
        }

        for (var outIndex = 0; outIndex < triangleCount; outIndex++)
        {
            if (bestTriangleIndex == -1)
            {
                bestTriangleIndex = FindBestTriangle(triangleScore, triangleAdded, triangleCount);
            }

            var i0 = indices[bestTriangleIndex * 3];
            var i1 = indices[bestTriangleIndex * 3 + 1];
            var i2 = indices[bestTriangleIndex * 3 + 2];

            result[resultIndex++] = i0;
            result[resultIndex++] = i1;
            result[resultIndex++] = i2;

            triangleAdded[bestTriangleIndex] = true;
            triangleScore[bestTriangleIndex] = -1.0f;

            remainingTriangleCount[i0]--;
            remainingTriangleCount[i1]--;
            remainingTriangleCount[i2]--;

            cacheSize = AddVertexToCache(cache, cacheSize, i0);
            cacheSize = AddVertexToCache(cache, cacheSize, i1);
            cacheSize = AddVertexToCache(cache, cacheSize, i2);

            for (var v = 0; v < vertexCount; v++)
            {
                cachePosition[v] = -1;
            }

            for (var c = 0; c < cacheSize; c++)
            {
                cachePosition[cache[c]] = c;
            }

            for (var c = 0; c < cacheSize; c++)
            {
                var v = cache[c];
                vertexScore[v] = ComputeVertexScore(cachePosition[v], remainingTriangleCount[v]);
            }

            bestTriangleScore = float.MaxValue;
            bestTriangleIndex = -1;

            for (var c = cacheSize - 1; c >= 0; c--)
            {
                var v = cache[c];
                foreach (var triIdx in vertexTriangleList[v])
                {
                    if (triangleAdded[triIdx])
                    {
                        continue;
                    }

                    triangleScore[triIdx] = ComputeTriangleScore(indices, triIdx, vertexScore);

                    if (triangleScore[triIdx] > bestTriangleScore || bestTriangleIndex == -1)
                    {
                        bestTriangleScore = triangleScore[triIdx];
                        bestTriangleIndex = triIdx;
                    }
                }
            }

            if (bestTriangleIndex == -1)
            {
                bestTriangleIndex = FindBestTriangle(triangleScore, triangleAdded, triangleCount);
            }
        }

        return result;
    }

    #endregion

    #region 私有方法

    private static float ComputeVertexScore(int cachePosition, int remainingTriangles)
    {
        if (remainingTriangles == 0)
        {
            return -1.0f;
        }

        var score = 0.0f;

        if (cachePosition < 0)
        {
            score = 0.0f;
        }
        else if (cachePosition < 3)
        {
            score = LastTriScore;
        }
        else
        {
            var normalizedPosition = (float)(cachePosition - 3) / (VertexCacheSize - 3);
            score = 1.0f - normalizedPosition;
            score = MathF.Pow(score, CacheDecayPower);
        }

        var valenceBoost = MathF.Pow(remainingTriangles, -ValenceBoostPower);
        score += ValenceBoostScale * valenceBoost;

        return score;
    }

    private static float ComputeTriangleScore(int[] indices, int triangleIndex, float[] vertexScore)
    {
        var i0 = indices[triangleIndex * 3];
        var i1 = indices[triangleIndex * 3 + 1];
        var i2 = indices[triangleIndex * 3 + 2];

        return vertexScore[i0] + vertexScore[i1] + vertexScore[i2];
    }

    private static int AddVertexToCache(int[] cache, int cacheSize, int vertex)
    {
        var foundAt = -1;
        for (var i = 0; i < cacheSize; i++)
        {
            if (cache[i] == vertex)
            {
                foundAt = i;
                break;
            }
        }

        if (foundAt >= 0)
        {
            for (var i = foundAt; i < cacheSize - 1; i++)
            {
                cache[i] = cache[i + 1];
            }
            cacheSize--;
        }

        if (cacheSize >= VertexCacheSize)
        {
            for (var i = 0; i < cacheSize - 1; i++)
            {
                cache[i] = cache[i + 1];
            }
            cacheSize--;
        }

        cache[cacheSize] = vertex;
        cacheSize++;

        return cacheSize;
    }

    private static int FindBestTriangle(float[] triangleScore, bool[] triangleAdded, int triangleCount)
    {
        var best = -1;
        var bestScore = -1.0f;

        for (var i = 0; i < triangleCount; i++)
        {
            if (triangleAdded[i])
            {
                continue;
            }

            if (triangleScore[i] > bestScore || best == -1)
            {
                bestScore = triangleScore[i];
                best = i;
            }
        }

        return best;
    }

    #endregion
}
