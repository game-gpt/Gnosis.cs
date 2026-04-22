using Gnosis.Core.Math;

namespace Gnosis.Graphic.Pipeline;

public sealed class LightCluster
{
    #region 常量

    public const uint ClusterSizeX = 16;
    public const uint ClusterSizeY = 16;
    public const uint ClusterSizeZ = 24;
    public const uint MaxLightsPerCluster = 256;

    #endregion

    #region 字段

    private readonly float _near;
    private readonly float _far;
    private uint _gridDimX;
    private uint _gridDimY;
    private uint _gridDimZ;
    private ClusterBounds[] _clusters;
    private uint[] _lightIndexLists;
    private uint[] _lightGrid;

    #endregion

    #region 属性

    public uint GridDimX => _gridDimX;
    public uint GridDimY => _gridDimY;
    public uint GridDimZ => _gridDimZ;
    public uint TotalClusterCount => _gridDimX * _gridDimY * _gridDimZ;
    public ClusterBounds[] Clusters => _clusters;
    public uint[] LightIndexLists => _lightIndexLists;
    public uint[] LightGrid => _lightGrid;

    #endregion

    #region 构造函数

    public LightCluster(float near, float far)
    {
        _near = near;
        _far = far;
        _gridDimX = 0;
        _gridDimY = 0;
        _gridDimZ = ClusterSizeZ;
        _clusters = [];
        _lightIndexLists = [];
        _lightGrid = [];
    }

    #endregion

    #region 公开方法

    public void BuildClusterGrid(uint screenWidth, uint screenHeight, float fov)
    {
        _gridDimX = (screenWidth + ClusterSizeX - 1) / ClusterSizeX;
        _gridDimY = (screenHeight + ClusterSizeY - 1) / ClusterSizeY;
        _gridDimZ = ClusterSizeZ;

        var totalClusters = _gridDimX * _gridDimY * _gridDimZ;
        _clusters = new ClusterBounds[totalClusters];

        var halfFovTan = MathF.Tan(fov * 0.5f);
        var aspectRatio = (float)screenWidth / screenHeight;

        for (uint z = 0; z < _gridDimZ; z++)
        {
            for (uint y = 0; y < _gridDimY; y++)
            {
                for (uint x = 0; x < _gridDimX; x++)
                {
                    var clusterIndex = z * _gridDimX * _gridDimY + y * _gridDimX + x;
                    _clusters[clusterIndex] = ComputeClusterBounds(
                        x, y, z,
                        screenWidth, screenHeight,
                        halfFovTan, aspectRatio);
                }
            }
        }
    }

    public void AssignLightsToClusters(ReadOnlySpan<ClusterLightInfo> lights, float[] viewMatrix)
    {
        var totalClusters = TotalClusterCount;
        _lightGrid = new uint[totalClusters * 2];

        var lightIndexList = new List<uint>();
        var clusterLightCounts = new uint[totalClusters];

        for (int lightIdx = 0; lightIdx < lights.Length; lightIdx++)
        {
            var light = lights[lightIdx];
            var viewPosition = TransformPoint(viewMatrix, light.Position);

            for (uint clusterIdx = 0; clusterIdx < totalClusters; clusterIdx++)
            {
                if (SphereIntersectsAABB(viewPosition, light.Range, _clusters[clusterIdx]))
                {
                    if (clusterLightCounts[clusterIdx] < MaxLightsPerCluster)
                    {
                        clusterLightCounts[clusterIdx]++;
                    }
                }
            }
        }

        var offsets = new uint[totalClusters];
        uint currentOffset = 0;
        for (uint i = 0; i < totalClusters; i++)
        {
            offsets[i] = currentOffset;
            _lightGrid[i * 2] = currentOffset;
            _lightGrid[i * 2 + 1] = clusterLightCounts[i];
            currentOffset += clusterLightCounts[i];
        }

        _lightIndexLists = new uint[currentOffset];

        for (int lightIdx = 0; lightIdx < lights.Length; lightIdx++)
        {
            var light = lights[lightIdx];
            var viewPosition = TransformPoint(viewMatrix, light.Position);

            for (uint clusterIdx = 0; clusterIdx < totalClusters; clusterIdx++)
            {
                if (SphereIntersectsAABB(viewPosition, light.Range, _clusters[clusterIdx]))
                {
                    var count = clusterLightCounts[clusterIdx];
                    if (count > 0)
                    {
                        var listOffset = offsets[clusterIdx];
                        var writeIdx = listOffset + (count - 1);
                        if (writeIdx < _lightIndexLists.Length)
                        {
                            _lightIndexLists[writeIdx] = (uint)lightIdx;
                        }

                        clusterLightCounts[clusterIdx]--;
                    }
                }
            }
        }
    }

    #endregion

    #region 私有方法

    private ClusterBounds ComputeClusterBounds(
        uint x, uint y, uint z,
        uint screenWidth, uint screenHeight,
        float halfFovTan, float aspectRatio)
    {
        var tileScaleX = (float)screenWidth / ClusterSizeX;
        var tileScaleY = (float)screenHeight / ClusterSizeY;

        var minX = ((float)x / tileScaleX * 2.0f - 1.0f) * halfFovTan * aspectRatio;
        var maxX = (((float)x + 1) / tileScaleX * 2.0f - 1.0f) * halfFovTan * aspectRatio;
        var minY = ((1.0f - (float)(y + 1) / tileScaleY) * 2.0f - 1.0f) * halfFovTan;
        var maxY = ((1.0f - (float)y / tileScaleY) * 2.0f - 1.0f) * halfFovTan;

        var nearZ = DepthSlice(z);
        var farZ = DepthSlice(z + 1);

        var minPoint = new Vector3(minX, minY, -nearZ);
        var maxPoint = new Vector3(maxX, maxY, -farZ);

        return new ClusterBounds
        {
            MinPoint = minPoint,
            MaxPoint = maxPoint
        };
    }

    private float DepthSlice(uint z)
    {
        return _near * MathF.Pow(_far / _near, (float)z / _gridDimZ);
    }

    private static bool SphereIntersectsAABB(in Vector3 center, float radius, in ClusterBounds bounds)
    {
        var closest = new Vector3(
            MathF.Max(bounds.MinPoint.X, MathF.Min(center.X, bounds.MaxPoint.X)),
            MathF.Max(bounds.MinPoint.Y, MathF.Min(center.Y, bounds.MaxPoint.Y)),
            MathF.Max(bounds.MinPoint.Z, MathF.Min(center.Z, bounds.MaxPoint.Z)));

        var distance = (closest - center).Length();
        return distance <= radius;
    }

    private static Vector3 TransformPoint(float[] matrix, in Vector3 point)
    {
        return new Vector3(
            matrix[0] * point.X + matrix[4] * point.Y + matrix[8] * point.Z + matrix[12],
            matrix[1] * point.X + matrix[5] * point.Y + matrix[9] * point.Z + matrix[13],
            matrix[2] * point.X + matrix[6] * point.Y + matrix[10] * point.Z + matrix[14]
        );
    }

    #endregion
}

public struct ClusterBounds
{
    public Vector3 MinPoint;
    public Vector3 MaxPoint;
}

public struct ClusterLightInfo
{
    public Vector3 Position;
    public float Range;
    public Vector3 Color;
    public float Intensity;
}
