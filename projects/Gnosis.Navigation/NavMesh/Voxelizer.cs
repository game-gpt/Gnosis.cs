namespace Gnosis.Navigation.NavMesh;

/// <summary>
/// 体素化器，将场景几何体转换为三维体素网格
/// </summary>
public class Voxelizer
{
    #region 内部类型

    /// <summary>
    /// 体素跨度，表示一列中连续的实心体素段
    /// </summary>
    private struct VoxelSpan
    {
        public int YMin;
        public int YMax;
        public int AreaId;
    }

    #endregion

    #region 字段

    private int _width;
    private int _height;
    private int _depth;
    private float _voxelSize;
    private float _inverseVoxelSize;
    private float[] _boundsMin = [];
    private float[] _boundsMax = [];
    private List<VoxelSpan>[] _columns = [];

    #endregion

    #region 属性

    /// <summary>
    /// 体素网格宽度
    /// </summary>
    public int Width => _width;

    /// <summary>
    /// 体素网格高度
    /// </summary>
    public int Height => _height;

    /// <summary>
    /// 体素网格深度
    /// </summary>
    public int Depth => _depth;

    /// <summary>
    /// 体素大小
    /// </summary>
    public float VoxelSize => _voxelSize;

    /// <summary>
    /// 边界最小点
    /// </summary>
    public float[] BoundsMin => _boundsMin;

    /// <summary>
    /// 边界最大点
    /// </summary>
    public float[] BoundsMax => _boundsMax;

    #endregion

    #region 公开方法

    /// <summary>
    /// 执行体素化，将三角形列表转换为体素网格
    /// </summary>
    public void Voxelize(float[] vertices, int[] triangles, int triangleCount, NavMeshBuildSettings settings)
    {
        _voxelSize = settings.VoxelSize;
        _inverseVoxelSize = 1.0f / _voxelSize;

        CalculateBounds(vertices, triangles, triangleCount, settings);

        _width = (int)MathF.Ceiling((_boundsMax[0] - _boundsMin[0]) * _inverseVoxelSize);
        _height = (int)MathF.Ceiling((_boundsMax[1] - _boundsMin[1]) * _inverseVoxelSize);
        _depth = (int)MathF.Ceiling((_boundsMax[2] - _boundsMin[2]) * _inverseVoxelSize);

        _width = Math.Max(1, _width);
        _height = Math.Max(1, _height);
        _depth = Math.Max(1, _depth);

        _columns = new List<VoxelSpan>[_width * _depth];
        for (var i = 0; i < _columns.Length; i++)
        {
            _columns[i] = new List<VoxelSpan>();
        }

        for (var i = 0; i < triangleCount; i++)
        {
            var v0Idx = triangles[i * 3] * 3;
            var v1Idx = triangles[i * 3 + 1] * 3;
            var v2Idx = triangles[i * 3 + 2] * 3;

            var v0 = new float[] { vertices[v0Idx], vertices[v0Idx + 1], vertices[v0Idx + 2] };
            var v1 = new float[] { vertices[v1Idx], vertices[v1Idx + 1], vertices[v1Idx + 2] };
            var v2 = new float[] { vertices[v2Idx], vertices[v2Idx + 1], vertices[v2Idx + 2] };

            RasterizeTriangle(v0, v1, v2);
        }
    }

    /// <summary>
    /// 标记可行走体素
    /// </summary>
    public void MarkWalkable(float walkableSlopeAngle, float agentHeight, float stepHeight)
    {
        var walkableClimb = (int)MathF.Ceiling(stepHeight * _inverseVoxelSize);
        var agentHeightVoxels = (int)MathF.Ceiling(agentHeight * _inverseVoxelSize);

        for (var z = 0; z < _depth; z++)
        {
            for (var x = 0; x < _width; x++)
            {
                var column = _columns[z * _width + x];

                for (var s = 0; s < column.Count; s++)
                {
                    var span = column[s];
                    var surfaceNormalY = ComputeSurfaceNormalY(x, span.YMax, z);
                    var slopeAngle = MathF.Acos(Math.Clamp(surfaceNormalY, -1.0f, 1.0f)) * (180.0f / MathF.PI);

                    if (slopeAngle <= walkableSlopeAngle)
                    {
                        span.AreaId = 0;
                    }
                    else
                    {
                        span.AreaId = -1;
                    }

                    if (span.AreaId == 0 && s > 0)
                    {
                        var prevSpan = column[s - 1];
                        var gapHeight = span.YMin - prevSpan.YMax;
                        if (gapHeight > walkableClimb)
                        {
                            span.AreaId = -1;
                        }
                    }

                    var aboveY = span.YMax + agentHeightVoxels;
                    var hasSpace = true;
                    for (var ns = s + 1; ns < column.Count; ns++)
                    {
                        if (column[ns].YMin <= aboveY && column[ns].YMax >= span.YMax)
                        {
                            hasSpace = false;
                            break;
                        }
                    }

                    if (!hasSpace)
                    {
                        span.AreaId = -1;
                    }

                    column[s] = span;
                }
            }
        }
    }

