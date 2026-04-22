namespace Gnosis.Navigation.NavMesh;

/// <summary>
/// 多边形化器，将区域轮廓转换为凸导航多边形
/// </summary>
public class Polygonizer
{
    #region 属性

    /// <summary>
    /// 生成的导航多边形列表
    /// </summary>
    public List<NavMeshPolygon> Polygons { get; private set; } = new();

    #endregion

    #region 公开方法

    /// <summary>
    /// 从区域轮廓生成凸多边形
    /// </summary>
    public void Polygonize(int regionCount, RegionGenerator regionGenerator, Voxelizer voxelizer, float maxSimplificationError)
    {
        Polygons = new List<NavMeshPolygon>();

        for (var regionId = 0; regionId < regionCount; regionId++)
        {
            var contour = regionGenerator.GetRegionContour(regionId, voxelizer);
            if (contour.Count < 3)
            {
                continue;
            }

            var simplified = SimplifyContour(contour, maxSimplificationError);
            if (simplified.Count < 3)
            {
                continue;
            }

            var polygons = PartitionToConvex(simplified, regionId);
            Polygons.AddRange(polygons);
        }

        BuildAdjacency();
    }

    #endregion

    #region 私有方法

    private static List<float[]> SimplifyContour(List<float[]> contour, float maxError)
    {
        if (contour.Count < 3)
        {
            return new List<float[]>(contour);
        }

        var maxIdx = 0;
        var maxDist = 0.0f;

        for (var i = 1; i < contour.Count - 1; i++)
        {
            var dist = PointToSegmentDistance(contour[i], contour[0], contour[^1]);
            if (dist > maxDist)
            {
                maxDist = dist;
                maxIdx = i;
            }
        }

        if (maxDist > maxError)
        {
            var left = new List<float[]>(contour.GetRange(0, maxIdx + 1));
            var right = new List<float[]>(contour.GetRange(maxIdx, contour.Count - maxIdx));

            var simplifiedLeft = SimplifyContour(left, maxError);
            var simplifiedRight = SimplifyContour(right, maxError);

            var result = new List<float[]>(simplifiedLeft);
            result.RemoveAt(result.Count - 1);
            result.AddRange(simplifiedRight);
            return result;
        }

        return new List<float[]> { contour[0], contour[^1] };
    }

    private static float PointToSegmentDistance(float[] point, float[] segStart, float[] segEnd)
    {
        var dx = segEnd[0] - segStart[0];
        var dy = segEnd[2] - segStart[2];

        var lenSq = dx * dx + dy * dy;
        if (lenSq < 1e-10f)
        {
            var ddx = point[0] - segStart[0];
            var ddz = point[2] - segStart[2];
            return MathF.Sqrt(ddx * ddx + ddz * ddz);
        }

        var t = ((point[0] - segStart[0]) * dx + (point[2] - segStart[2]) * dy) / lenSq;
        t = Math.Clamp(t, 0.0f, 1.0f);

        var projX = segStart[0] + t * dx;
        var projZ = segStart[2] + t * dy;

        var px = point[0] - projX;
        var pz = point[2] - projZ;

        return MathF.Sqrt(px * px + pz * pz);
    }

    private List<NavMeshPolygon> PartitionToConvex(List<float[]> contour, int regionId)
    {
        var result = new List<NavMeshPolygon>();

        if (IsConvex(contour))
        {
            var polygon = CreatePolygon(contour, regionId, result.Count);
            result.Add(polygon);
            return result;
        }

        var remaining = new List<float[]>(contour);
        var attempts = 0;
        var maxAttempts = contour.Count;

        while (remaining.Count > 3 && attempts < maxAttempts)
        {
            var earFound = false;

            for (var i = 0; i < remaining.Count; i++)
            {
                var prev = (i - 1 + remaining.Count) % remaining.Count;
                var next = (i + 1) % remaining.Count;

                if (IsConvexAngle(remaining[prev], remaining[i], remaining[next]))
                {
                    var triangle = new List<float[]> { remaining[prev], remaining[i], remaining[next] };

                    if (IsConvex(triangle))
                    {
                        var polygon = CreatePolygon(triangle, regionId, result.Count);
                        result.Add(polygon);
                        remaining.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }
            }

            if (!earFound)
            {
                break;
            }

            attempts++;
        }

        if (remaining.Count >= 3)
        {
            var polygon = CreatePolygon(remaining, regionId, result.Count);
            result.Add(polygon);
        }

        return result;
    }

    private static bool IsConvex(List<float[]> polygon)
    {
        var sign = 0;

        for (var i = 0; i < polygon.Count; i++)
        {
            var prev = (i - 1 + polygon.Count) % polygon.Count;
            var next = (i + 1) % polygon.Count;

            var cross = (polygon[i][0] - polygon[prev][0]) * (polygon[next][2] - polygon[i][2])
                      - (polygon[i][2] - polygon[prev][2]) * (polygon[next][0] - polygon[i][0]);

            if (cross != 0)
            {
                if (sign == 0)
                {
                    sign = cross > 0 ? 1 : -1;
                }
                else if ((cross > 0 ? 1 : -1) != sign)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsConvexAngle(float[] prev, float[] current, float[] next)
    {
        var cross = (current[0] - prev[0]) * (next[2] - current[2])
                  - (current[2] - prev[2]) * (next[0] - current[0]);

        return cross > 0;
    }

    private static NavMeshPolygon CreatePolygon(List<float[]> vertices, int regionId, int localId)
    {
        var flatVertices = new float[vertices.Count * 3];
        for (var i = 0; i < vertices.Count; i++)
        {
            flatVertices[i * 3] = vertices[i][0];
            flatVertices[i * 3 + 1] = vertices[i][1];
            flatVertices[i * 3 + 2] = vertices[i][2];
        }

        return new NavMeshPolygon
        {
            Id = regionId * 1000 + localId,
            Vertices = flatVertices,
            Neighbors = Array.Empty<int>(),
            AreaId = regionId,
            AreaCost = 1.0f
        };
    }

    private void BuildAdjacency()
    {
        for (var i = 0; i < Polygons.Count; i++)
        {
            var neighbors = new List<int>();

            for (var j = 0; j < Polygons.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                if (SharesEdge(Polygons[i], Polygons[j]))
                {
                    neighbors.Add(Polygons[j].Id);
                }
            }

            Polygons[i] = new NavMeshPolygon
            {
                Id = Polygons[i].Id,
                Vertices = Polygons[i].Vertices,
                Neighbors = neighbors.ToArray(),
                AreaId = Polygons[i].AreaId,
                AreaCost = Polygons[i].AreaCost
            };
        }
    }

    private static bool SharesEdge(NavMeshPolygon a, NavMeshPolygon b)
    {
        var vertexCountA = a.Vertices.Length / 3;
        var vertexCountB = b.Vertices.Length / 3;
        var sharedVertices = 0;
        var threshold = 0.01f;

        for (var i = 0; i < vertexCountA; i++)
        {
            var ax = a.Vertices[i * 3];
            var az = a.Vertices[i * 3 + 2];

            for (var j = 0; j < vertexCountB; j++)
            {
                var bx = b.Vertices[j * 3];
                var bz = b.Vertices[j * 3 + 2];

                var dx = ax - bx;
                var dz = az - bz;

                if (dx * dx + dz * dz < threshold * threshold)
                {
                    sharedVertices++;
                    break;
                }
            }
        }

        return sharedVertices >= 2;
    }

    #endregion
}
