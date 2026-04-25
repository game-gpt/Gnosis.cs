using System.Numerics;

namespace Gnosis.Audio.Spatial;

public interface IAudioOcclusionCalculator
{
    float CalculateOcclusion(Vector3 sourcePosition, Vector3 listenerPosition);
}

public interface IAudioRaycastProvider
{
    bool Raycast(Vector3 origin, Vector3 direction, float maxDistance);
}

public sealed class RaycastOcclusionCalculator : IAudioOcclusionCalculator
{
    #region 字段

    private IAudioRaycastProvider? _raycastProvider;

    #endregion

    #region 属性

    public float OcclusionPerObstacle { get; set; } = 0.3f;

    public float MaxOcclusion { get; set; } = 0.8f;

    public int RayCount { get; set; } = 3;

    #endregion

    #region 公开方法

    public void SetRaycastProvider(IAudioRaycastProvider provider)
    {
        _raycastProvider = provider;
    }

    public float CalculateOcclusion(Vector3 sourcePosition, Vector3 listenerPosition)
    {
        if (_raycastProvider == null)
        {
            return 0f;
        }

        var toListener = listenerPosition - sourcePosition;
        var distance = toListener.Length();

        if (distance < 0.0001f)
        {
            return 0f;
        }

        var direction = toListener / distance;
        var hitCount = 0;

        if (_raycastProvider.Raycast(sourcePosition, direction, distance))
        {
            hitCount++;
        }

        if (RayCount >= 3)
        {
            var up = Vector3.UnitY;
            var right = Vector3.Cross(direction, up);

            if (right.LengthSquared() < 0.0001f)
            {
                right = Vector3.UnitX;
            }

            right = Vector3.Normalize(right);
            var offset = right * 0.1f;

            if (_raycastProvider.Raycast(sourcePosition + offset, direction, distance))
            {
                hitCount++;
            }

            if (_raycastProvider.Raycast(sourcePosition - offset, direction, distance))
            {
                hitCount++;
            }
        }

        if (hitCount == 0)
        {
            return 0f;
        }

        var occlusion = hitCount / (float)RayCount * OcclusionPerObstacle;
        return Math.Clamp(occlusion, 0f, MaxOcclusion);
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

public sealed class CombinedOcclusionCalculator : IAudioOcclusionCalculator
{
    #region 字段

    private readonly RaycastOcclusionCalculator _raycastCalculator;
    private readonly DistanceOcclusionCalculator _distanceCalculator;

    #endregion

    #region 属性

    public float RaycastWeight { get; set; } = 0.7f;

    public float DistanceWeight { get; set; } = 0.3f;

    #endregion

    #region 构造函数

    public CombinedOcclusionCalculator()
    {
        _raycastCalculator = new RaycastOcclusionCalculator();
        _distanceCalculator = new DistanceOcclusionCalculator();
    }

    #endregion

    #region 公开方法

    public void SetRaycastProvider(IAudioRaycastProvider provider)
    {
        _raycastCalculator.SetRaycastProvider(provider);
    }

    #endregion

    #region IAudioOcclusionCalculator 实现

    public float CalculateOcclusion(Vector3 sourcePosition, Vector3 listenerPosition)
    {
        var raycastOcclusion = _raycastCalculator.CalculateOcclusion(sourcePosition, listenerPosition);
        var distanceOcclusion = _distanceCalculator.CalculateOcclusion(sourcePosition, listenerPosition);

        return Math.Clamp(raycastOcclusion * RaycastWeight + distanceOcclusion * DistanceWeight, 0f, 1f);
    }

    #endregion
}
