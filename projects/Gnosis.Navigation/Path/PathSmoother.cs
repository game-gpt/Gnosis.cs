using Gnosis.Core.Math;
using Gnosis.Navigation.NavMesh;

namespace Gnosis.Navigation.Path;

/// <summary>
/// 路径平滑器，通过拉直路径点减少不必要的转弯
/// </summary>
public sealed class PathSmoother
{
    #region 公开方法

    /// <summary>
    /// 对路径点列表执行平滑处理
    /// </summary>
    public List<Vector3> Smooth(List<Vector3> waypoints, INavMesh? navMesh)
    {
        if (waypoints.Count <= 2)
        {
            return new List<Vector3>(waypoints);
        }

        if (navMesh is null || !navMesh.IsBuilt)
        {
            return new List<Vector3>(waypoints);
        }

        var result = FunnelSmooth(waypoints, navMesh);
        return result;
    }

    #endregion

    #region 私有方法

    private static List<Vector3> FunnelSmooth(List<Vector3> waypoints, INavMesh navMesh)
    {
        var smoothed = new List<Vector3> { waypoints[0] };

        var apex = waypoints[0];
        var i = 1;
        while (i < waypoints.Count - 1)
        {
            var directDist = DistanceXZ(apex, waypoints[i + 1]);

            var canSkip = true;
            var steps = Math.Max(3, (int)(directDist / 0.5f));

            for (var s = 1; s < steps; s++)
            {
                var t = (float)s / steps;
                var testPoint = Vector3.Lerp(apex, waypoints[i + 1], t);

                if (!navMesh.IsPointWalkable(testPoint))
                {
                    canSkip = false;
                    break;
                }
            }

            if (canSkip)
            {
                i++;
            }
            else
            {
                smoothed.Add(waypoints[i]);
                apex = waypoints[i];
                i++;
            }
        }

        smoothed.Add(waypoints[^1]);
        return smoothed;
    }

    private static float DistanceXZ(Vector3 a, Vector3 b)
    {
        var dx = a.X - b.X;
        var dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    #endregion
}
