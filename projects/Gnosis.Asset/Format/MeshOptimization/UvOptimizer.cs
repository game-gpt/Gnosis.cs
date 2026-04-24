namespace Gnosis.Asset.Format.MeshOptimization;

/// <summary>
/// UV 优化器，提供 UV 图集打包功能
/// 将网格的 UV 坐标重归一化并优化布局，提高纹理利用率
/// </summary>
public static class UvOptimizer
{
    #region 公开方法

    /// <summary>
    /// 将 UV 图表打包到 [0,1]×[0,1] 空间，优化纹理利用率
    /// </summary>
    public static List<MeshVertex> PackUvCharts(IReadOnlyList<MeshVertex> vertices, IReadOnlyList<int> indices)
    {
        if (vertices.Count == 0 || indices.Count == 0)
        {
            return vertices.ToList();
        }

        bool hasUv = vertices.Any(v => v.Uv is { Length: >= 2 });
        if (!hasUv)
        {
            return vertices.ToList();
        }

        var uvBounds = ComputeUvBounds(vertices);
        if (uvBounds.Width <= 0 || uvBounds.Height <= 0)
        {
            return vertices.ToList();
        }

        var charts = ExtractUvCharts(vertices, indices);
        var packedCharts = PackCharts(charts, uvBounds);

        return ApplyPackedUvs(vertices, packedCharts);
    }

    #endregion

    #region UV 边界计算

    private record UvBounds(float MinU, float MinV, float MaxU, float MaxV)
    {
        public float Width => MaxU - MinU;
        public float Height => MaxV - MinV;
    }

    private static UvBounds ComputeUvBounds(IReadOnlyList<MeshVertex> vertices)
    {
        float minU = float.MaxValue, minV = float.MaxValue;
        float maxU = float.MinValue, maxV = float.MinValue;

        foreach (var vertex in vertices)
        {
            if (vertex.Uv is { Length: >= 2 })
            {
                float u = vertex.Uv[0];
                float v = vertex.Uv[1];

                if (u < minU) minU = u;
                if (v < minV) minV = v;
                if (u > maxU) maxU = u;
                if (v > maxV) maxV = v;
            }
        }

        if (minU == float.MaxValue)
        {
            return new UvBounds(0, 0, 1, 1);
        }

        return new UvBounds(minU, minV, maxU, maxV);
    }

    #endregion

    #region UV 图表提取

    private class UvChart
    {
        public List<int> VertexIndices { get; } = [];
        public float MinU { get; set; }
        public float MinV { get; set; }
        public float MaxU { get; set; }
        public float MaxV { get; set; }
        public float PackedMinU { get; set; }
        public float PackedMinV { get; set; }
        public float PackedMaxU { get; set; }
        public float PackedMaxV { get; set; }
    }

    private static List<UvChart> ExtractUvCharts(IReadOnlyList<MeshVertex> vertices, IReadOnlyList<int> indices)
    {
        var vertexChartMap = new int[vertices.Count];
        Array.Fill(vertexChartMap, -1);
        var charts = new List<UvChart>();

        int triangleCount = indices.Count / 3;

        for (int tri = 0; tri < triangleCount; tri++)
        {
            int i0 = indices[tri * 3];
            int i1 = indices[tri * 3 + 1];
            int i2 = indices[tri * 3 + 2];

            int chart0 = vertexChartMap[i0];
            int chart1 = vertexChartMap[i1];
            int chart2 = vertexChartMap[i2];

            if (chart0 < 0 && chart1 < 0 && chart2 < 0)
            {
                int newChartIdx = charts.Count;
                var chart = new UvChart();
                chart.VertexIndices.Add(i0);
                chart.VertexIndices.Add(i1);
                chart.VertexIndices.Add(i2);
                vertexChartMap[i0] = newChartIdx;
                vertexChartMap[i1] = newChartIdx;
                vertexChartMap[i2] = newChartIdx;
                charts.Add(chart);
            }
            else
            {
                int targetChart = chart0 >= 0 ? chart0 : (chart1 >= 0 ? chart1 : chart2);

                var target = charts[targetChart];

                foreach (int idx in new[] { i0, i1, i2 })
                {
                    if (vertexChartMap[idx] < 0)
                    {
                        vertexChartMap[idx] = targetChart;
                        target.VertexIndices.Add(idx);
                    }
                    else if (vertexChartMap[idx] != targetChart)
                    {
                        int sourceChart = vertexChartMap[idx];
                        if (sourceChart != targetChart)
                        {
                            var source = charts[sourceChart];
                            foreach (int srcIdx in source.VertexIndices)
                            {
                                vertexChartMap[srcIdx] = targetChart;
                                target.VertexIndices.Add(srcIdx);
                            }

                            source.VertexIndices.Clear();
                        }
                    }
                }
            }
        }

        var validCharts = new List<UvChart>();
        foreach (var chart in charts)
        {
            if (chart.VertexIndices.Count == 0) continue;

            float minU = float.MaxValue, minV = float.MaxValue;
            float maxU = float.MinValue, maxV = float.MinValue;

            foreach (int idx in chart.VertexIndices)
            {
                var uv = vertices[idx].Uv;
                if (uv is { Length: >= 2 })
                {
                    if (uv[0] < minU) minU = uv[0];
                    if (uv[1] < minV) minV = uv[1];
                    if (uv[0] > maxU) maxU = uv[0];
                    if (uv[1] > maxV) maxV = uv[1];
                }
            }

            if (minU == float.MaxValue) continue;

            chart.MinU = minU;
            chart.MinV = minV;
            chart.MaxU = maxU;
            chart.MaxV = maxV;

            validCharts.Add(chart);
        }

        return validCharts;
    }

