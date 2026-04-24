using System.Numerics;
using Gnosis.Core.Math;

namespace Gnosis.Geometry.Cluster;

/// <summary>
/// 簇包围信息，用于视锥剔除和遮挡剔除
/// </summary>
public readonly record struct ClusterBounds
{
    /// <summary>
    /// 轴对齐包围盒
    /// </summary>
    public BoundingBox Bounds { get; init; }

    /// <summary>
    /// 包围球中心
    /// </summary>
    public Vector3 SphereCenter { get; init; }

    /// <summary>
    /// 包围球半径
    /// </summary>
    public float SphereRadius { get; init; }

    /// <summary>
    /// 屏幕空间误差阈值，用于 LOD 选择
    /// </summary>
    public float ScreenSpaceError { get; init; }

    /// <summary>
    /// 簇的最近深度值（用于 HZB 遮挡剔除）
    /// </summary>
    public float NearDepth { get; init; }

    /// <summary>
    /// 簇的最远深度值（用于 HZB 遮挡剔除）
    /// </summary>
    public float FarDepth { get; init; }

    /// <summary>
    /// 从一组顶点位置计算簇包围信息
    /// </summary>
    public static ClusterBounds FromPositions(ReadOnlySpan<Vector3> positions, float screenSpaceError = 0.0f)
    {
        if (positions.IsEmpty)
        {
            return new ClusterBounds
            {
                Bounds = BoundingBox.Empty,
                SphereCenter = Vector3.Zero,
                SphereRadius = 0.0f,
                ScreenSpaceError = screenSpaceError
            };
        }

        var bounds = BoundingBox.CreateFromPoints(positions);

        var center = bounds.Center;
        var maxRadiusSq = 0.0f;

        for (var i = 0; i < positions.Length; i++)
        {
            var diff = positions[i] - center;
            var distSq = diff.LengthSquared();
            if (distSq > maxRadiusSq)
            {
                maxRadiusSq = distSq;
            }
        }

        return new ClusterBounds
        {
            Bounds = bounds,
            SphereCenter = center,
            SphereRadius = MathF.Sqrt(maxRadiusSq),
            ScreenSpaceError = screenSpaceError
        };
    }
}