    /// <summary>
    /// 获取指定位置的体素跨度数量
    /// </summary>
    public int GetSpanCount(int x, int z)
    {
        if (x < 0 || x >= _width || z < 0 || z >= _depth)
        {
            return 0;
        }
        return _columns[z * _width + x].Count;
    }

    /// <summary>
    /// 获取指定位置的可行走体素信息
    /// </summary>
    public bool GetWalkableSpan(int x, int z, out int yMin, out int yMax)
    {
        yMin = 0;
        yMax = 0;

        if (x < 0 || x >= _width || z < 0 || z >= _depth)
        {
            return false;
        }

        var column = _columns[z * _width + x];

        foreach (var span in column)
        {
            if (span.AreaId == 0)
            {
                yMin = span.YMin;
                yMax = span.YMax;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 将体素坐标转换为世界坐标
    /// </summary>
    public float[] VoxelToWorld(int x, int y, int z)
    {
        return new float[]
        {
            _boundsMin[0] + x * _voxelSize,
            _boundsMin[1] + y * _voxelSize,
            _boundsMin[2] + z * _voxelSize
        };
    }

    /// <summary>
    /// 将世界坐标转换为体素坐标
    /// </summary>
    public void WorldToVoxel(float[] worldPos, out int x, out int y, out int z)
    {
        x = (int)MathF.Floor((worldPos[0] - _boundsMin[0]) * _inverseVoxelSize);
        y = (int)MathF.Floor((worldPos[1] - _boundsMin[1]) * _inverseVoxelSize);
        z = (int)MathF.Floor((worldPos[2] - _boundsMin[2]) * _inverseVoxelSize);
    }

    #endregion

    #region 私有方法

    private void CalculateBounds(float[] vertices, int[] triangles, int triangleCount, NavMeshBuildSettings settings)
    {
        _boundsMin = new float[] { float.MaxValue, float.MaxValue, float.MaxValue };
        _boundsMax = new float[] { float.MinValue, float.MinValue, float.MinValue };

        for (var i = 0; i < triangleCount; i++)
        {
            for (var j = 0; j < 3; j++)
            {
                var idx = triangles[i * 3 + j] * 3;
                for (var k = 0; k < 3; k++)
                {
                    _boundsMin[k] = Math.Min(_boundsMin[k], vertices[idx + k]);
                    _boundsMax[k] = Math.Max(_boundsMax[k], vertices[idx + k]);
                }
            }
        }

        var padding = settings.AgentRadius * 2;
        for (var k = 0; k < 3; k++)
        {
            _boundsMin[k] -= padding;
            _boundsMax[k] += padding;
        }
    }

    private void RasterizeTriangle(float[] v0, float[] v1, float[] v2)
    {
        var invVoxel = _inverseVoxelSize;
        var bMinX = _boundsMin[0];
        var bMinZ = _boundsMin[2];

        var minX = (int)MathF.Floor(Math.Min(Math.Min(v0[0], v1[0]), v2[0]) * invVoxel - bMinX * invVoxel);
        var maxX = (int)MathF.Ceiling(Math.Max(Math.Max(v0[0], v1[0]), v2[0]) * invVoxel - bMinX * invVoxel);
        var minZ = (int)MathF.Floor(Math.Min(Math.Min(v0[2], v1[2]), v2[2]) * invVoxel - bMinZ * invVoxel);
        var maxZ = (int)MathF.Ceiling(Math.Max(Math.Max(v0[2], v1[2]), v2[2]) * invVoxel - bMinZ * invVoxel);

        minX = Math.Clamp(minX, 0, _width - 1);
        maxX = Math.Clamp(maxX, 0, _width - 1);
        minZ = Math.Clamp(minZ, 0, _depth - 1);
        maxZ = Math.Clamp(maxZ, 0, _depth - 1);

        var edge1 = new float[] { v1[0] - v0[0], v1[1] - v0[1], v1[2] - v0[2] };
        var edge2 = new float[] { v2[0] - v0[0], v2[1] - v0[1], v2[2] - v0[2] };
        var normal = new float[]
        {
            edge1[1] * edge2[2] - edge1[2] * edge2[1],
            edge1[2] * edge2[0] - edge1[0] * edge2[2],
            edge1[0] * edge2[1] - edge1[1] * edge2[0]
        };

        var len = MathF.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
        if (len < 1e-6f)
        {
            return;
        }
        normal[0] /= len;
        normal[1] /= len;
        normal[2] /= len;

        for (var z = minZ; z <= maxZ; z++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var worldX = _boundsMin[0] + (x + 0.5f) * _voxelSize;
                var worldZ = _boundsMin[2] + (z + 0.5f) * _voxelSize;

                if (!PointInTriangleXZ(worldX, worldZ, v0, v1, v2))
                {
                    continue;
                }

                float heightY;
                if (MathF.Abs(normal[1]) > 1e-6f)
                {
                    heightY = (v0[0] * normal[0] + v0[1] * normal[1] + v0[2] * normal[2]
                             - worldX * normal[0] - worldZ * normal[2]) / normal[1];
                }
                else
                {
                    heightY = (v0[1] + v1[1] + v2[1]) / 3.0f;
                }

                var yMin = (int)MathF.Floor((heightY - _boundsMin[1]) * _inverseVoxelSize);
                var yMax = yMin + 1;

                yMin = Math.Clamp(yMin, 0, _height - 1);
                yMax = Math.Clamp(yMax, 0, _height - 1);

                AddSpan(x, z, yMin, yMax);
            }
        }
    }

    private void AddSpan(int x, int z, int yMin, int yMax)
    {
        var column = _columns[z * _width + x];

        var newSpan = new VoxelSpan
        {
            YMin = yMin,
            YMax = yMax,
            AreaId = 1
        };

        var insertIdx = 0;
        for (var i = 0; i < column.Count; i++)
        {
            if (column[i].YMin <= yMin)
            {
                insertIdx = i + 1;
            }
        }

        column.Insert(insertIdx, newSpan);

        MergeSpans(column);
    }

    private static void MergeSpans(List<VoxelSpan> spans)
    {
        var i = 0;
        while (i < spans.Count - 1)
        {
            if (spans[i + 1].YMin <= spans[i].YMax + 1)
            {
                var merged = new VoxelSpan
                {
                    YMin = spans[i].YMin,
                    YMax = Math.Max(spans[i].YMax, spans[i + 1].YMax),
                    AreaId = spans[i].AreaId
                };
                spans[i] = merged;
                spans.RemoveAt(i + 1);
            }
            else
            {
                i++;
            }
        }
    }

    private float ComputeSurfaceNormalY(int x, int y, int z)
    {
        var count = 0;
        var sumY = 0.0f;

        for (var dz = -1; dz <= 1; dz++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                var nx = x + dx;
                var nz = z + dz;
                if (nx < 0 || nx >= _width || nz < 0 || nz >= _depth)
                {
                    continue;
                }

                var column = _columns[nz * _width + nx];
                foreach (var span in column)
                {
                    if (MathF.Abs(span.YMax - y) <= 1)
                    {
                        sumY += 1.0f;
                        count++;
                    }
                }
            }
        }

        if (count == 0)
        {
            return 0.0f;
        }

        return sumY / count;
    }

    private static bool PointInTriangleXZ(float px, float pz, float[] v0, float[] v1, float[] v2)
    {
        var d1 = Sign2D(px, pz, v0[0], v0[2], v1[0], v1[2]);
        var d2 = Sign2D(px, pz, v1[0], v1[2], v2[0], v2[2]);
        var d3 = Sign2D(px, pz, v2[0], v2[2], v0[0], v0[2]);

        var hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
        var hasPos = d1 > 0 || d2 > 0 || d3 > 0;

        return !(hasNeg && hasPos);
    }

    private static float Sign2D(float px, float pz, float ax, float az, float bx, float bz)
    {
        return (px - bx) * (az - bz) - (ax - bx) * (pz - bz);
    }

    #endregion
}
