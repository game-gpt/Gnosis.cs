using System.Numerics;

namespace Gnosis.Graphic.Sky;

public sealed class AtmosphericScattering : IAtmosphericScattering
{
    #region 字段

    private Vector3 _sunDirection;
    private Vector3 _sunIntensity;
    private float _rayleighCoefficient;
    private float _mieCoefficient;
    private float _mieDirectionalG;
    private float _planetRadius;
    private float _atmosphereRadius;
    private int _numSamples;
    private int _numLightSamples;
    private float _heightScale;

    #endregion

    #region 属性

    public Vector3 SunDirection
    {
        get => _sunDirection;
        set => _sunDirection = Vector3.Normalize(value);
    }

    public Vector3 SunIntensity
    {
        get => _sunIntensity;
        set => _sunIntensity = value;
    }

    public float RayleighCoefficient
    {
        get => _rayleighCoefficient;
        set => _rayleighCoefficient = Math.Max(0.0f, value);
    }

    public float MieCoefficient
    {
        get => _mieCoefficient;
        set => _mieCoefficient = Math.Max(0.0f, value);
    }

    public float MieDirectionalG
    {
        get => _mieDirectionalG;
        set => _mieDirectionalG = Math.Clamp(value, 0.0f, 1.0f);
    }

    public float PlanetRadius
    {
        get => _planetRadius;
        set => _planetRadius = Math.Max(0.001f, value);
    }

    public float AtmosphereRadius
    {
        get => _atmosphereRadius;
        set => _atmosphereRadius = Math.Max(_planetRadius + 0.001f, value);
    }

    public Vector3 RayleighScattering => new(_rayleighCoefficient, _rayleighCoefficient, _rayleighCoefficient * 0.75f);

    public Vector3 MieScattering => new(_mieCoefficient, _mieCoefficient, _mieCoefficient);

    public int NumSamples
    {
        get => _numSamples;
        set => _numSamples = Math.Max(1, value);
    }

    public int NumLightSamples
    {
        get => _numLightSamples;
        set => _numLightSamples = Math.Max(1, value);
    }

    public float HeightScale
    {
        get => _heightScale;
        set => _heightScale = Math.Max(0.001f, value);
    }

    #endregion

    #region 构造函数

    public AtmosphericScattering()
    {
        _sunDirection = Vector3.Normalize(new Vector3(0.0f, 1.0f, 0.0f));
        _sunIntensity = new Vector3(10.0f, 10.0f, 8.0f);
        _rayleighCoefficient = 5.5f;
        _mieCoefficient = 0.05f;
        _mieDirectionalG = 0.8f;
        _planetRadius = 6360.0f;
        _atmosphereRadius = 6420.0f;
        _numSamples = 16;
        _numLightSamples = 8;
        _heightScale = 0.25f;
    }

    #endregion

    #region 公开方法

    public AtmosphereData BuildAtmosphereData()
    {
        return new AtmosphereData
        {
            SunDirection = _sunDirection,
            SunIntensity = _sunIntensity,
            RayleighScattering = RayleighScattering,
            MieScattering = MieScattering,
            MieDirectionalG = _mieDirectionalG,
            PlanetRadius = _planetRadius,
            AtmosphereRadius = _atmosphereRadius,
            HeightScale = _heightScale,
            NumSamples = _numSamples,
            NumLightSamples = _numLightSamples
        };
    }

    public Vector3 ComputeSkyColor(Vector3 viewDirection)
    {
        var origin = new Vector3(0.0f, _planetRadius, 0.0f);
        return ComputeScattering(origin, viewDirection);
    }

    #endregion

    #region 私有方法

