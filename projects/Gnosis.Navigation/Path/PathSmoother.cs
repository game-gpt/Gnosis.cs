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
    public List<float[]> Smooth(List<float[]> waypoints, INavMesh? navMesh)
    {
        if (waypoints.Count <= 2)
        {
            return new List<float[]>(waypoints);
        }

        if (navMesh is null || !navMesh.IsBuilt)
        {
            return new List<float[]>(waypoints);
        }

        var result = FunnelSmooth(waypoints, navMesh);
        return result;
    }

    #endregion

    #region 私有方法

    private static List<float[]> FunnelSmooth(List<float[]> waypoints, INavMesh navMesh)
    {
        var smoothed = new List<float[]> { waypoints[0] };

        var apex = waypoints[0];
        var apexIdx = 0;

        var i = 1;
        while (i < waypoints.Count - 1)
        {
            var directDist = DistanceXZ(apex, waypoints[i + 1]);

            var canSkip = true;
            var steps = Math.Max(3, (int)(directDist / 0.5f));

            for (var s = 1; s < steps; s++)
            {
                var t = (float)s / steps;
                var testX = apex[0] + (waypoints[i + 1][0] - apex[0]) * t;
                var testZ = apex[2] + (waypoints[i + 1][2] - apex[2]) * t;
                var testY = apex[1] + (waypoints[i + 1][1] - apex[1]) * t;

                if (!navMesh.IsPointWalkable(new float[] { testX, testY, testZ }))
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
                apexIdx = i;
                i++;
            }
        }

        smoothed.Add(waypoints[^1]);
        return smoothed;
    }

    private static float DistanceXZ(float[] a, float[] b)
    {
        var dx = a[0] - b[0];
        var dz = a[2] - b[2];
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    #endregion
}
