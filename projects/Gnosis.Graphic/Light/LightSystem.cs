using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Light;

public sealed class LightSystem
{
    #region 字段

    private readonly List<ILight> _lights;
    private readonly List<IPointLight> _pointLights;
    private readonly List<IDirectionalLight> _directionalLights;
    private readonly List<ISpotLight> _spotLights;
    private readonly List<IAreaLight> _areaLights;
    private readonly Dictionary<string, ILight> _lightsByName;
    private Vector3 _ambientColor;
    private float _ambientIntensity;

    #endregion

    #region 属性

    public IReadOnlyList<ILight> Lights => _lights;
    public IReadOnlyList<IPointLight> PointLights => _pointLights;
    public IReadOnlyList<IDirectionalLight> DirectionalLights => _directionalLights;
    public IReadOnlyList<ISpotLight> SpotLights => _spotLights;
    public IReadOnlyList<IAreaLight> AreaLights => _areaLights;
    public int LightCount => _lights.Count;
    public Vector3 AmbientColor
    {
        get => _ambientColor;
        set => _ambientColor = value;
    }
    public float AmbientIntensity
    {
        get => _ambientIntensity;
        set => _ambientIntensity = value;
    }

    #endregion

    #region 构造函数

    public LightSystem()
    {
        _lights = [];
        _pointLights = [];
        _directionalLights = [];
        _spotLights = [];
        _areaLights = [];
        _lightsByName = [];
        _ambientColor = new Vector3(0.1f, 0.1f, 0.1f);
        _ambientIntensity = 1.0f;
    }

    #endregion

    #region 公开方法 - 光源管理

    public T AddLight<T>(T light) where T : ILight
    {
        _lights.Add(light);
        _lightsByName[light.Name] = light;

        switch (light)
        {
            case IPointLight pointLight:
                _pointLights.Add(pointLight);
                break;
            case IDirectionalLight directionalLight:
                _directionalLights.Add(directionalLight);
                break;
            case ISpotLight spotLight:
                _spotLights.Add(spotLight);
                break;
            case IAreaLight areaLight:
                _areaLights.Add(areaLight);
                break;
        }

        return light;
    }

    public bool RemoveLight(ILight light)
    {
        if (!_lights.Remove(light))
        {
            return false;
        }

        _lightsByName.Remove(light.Name);

        switch (light)
        {
            case IPointLight pointLight:
                _pointLights.Remove(pointLight);
                break;
            case IDirectionalLight directionalLight:
                _directionalLights.Remove(directionalLight);
                break;
            case ISpotLight spotLight:
                _spotLights.Remove(spotLight);
                break;
            case IAreaLight areaLight:
                _areaLights.Remove(areaLight);
                break;
        }

        return true;
    }

    public bool RemoveLight(string name)
    {
        if (!_lightsByName.TryGetValue(name, out var light))
        {
            return false;
        }

        return RemoveLight(light);
    }

    public ILight? GetLight(string name)
    {
        return _lightsByName.GetValueOrDefault(name);
    }

    public T? GetLight<T>(string name) where T : ILight
    {
        return GetLight(name) is T typed ? typed : default;
    }

    public void Clear()
    {
        _lights.Clear();
        _pointLights.Clear();
        _directionalLights.Clear();
        _spotLights.Clear();
        _areaLights.Clear();
        _lightsByName.Clear();
    }

    #endregion

    #region 公开方法 - 光源查询

    public IEnumerable<ILight> GetEnabledLights()
    {
        foreach (var light in _lights)
        {
            if (light.IsEnabled)
            {
                yield return light;
            }
        }
    }

    public IEnumerable<ILight> GetShadowCastingLights()
    {
        foreach (var light in _lights)
        {
            if (light.IsEnabled && light.CastShadows)
            {
                yield return light;
            }
        }
    }

    public int GetShadowCastingLightCount()
    {
        int count = 0;
        foreach (var light in _lights)
        {
            if (light.IsEnabled && light.CastShadows)
            {
                count++;
            }
        }
        return count;
    }

    #endregion

    #region 公开方法 - GPU 数据

    public LightData[] BuildLightDataArray()
    {
        var result = new LightData[_lights.Count];
        for (int i = 0; i < _lights.Count; i++)
        {
            result[i] = BuildLightData(_lights[i]);
        }
        return result;
    }

    public static LightData BuildLightData(ILight light)
    {
        var data = new LightData
        {
            Color = new Vector3(light.Color.X, light.Color.Y, light.Color.Z),
            Intensity = light.Intensity,
            Type = (int)light.Type,
            CastShadows = light.CastShadows ? 1 : 0,
            ShadowStrength = light.ShadowStrength,
            ShadowBias = light.ShadowBias,
            ShadowNormalBias = light.ShadowNormalBias,
            ShadowResolution = light.ShadowResolution
        };

        switch (light)
        {
            case IDirectionalLight directional:
                data.Direction = directional.Direction;
                data.CascadeCount = directional.CascadeCount;
                data.CascadeSplits = directional.CascadeSplits;
                data.CascadeBlend = directional.CascadeBlend;
                break;
            case IPointLight point:
                data.Position = point.Position;
                data.Range = point.Range;
                data.Attenuation = point.Attenuation;
                break;
            case ISpotLight spot:
                data.Position = spot.Position;
                data.Direction = spot.Direction;
                data.Range = spot.Range;
                data.InnerConeAngle = spot.InnerConeAngle;
                data.OuterConeAngle = spot.OuterConeAngle;
                break;
            case IAreaLight area:
                data.Position = area.Position;
                data.Direction = area.Direction;
                data.Range = area.Range;
                data.Width = area.Width;
                data.Height = area.Height;
                break;
        }

        return data;
    }

    #endregion
}

public struct LightData
{
    public Vector3 Color;
    public float Intensity;
    public int Type;
    public int CastShadows;
    public float ShadowStrength;
    public float ShadowBias;
    public float ShadowNormalBias;
    public int ShadowResolution;
    public Vector3 Position;
    public Vector3 Direction;
    public float Range;
    public float Attenuation;
    public int CascadeCount;
    public float[] CascadeSplits;
    public float CascadeBlend;
    public float InnerConeAngle;
    public float OuterConeAngle;
    public float Width;
    public float Height;
}
