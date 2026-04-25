using System.Numerics;

namespace Gnosis.Audio.Spatial;

public interface IAudioOcclusionCalculator
{
    float CalculateOcclusion(Vector3 sourcePosition, Vector3 listenerPosition);
}

public sealed class RaycastOcclusionCalculator : IAudioOcclusionCalculator
{
    #region 属性

    public float OcclusionPerObstacle { get; set; } = 0.3f;

    public float MaxOcclusion { get; set; } = 0.8f;

    #endregion

    #region IAudioOcclusionCalculator 实现

    public float CalculateOcclusion(Vector3 sourcePosition, Vector3 listenerPosition)
    {
        return 0f;
    }

    #endregion
}

public sealed class DistanceOcclusionCalculator : IAudioOcclusionCalculator
{
    #region 属性

    public float OcclusionStartDistance { get; set; } = 10f;

    public float OcclusionMaxDistance { get; set; } = 50f;

    public float MaxOcclusion { get; set; } = 0.6f;

    #endregion

    #region IAudioOcclusionCalculator 实现

    public float CalculateOcclusion(Vector3 sourcePosition, Vector3 listenerPosition)
    {
        var distance = Vector3.Distance(sourcePosition, listenerPosition);

        if (distance <= OcclusionStartDistance)
        {
            return 0f;
        }

        if (distance >= OcclusionMaxDistance)
        {
            return MaxOcclusion;
        }

        var t = (distance - OcclusionStartDistance) / (OcclusionMaxDistance - OcclusionStartDistance);
        return MaxOcclusion * t;
    }

    #endregion
}
