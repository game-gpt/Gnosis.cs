using System.Numerics;
using Gnosis.Audio.Clip;
using Gnosis.Audio.Listener;

namespace Gnosis.Audio.Spatial;

public sealed class SpatialAudioCalculator
{
    #region 常量

    private const float SpeedOfSound = 343.0f;

    #endregion

    #region 字段

    private readonly HrtfProcessor _hrtfProcessor;

    #endregion

    #region 属性

    public float GlobalRolloffFactor { get; set; } = 1.0f;

    public float DopplerFactor { get; set; } = 1.0f;

    public bool EnableHrtf { get; set; } = true;

    public HrtfProcessor Hrtf => _hrtfProcessor;

    #endregion

    #region 构造函数

    public SpatialAudioCalculator()
    {
        _hrtfProcessor = new HrtfProcessor();
    }

    #endregion

    #region 公开方法

    public SpatialAudioResult Calculate(
        Vector3 sourcePosition,
        Vector3 sourceVelocity,
        Vector3 listenerPosition,
        Vector3 listenerForward,
        Vector3 listenerUp,
        Vector3 listenerVelocity,
        float minDistance,
        float maxDistance,
        AudioRolloffMode rolloffMode,
        float spatialBlend,
        float dopplerLevel,
        float spread)
    {
        var toSource = sourcePosition - listenerPosition;
        var distance = toSource.Length();

        var distanceRatio = minDistance > 0f ? distance / minDistance : 1f;

        var attenuation = CalculateAttenuation(distanceRatio, rolloffMode, minDistance, maxDistance);

        var dopplerPitch = CalculateDopplerPitch(
            sourcePosition, sourceVelocity,
            listenerPosition, listenerVelocity,
            dopplerLevel);

        float pan;
        float leftGain;
        float rightGain;

        if (EnableHrtf)
        {
            var hrtfResult = _hrtfProcessor.Process(sourcePosition, listenerPosition, listenerForward, listenerUp);

            leftGain = hrtfResult.LeftGain;
            rightGain = hrtfResult.RightGain;
            pan = (rightGain - leftGain) / (leftGain + rightGain + 0.0001f);
        }
        else
        {
            pan = CalculatePanning(toSource, distance, listenerForward, listenerUp);
            leftGain = 1f - MathF.Max(0f, pan);
            rightGain = 1f + MathF.Min(0f, pan);
        }

        var spatialVolume = attenuation * spatialBlend + (1f - spatialBlend);

        var occlusion = 0f;

        return new SpatialAudioResult
        {
            Volume = Math.Clamp(spatialVolume, 0f, 1f),
            Pan = Math.Clamp(pan, -1f, 1f),
            Pitch = Math.Clamp(dopplerPitch, 0.1f, 3f),
            Attenuation = attenuation,
            Distance = distance,
            Occlusion = occlusion,
            SpatialBlend = spatialBlend,
            LeftGain = leftGain,
            RightGain = rightGain
        };
    }

    public SpatialAudioResult CalculateHrtf(
        Vector3 sourcePosition,
        Vector3 sourceVelocity,
        Vector3 listenerPosition,
        Vector3 listenerForward,
        Vector3 listenerUp,
        Vector3 listenerVelocity,
        float minDistance,
        float maxDistance,
        AudioRolloffMode rolloffMode,
        float spatialBlend,
        float dopplerLevel,
        float spread)
    {
        var toSource = sourcePosition - listenerPosition;
        var distance = toSource.Length();

        var distanceRatio = minDistance > 0f ? distance / minDistance : 1f;

        var attenuation = CalculateAttenuation(distanceRatio, rolloffMode, minDistance, maxDistance);

        var dopplerPitch = CalculateDopplerPitch(
            sourcePosition, sourceVelocity,
            listenerPosition, listenerVelocity,
            dopplerLevel);

        var hrtfResult = _hrtfProcessor.Process(sourcePosition, listenerPosition, listenerForward, listenerUp);

        var leftGain = hrtfResult.LeftGain * attenuation;
        var rightGain = hrtfResult.RightGain * attenuation;

        var spatialBlendFactor = spatialBlend;
        leftGain = leftGain * spatialBlendFactor + (1f - spatialBlendFactor) * attenuation;
        rightGain = rightGain * spatialBlendFactor + (1f - spatialBlendFactor) * attenuation;

        var pan = (rightGain - leftGain) / (leftGain + rightGain + 0.0001f);

        return new SpatialAudioResult
        {
            Volume = Math.Clamp((leftGain + rightGain) * 0.5f, 0f, 1f),
            Pan = Math.Clamp(pan, -1f, 1f),
            Pitch = Math.Clamp(dopplerPitch, 0.1f, 3f),
            Attenuation = attenuation,
            Distance = distance,
            Occlusion = 0f,
            SpatialBlend = spatialBlend,
            LeftGain = Math.Clamp(leftGain, 0f, 1f),
            RightGain = Math.Clamp(rightGain, 0f, 1f)
        };
    }

    public float CalculateAttenuation(float distanceRatio, AudioRolloffMode rolloffMode, float minDistance, float maxDistance)
    {
        if (distanceRatio <= 1f)
        {
            return 1f;
        }

        if (distanceRatio >= maxDistance / Math.Max(0.001f, minDistance))
        {
            return 0f;
        }

        return rolloffMode switch
        {
            AudioRolloffMode.Logarithmic => minDistance / (minDistance + (distanceRatio - 1f) * minDistance * GlobalRolloffFactor),
            AudioRolloffMode.Linear => 1f - (distanceRatio - 1f) / (maxDistance / Math.Max(0.001f, minDistance) - 1f),
            AudioRolloffMode.Custom => minDistance / (minDistance + (distanceRatio - 1f) * minDistance * GlobalRolloffFactor),
            _ => 1f
        };
    }

    #endregion

    #region 私有方法

    private float CalculatePanning(Vector3 toSource, float distance, Vector3 listenerForward, Vector3 listenerUp)
    {
        if (distance < 0.0001f)
        {
            return 0f;
        }

        var listenerRight = Vector3.Cross(listenerForward, listenerUp);

        if (listenerRight.LengthSquared() < 0.0001f)
        {
            return 0f;
        }

        listenerRight = Vector3.Normalize(listenerRight);

        var direction = Vector3.Normalize(toSource);
        var dot = Vector3.Dot(direction, listenerRight);

        return Math.Clamp(dot, -1f, 1f);
    }

    private float CalculateDopplerPitch(
        Vector3 sourcePosition, Vector3 sourceVelocity,
        Vector3 listenerPosition, Vector3 listenerVelocity,
        float dopplerLevel)
    {
        if (dopplerLevel <= 0f)
        {
            return 1f;
        }

        var toListener = listenerPosition - sourcePosition;
        var distance = toListener.Length();

        if (distance < 0.0001f)
        {
            return 1f;
        }

        var direction = toListener / distance;

        var sourceRadialVelocity = Vector3.Dot(sourceVelocity, direction);
        var listenerRadialVelocity = Vector3.Dot(listenerVelocity, direction);

        var relativeVelocity = listenerRadialVelocity - sourceRadialVelocity;

        var dopplerShift = SpeedOfSound / (SpeedOfSound + relativeVelocity * DopplerFactor * dopplerLevel);

        return Math.Clamp(dopplerShift, 0.1f, 3f);
    }

    #endregion
}

public struct SpatialAudioResult
{
    public float Volume;
    public float Pan;
    public float Pitch;
    public float Attenuation;
    public float Distance;
    public float Occlusion;
    public float SpatialBlend;
    public float LeftGain;
    public float RightGain;
}
