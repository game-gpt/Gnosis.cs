using System.Numerics;
using Gnosis.Graphic.Light;
using Gnosis.Graphic.RHI;
using Gnosis.Graphic.Shadow;

namespace Gnosis.Graphic.Pipeline;

public sealed class ShadowRenderPass : IRenderPass, IDisposable
{
    #region 字段

    private readonly ShadowSystem _shadowSystem;
    private readonly LightSystem _lightSystem;
    private readonly MeshRenderer _meshRenderer;
    private readonly IDevice _device;
    private IPipelineState? _shadowPipelineState;
    private bool _isDisposed;

    #endregion

    #region 属性

    public string Name { get; }
    public bool Enabled { get; set; }
    public ShadowSystem ShadowSystem => _shadowSystem;

    #endregion

    #region 构造函数

    public ShadowRenderPass(IDevice device, LightSystem lightSystem, MeshRenderer meshRenderer, IShadowSettings? settings = null)
    {
        _device = device;
        _lightSystem = lightSystem;
        _meshRenderer = meshRenderer;
        _shadowSystem = new ShadowSystem(device, lightSystem, settings);
        Name = "ShadowPass";
        Enabled = true;
    }

    #endregion

    #region 公开方法

    public void Initialize()
    {
        _shadowSystem.Initialize();
    }

    public void UpdateCamera(Camera3D camera)
    {
        _shadowSystem.CameraView = camera.ViewMatrix;
        _shadowSystem.CameraProjection = camera.ProjectionMatrix;
        _shadowSystem.CameraNearPlane = camera.NearPlane;
        _shadowSystem.CameraFarPlane = camera.FarPlane;
    }

    public void Execute(RenderContext context, ICommandTable commandTable)
    {
        if (!Enabled)
        {
            return;
        }

        _shadowSystem.RenderShadowMaps();

        RenderDirectionalLightShadows(commandTable);
        RenderPointLightShadows(commandTable);
        RenderSpotLightShadows(commandTable);
    }

    #endregion

    #region 私有方法

    private void RenderDirectionalLightShadows(ICommandTable commandTable)
    {
        var cascadedShadowMap = _shadowSystem.CascadedShadowMap;
        if (cascadedShadowMap == null)
        {
            return;
        }

        foreach (var light in _lightSystem.DirectionalLights)
        {
            if (!light.IsEnabled || !light.CastShadows)
            {
                continue;
            }

            _shadowSystem.UpdateCascades(light);

            for (int cascade = 0; cascade < cascadedShadowMap.CascadeCount; cascade++)
            {
                commandTable.BeginRenderPass(
                    cascadedShadowMap.RenderPass,
                    cascadedShadowMap.Framebuffers[cascade],
                    null,
                    1.0f,
                    0);

                commandTable.SetViewport(0, 0, (uint)cascadedShadowMap.Resolution, (uint)cascadedShadowMap.Resolution);
                commandTable.SetScissor(0, 0, (uint)cascadedShadowMap.Resolution, (uint)cascadedShadowMap.Resolution);

                _meshRenderer.RenderShadow(commandTable);

                commandTable.EndRenderPass();
            }
        }
    }

    private void RenderPointLightShadows(ICommandTable commandTable)
    {
        foreach (var light in _lightSystem.PointLights)
        {
            if (!light.IsEnabled || !light.CastShadows)
            {
                continue;
            }

            var shadowMap = _shadowSystem.GetPointShadowMap(light.Name);
            if (shadowMap == null)
            {
                continue;
            }

            commandTable.BeginRenderPass(shadowMap.RenderPass, shadowMap.Framebuffer, null, 1.0f, 0);

            commandTable.SetViewport(0, 0, (uint)shadowMap.Resolution, (uint)shadowMap.Resolution);
            commandTable.SetScissor(0, 0, (uint)shadowMap.Resolution, (uint)shadowMap.Resolution);

            _meshRenderer.RenderShadow(commandTable);

            commandTable.EndRenderPass();
        }
    }

    private void RenderSpotLightShadows(ICommandTable commandTable)
    {
        foreach (var light in _lightSystem.SpotLights)
        {
            if (!light.IsEnabled || !light.CastShadows)
            {
                continue;
            }

            var shadowMap = _shadowSystem.GetSpotShadowMap(light.Name);
            if (shadowMap == null)
            {
                continue;
            }

            commandTable.BeginRenderPass(shadowMap.RenderPass, shadowMap.Framebuffer, null, 1.0f, 0);

            commandTable.SetViewport(0, 0, (uint)shadowMap.Resolution, (uint)shadowMap.Resolution);
            commandTable.SetScissor(0, 0, (uint)shadowMap.Resolution, (uint)shadowMap.Resolution);

            _meshRenderer.RenderShadow(commandTable);

            commandTable.EndRenderPass();
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _shadowSystem.Dispose();
        _shadowPipelineState?.Dispose();

        _isDisposed = true;
    }

    #endregion
}
