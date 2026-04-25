using System.Numerics;

namespace Gnosis.Audio.Spatial;

public sealed class HrtfProcessor
{
    #region 常量

    private const int AzimuthSteps = 36;
    private const int ElevationSteps = 14;
    private const int HrirLength = 64;

    #endregion

    #region 字段

    private readonly float[,,] _hrirLeft;
    private readonly float[,,] _hrirRight;
    private readonly float[] _leftBuffer = new float[HrirLength];
    private readonly float[] _rightBuffer = new float[HrirLength];
    private readonly float[] _leftHistory = new float[HrirLength];
    private readonly float[] _rightHistory = new float[HrirLength];
    private int _historyIndex;

    #endregion

    #region 属性

    public float HeadRadius { get; set; } = 0.0875f;

    public float InterpolationFactor { get; set; } = 1.0f;

    #endregion

    #region 构造函数

    public HrtfProcessor()
    {
        _hrirLeft = new float[AzimuthSteps, ElevationSteps, HrirLength];
        _hrirRight = new float[AzimuthSteps, ElevationSteps, HrirLength];
        GenerateSimplifiedHrtf();
    }

    #endregion

    #region 公开方法

    public HrtfResult Process(
        Vector3 sourcePosition,
        Vector3 listenerPosition,
        Vector3 listenerForward,
        Vector3 listenerUp)
    {
        var toSource = sourcePosition - listenerPosition;
        var distance = toSource.Length();

        if (distance < 0.0001f)
        {
            return new HrtfResult
            {
                LeftGain = 1f,
                RightGain = 1f,
                LeftDelay = 0f,
                RightDelay = 0f,
                Distance = 0f,
                Azimuth = 0f,
                Elevation = 0f
            };
        }

        var listenerRight = Vector3.Cross(listenerForward, listenerUp);
        if (listenerRight.LengthSquared() < 0.0001f)
        {
            listenerRight = Vector3.UnitX;
        }
        else
        {
            listenerRight = Vector3.Normalize(listenerRight);
        }

        var normForward = Vector3.Normalize(listenerForward);
        var normUp = Vector3.Normalize(listenerUp);

        var direction = toSource / distance;

        var azimuth = MathF.Atan2(
            Vector3.Dot(direction, listenerRight),
            Vector3.Dot(direction, normForward));

        var horizontalProj = direction - normUp * Vector3.Dot(direction, normUp);
        var horizontalLen = horizontalProj.Length();

        var elevation = MathF.Atan2(
            Vector3.Dot(direction, normUp),
            horizontalLen > 0.0001f ? horizontalLen : 1f);

        var (leftGain, rightGain) = ComputeBinauralGains(azimuth, distance);

        var itd = ComputeInterauralTimeDifference(azimuth);

        var leftDelay = MathF.Max(0f, -itd);
        var rightDelay = MathF.Max(0f, itd);

        return new HrtfResult
        {
            LeftGain = leftGain,
            RightGain = rightGain,
            LeftDelay = leftDelay,
            RightDelay = rightDelay,
            Distance = distance,
            Azimuth = azimuth,
            Elevation = elevation
        };
    }

    public void ProcessBuffer(
        ReadOnlySpan<float> input,
        Span<float> leftOutput,
        Span<float> rightOutput,
        float azimuth,
        float elevation)
    {
        var azIndex = AzimuthToIndex(azimuth);
        var elIndex = ElevationToIndex(elevation);

        var sampleCount = Math.Min(Math.Min(input.Length, leftOutput.Length), rightOutput.Length);

        for (var i = 0; i < sampleCount; i++)
        {
            _leftHistory[_historyIndex] = input[i];
            _rightHistory[_historyIndex] = input[i];

            var leftSample = 0f;
            var rightSample = 0f;

            for (var j = 0; j < HrirLength; j++)
            {
                var histIdx = (_historyIndex - j + HrirLength) % HrirLength;
                leftSample += _leftHistory[histIdx] * _hrirLeft[azIndex, elIndex, j];
                rightSample += _rightHistory[histIdx] * _hrirRight[azIndex, elIndex, j];
            }

            leftOutput[i] = leftSample;
            rightOutput[i] = rightSample;

            _historyIndex = (_historyIndex + 1) % HrirLength;
        }
    }

    public void Reset()
    {
        Array.Clear(_leftHistory);
        Array.Clear(_rightHistory);
        _historyIndex = 0;
    }

    #endregion

    #region 私有方法 - HRTF 数据生成

    private void GenerateSimplifiedHrtf()
    {
        for (var az = 0; az < AzimuthSteps; az++)
        {
            var azimuth = az * (2f * MathF.PI / AzimuthSteps);

            for (var el = 0; el < ElevationSteps; el++)
            {
                var elevation = (el - 7) * (MathF.PI / 14f);

                GenerateHrirPair(azimuth, elevation, out var left, out var right);

                for (var t = 0; t < HrirLength; t++)
                {
                    _hrirLeft[az, el, t] = left[t];
                    _hrirRight[az, el, t] = right[t];
                }
            }
        }
    }