    private Vector3 ComputeScattering(Vector3 origin, Vector3 direction)
    {
        if (!IntersectAtmosphere(origin, direction, out var tMin, out var tMax))
        {
            return Vector3.Zero;
        }

        var segmentLength = (tMax - tMin) / _numSamples;
        var currentPos = origin + direction * (tMin + segmentLength * 0.5f);

        var rayleighAccum = Vector3.Zero;
        var mieAccum = Vector3.Zero;
        var opticalDepthRayleigh = Vector3.Zero;
        var opticalDepthMie = 0.0f;

        for (var i = 0; i < _numSamples; i++)
        {
            var height = currentPos.Length() - _planetRadius;
            if (height < 0.0f)
            {
                currentPos += direction * segmentLength;
                continue;
            }

            var rayleighDensity = MathF.Exp(-height / _heightScale) * segmentLength;
            var mieDensity = MathF.Exp(-height / (_heightScale * 0.2f)) * segmentLength;

            opticalDepthRayleigh += RayleighScattering * rayleighDensity;
            opticalDepthMie += _mieCoefficient * mieDensity;

            if (!IntersectAtmosphere(currentPos, _sunDirection, out _, out var lightTMax))
            {
                currentPos += direction * segmentLength;
                continue;
            }

            var lightSegmentLength = lightTMax / _numLightSamples;
            var lightPos = currentPos + _sunDirection * (lightSegmentLength * 0.5f);

            var lightOpticalDepthRayleigh = Vector3.Zero;
            var lightOpticalDepthMie = 0.0f;

            for (var j = 0; j < _numLightSamples; j++)
            {
                var lightHeight = lightPos.Length() - _planetRadius;
                if (lightHeight < 0.0f)
                {
                    lightPos += _sunDirection * lightSegmentLength;
                    continue;
                }

                lightOpticalDepthRayleigh += RayleighScattering * MathF.Exp(-lightHeight / _heightScale) * lightSegmentLength;
                lightOpticalDepthMie += _mieCoefficient * MathF.Exp(-lightHeight / (_heightScale * 0.2f)) * lightSegmentLength;

                lightPos += _sunDirection * lightSegmentLength;
            }

            var tau = opticalDepthRayleigh + lightOpticalDepthRayleigh + new Vector3(opticalDepthMie + lightOpticalDepthMie);
            var attenuation = new Vector3(MathF.Exp(-tau.X), MathF.Exp(-tau.Y), MathF.Exp(-tau.Z));

            rayleighAccum += attenuation * RayleighScattering * rayleighDensity;
            mieAccum += attenuation * _mieCoefficient * mieDensity;

            currentPos += direction * segmentLength;
        }

        var cosTheta = Vector3.Dot(direction, _sunDirection);
        var rayleighPhase = 3.0f / (16.0f * MathF.PI) * (1.0f + cosTheta * cosTheta);
        var miePhase = HenyeyGreensteinPhase(cosTheta);

        return _sunIntensity * (rayleighAccum * rayleighPhase + mieAccum * miePhase);
    }

    private float HenyeyGreensteinPhase(float cosTheta)
    {
        var g2 = _mieDirectionalG * _mieDirectionalG;
        var numerator = (1.0f - g2) / (4.0f * MathF.PI);
        var denominator = MathF.Pow(1.0f + g2 - 2.0f * _mieDirectionalG * cosTheta, 1.5f);
        return numerator / denominator;
    }

    private bool IntersectAtmosphere(Vector3 origin, Vector3 direction, out float tMin, out float tMax)
    {
        tMin = 0.0f;
        tMax = 0.0f;

        var a = Vector3.Dot(direction, direction);
        var b = 2.0f * Vector3.Dot(origin, direction);
        var c = Vector3.Dot(origin, origin) - _atmosphereRadius * _atmosphereRadius;

        var discriminant = b * b - 4.0f * a * c;
        if (discriminant < 0.0f)
        {
            return false;
        }

        var sqrtDiscriminant = MathF.Sqrt(discriminant);
        var t0 = (-b - sqrtDiscriminant) / (2.0f * a);
        var t1 = (-b + sqrtDiscriminant) / (2.0f * a);

        if (t0 > t1)
        {
            (t0, t1) = (t1, t0);
        }

        tMin = Math.Max(0.0f, t0);
        tMax = t1;

        var planetC = Vector3.Dot(origin, origin) - _planetRadius * _planetRadius;
        var planetDiscriminant = b * b - 4.0f * a * planetC;

        if (planetDiscriminant >= 0.0f)
        {
            var planetSqrt = MathF.Sqrt(planetDiscriminant);
            var pt0 = (-b - planetSqrt) / (2.0f * a);
            var pt1 = (-b + planetSqrt) / (2.0f * a);

            if (pt0 > 0.0f)
            {
                tMax = Math.Min(tMax, pt0);
            }
        }

        return tMax > tMin;
    }

    #endregion
}

public struct AtmosphereData
{
    public Vector3 SunDirection;
    public float MieDirectionalG;
    public Vector3 SunIntensity;
    public float PlanetRadius;
    public Vector3 RayleighScattering;
    public float AtmosphereRadius;
    public Vector3 MieScattering;
    public float HeightScale;
    public int NumSamples;
    public int NumLightSamples;
}