    #endregion

    #region 图表打包

    private static List<UvChart> PackCharts(List<UvChart> charts, UvBounds globalBounds)
    {
        if (charts.Count == 0) return charts;

        float globalWidth = globalBounds.Width > 0 ? globalBounds.Width : 1f;
        float globalHeight = globalBounds.Height > 0 ? globalBounds.Height : 1f;

        foreach (var chart in charts)
        {
            chart.PackedMinU = (chart.MinU - globalBounds.MinU) / globalWidth;
            chart.PackedMinV = (chart.MinV - globalBounds.MinV) / globalHeight;
            chart.PackedMaxU = (chart.MaxU - globalBounds.MinU) / globalWidth;
            chart.PackedMaxV = (chart.MaxV - globalBounds.MinV) / globalHeight;
        }

        var sortedCharts = charts.OrderByDescending(c => (c.PackedMaxU - c.PackedMinU) * (c.PackedMaxV - c.PackedMinV)).ToList();

        float cursorX = 0f;
        float cursorY = 0f;
        float rowHeight = 0f;
        float maxWidth = 1f;

        foreach (var chart in sortedCharts)
        {
            float chartWidth = chart.PackedMaxU - chart.PackedMinU;
            float chartHeight = chart.PackedMaxV - chart.PackedMinV;

            if (chartWidth <= 0 || chartHeight <= 0) continue;

            if (cursorX + chartWidth > maxWidth)
            {
                cursorX = 0f;
                cursorY += rowHeight;
                rowHeight = 0f;
            }

            float offsetX = cursorX - chart.PackedMinU;
            float offsetY = cursorY - chart.PackedMinV;

            chart.PackedMinU += offsetX;
            chart.PackedMaxU += offsetX;
            chart.PackedMinV += offsetY;
            chart.PackedMaxV += offsetY;

            cursorX += chartWidth;
            rowHeight = Math.Max(rowHeight, chartHeight);
        }

        float usedHeight = cursorY + rowHeight;
        if (usedHeight > 1f && usedHeight > 0)
        {
            float scaleY = 1f / usedHeight;
            foreach (var chart in sortedCharts)
            {
                chart.PackedMinV *= scaleY;
                chart.PackedMaxV *= scaleY;
                chart.PackedMinU *= scaleY;
                chart.PackedMaxU *= scaleY;
            }
        }

        return sortedCharts;
    }

    #endregion

    #region 应用打包结果

    private static List<MeshVertex> ApplyPackedUvs(IReadOnlyList<MeshVertex> vertices, List<UvChart> charts)
    {
        var result = new List<MeshVertex>(vertices.Count);

        var vertexRemap = new Dictionary<int, (float newU, float newV)>();

        foreach (var chart in charts)
        {
            float oldMinU = chart.MinU;
            float oldMinV = chart.MinV;
            float oldMaxU = chart.MaxU;
            float oldMaxV = chart.MaxV;
            float oldRangeU = oldMaxU - oldMinU;
            float oldRangeV = oldMaxV - oldMinV;

            float newMinU = chart.PackedMinU;
            float newMinV = chart.PackedMinV;
            float newMaxU = chart.PackedMaxU;
            float newMaxV = chart.PackedMaxV;
            float newRangeU = newMaxU - newMinU;
            float newRangeV = newMaxV - newMinV;

            foreach (int idx in chart.VertexIndices)
            {
                var uv = vertices[idx].Uv;
                if (uv is not { Length: >= 2 }) continue;

                float oldU = uv[0];
                float oldV = uv[1];

                float tU = oldRangeU > 0 ? (oldU - oldMinU) / oldRangeU : 0f;
                float tV = oldRangeV > 0 ? (oldV - oldMinV) / oldRangeV : 0f;

                float newU = newMinU + tU * newRangeU;
                float newV = newMinV + tV * newRangeV;

                vertexRemap[idx] = (newU, newV);
            }
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];

            if (vertexRemap.TryGetValue(i, out var remap))
            {
                var newUv = new float[v.Uv?.Length ?? 2];
                if (v.Uv is { Length: >= 2 })
                {
                    Array.Copy(v.Uv, newUv, v.Uv.Length);
                }

                newUv[0] = remap.newU;
                newUv[1] = remap.newV;

                result.Add(v with { Uv = newUv });
            }
            else
            {
                result.Add(v);
            }
        }

        return result;
    }

    #endregion
}