    private void GenerateHrirPair(float azimuth, float elevation, out float[] left, out float[] right)
    {
        left = new float[HrirLength];
        right = new float[HrirLength];

        var sinAz = MathF.Sin(azimuth);
        var cosAz = MathF.Cos(azimuth);

        var ipsilateralGain = 1.0f;
        var contralateralGain = 0.5f + 0.3f * MathF.Abs(sinAz);

        var leftGain = sinAz >= 0f ? ipsilateralGain : contralateralGain;
        var rightGain = sinAz >= 0f ? contralateralGain : ipsilateralGain;

        var headShadow = 1f - 0.3f * MathF.Max(0f, -sinAz);
        leftGain *= headShadow;

        headShadow = 1f - 0.3f * MathF.Max(0f, sinAz);
        rightGain *= headShadow;

        var elevationFactor = MathF.Cos(elevation) * 0.8f + 0.2f;
        leftGain *= elevationFactor;
        rightGain *= elevationFactor;

        var leftDelay = sinAz >= 0f ? 0f : MathF.Abs(sinAz) * 0.4f;
        var rightDelay = sinAz >= 0f ? MathF.Abs(sinAz) * 0.4f : 0f;

        GenerateHrirPulse(left, leftGain, leftDelay);
        GenerateHrirPulse(right, rightGain, rightDelay);
    }

    private static void GenerateHrirPulse(float[] hrir, float gain, float delayMs)
    {
        var delaySamples = (int)(delayMs * 0.06f);
        delaySamples = Math.Clamp(delaySamples, 0, HrirLength - 1);

        for (var i = 0; i < HrirLength; i++)
        {
            if (i < delaySamples)
            {
                hrir[i] = 0f;
                continue;
            }

            var t = (i - delaySamples) / (float)HrirLength;

            var direct = MathF.Exp(-t * 8f) * gain;
            var reflection = MathF.Exp(-t * 12f) * gain * 0.3f * MathF.Sin(t * MathF.PI * 4f);
            var pinna = MathF.Exp(-t * 20f) * gain * 0.15f * MathF.Sin(t * MathF.PI * 8f);

            hrir[i] = direct + reflection + pinna;
        }

        NormalizeHrir(hrir);
    }

    private static void NormalizeHrir(float[] hrir)
    {
        var sum = 0f;
        for (var i = 0; i < hrir.Length; i++)
        {
            sum += hrir[i] * hrir[i];
        }

        if (sum < 0.0001f)
        {
            return;
        }

        var norm = 1f / MathF.Sqrt(sum);
        for (var i = 0; i < hrir.Length; i++)
        {
            hrir[i] *= norm;
        }
    }

    #endregion

    #region 私有方法 - 双耳增益计算

    private (float leftGain, float rightGain) ComputeBinauralGains(float azimuth, float distance)
    {
        var sinAz = MathF.Sin(azimuth);
        var cosAz = MathF.Cos(azimuth);

        var ipsiGain = 1.0f;
        var contraGain = 0.5f + 0.3f * MathF.Abs(sinAz);

        float leftGain, rightGain;

        if (sinAz >= 0f)
        {
            leftGain = ipsiGain;
            rightGain = contraGain;
        }
        else
        {
            leftGain = contraGain;
            rightGain = ipsiGain;
        }

        var headShadowLeft = 1f - 0.3f * MathF.Max(0f, -sinAz);
        var headShadowRight = 1f - 0.3f * MathF.Max(0f, sinAz);
        leftGain *= headShadowLeft;
        rightGain *= headShadowRight;

        leftGain *= 0.5f * (1f + cosAz);
        rightGain *= 0.5f * (1f - cosAz);

        leftGain = MathF.Sqrt(leftGain);
        rightGain = MathF.Sqrt(rightGain);

        return (leftGain, rightGain);
    }

    #endregion

    #region 私有方法 - ITD 计算

    private float ComputeInterauralTimeDifference(float azimuth)
    {
        var sinAz = MathF.Sin(azimuth);

        var woodworthItd = HeadRadius * (sinAz + MathF.Asin(sinAz)) / 343f;

        return woodworthItd * 1000f;
    }

    #endregion

    #region 私有方法 - 索引转换

    private static int AzimuthToIndex(float azimuth)
    {
        var normalized = ((azimuth % (2f * MathF.PI)) + 2f * MathF.PI) % (2f * MathF.PI);
        var index = (int)(normalized / (2f * MathF.PI) * AzimuthSteps);
        return Math.Clamp(index, 0, AzimuthSteps - 1);
    }

    private static int ElevationToIndex(float elevation)
    {
        var normalized = (elevation + MathF.PI / 2f) / MathF.PI;
        var index = (int)(normalized * ElevationSteps);
        return Math.Clamp(index, 0, ElevationSteps - 1);
    }

    #endregion
}

public struct HrtfResult
{
    public float LeftGain;
    public float RightGain;
    public float LeftDelay;
    public float RightDelay;
    public float Distance;
    public float Azimuth;
    public float Elevation;
}
