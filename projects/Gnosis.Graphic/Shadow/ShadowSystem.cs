using System.Numerics;
using Gnosis.Core.Math;
using Gnosis.Graphic.Light;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Shadow;

public sealed class ShadowSystem : IShadowSystem, IDisposable
{
    #region 字段

    private readonly IDevice _device;
    private readonly LightSystem _lightSystem;
    private readonly Dictionary<string, ShadowMap> _pointShadowMaps;
    private readonly Dictionary<string, ShadowMap> _spotShadowMaps;
    private CascadedShadowMap? _cascadedShadowMap;
    private bool _isDisposed;

    #endregion

    #region 属性

    public IShadowSettings Settings { get; set; }
    public CascadedShadowMap? CascadedShadowMap => _cascadedShadowMap;
    public IReadOnlyDictionary<string, ShadowMap> PointShadowMaps => _pointShadowMaps;
    public IReadOnlyDictionary<string, ShadowMap> SpotShadowMaps => _spotShadowMaps;

    /// <summary>
    /// 当前相机视图矩阵
    /// </summary>
    public Matrix4x4 CameraView { get; set; }

    /// <summary>
    /// 当前相机投影矩阵
    /// </summary>
    public Matrix4x4 CameraProjection { get; set; }

    /// <summary>
    /// 相机近裁面
    /// </summary>
    public float CameraNearPlane { get; set; }

    /// <summary>
    /// 相机远裁面
    /// </summary>
    public float CameraFarPlane { get; set; }

    #endregion

    #region 构造函数

    public ShadowSystem(IDevice device, LightSystem lightSystem, IShadowSettings? settings = null)
    {
        _device = device;
        _lightSystem = lightSystem;
        _pointShadowMaps = [];
        _spotShadowMaps = [];
        Settings = settings ?? new ShadowSettings();
        CameraView = Matrix4x4.Identity;
        CameraProjection = Matrix4x4.Identity;
        CameraNearPlane = 0.1f;
        CameraFarPlane = 100.0f;
    }

    #endregion

    #region 公开方法

    public void Initialize()
    {
        _cascadedShadowMap = new CascadedShadowMap(
            _device,
            Settings.CascadeCount,
            Settings.Resolution);

        foreach (var light in _lightSystem.PointLights)
        {
            if (light.CastShadows)
            {
                _pointShadowMaps[light.Name] = new ShadowMap(_device, light.ShadowResolution);
            }
        }

        foreach (var light in _lightSystem.SpotLights)
        {
            if (light.CastShadows)
            {
                _spotShadowMaps[light.Name] = new ShadowMap(_device, light.ShadowResolution);
            }
        }
    }

    public void RenderShadowMaps()
    {
        if (_cascadedShadowMap is null)
        {
            return;
        }

        foreach (var light in _lightSystem.DirectionalLights)
        {
            if (light.IsEnabled && light.CastShadows)
            {
                UpdateCascades(light);
            }
        }
    }

    public void UpdateCascades(IDirectionalLight light)
    {
        _cascadedShadowMap?.UpdateCascades(
            light,
            CameraView,
            CameraProjection,
            CameraNearPlane,
            CameraFarPlane);
    }

    public void OnLightAdded(ILight light)
    {
        if (!light.CastShadows)
        {
            return;
        }

        switch (light)
        {
            case IPointLight pointLight:
                _pointShadowMaps[pointLight.Name] = new ShadowMap(_device, pointLight.ShadowResolution);
                break;
            case ISpotLight spotLight:
                _spotShadowMaps[spotLight.Name] = new ShadowMap(_device, spotLight.ShadowResolution);
                break;
        }
    }

    public void OnLightRemoved(ILight light)
    {
        switch (light)
        {
            case IPointLight pointLight:
                if (_pointShadowMaps.Remove(pointLight.Name, out var pointMap))
                {
                    pointMap.Dispose();
                }
                break;
            case ISpotLight spotLight:
                if (_spotShadowMaps.Remove(spotLight.Name, out var spotMap))
                {
                    spotMap.Dispose();
                }
                break;
        }
    }

    public ShadowMap? GetPointShadowMap(string lightName)
    {
        return _pointShadowMaps.GetValueOrDefault(lightName);
    }

    public ShadowMap? GetSpotShadowMap(string lightName)
    {
        return _spotShadowMaps.GetValueOrDefault(lightName);
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _cascadedShadowMap?.Dispose();

        foreach (var map in _pointShadowMaps.Values)
        {
            map.Dispose();
        }

        foreach (var map in _spotShadowMaps.Values)
        {
            map.Dispose();
        }

        _pointShadowMaps.Clear();
        _spotShadowMaps.Clear();

        _isDisposed = true;
    }

    #endregion
}
