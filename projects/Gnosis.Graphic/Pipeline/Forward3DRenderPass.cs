using System.Numerics;
using Gnosis.Graphic.Light;
using Gnosis.Graphic.Material;
using Gnosis.Graphic.RHI;
using Gnosis.Graphic.Shadow;

namespace Gnosis.Graphic.Pipeline;

public sealed class Forward3DRenderPass : IRenderPass
{
    #region 字段

    private readonly LightSystem _lightSystem;
    private readonly MeshRenderer _meshRenderer;
    private readonly VoxelRenderer? _voxelRenderer;
    private readonly IDevice _device;
    private ParameterBlock? _frameParameterBlock;
    private ParameterBlock? _lightParameterBlock;
    private bool _isDisposed;

    #endregion

    #region 属性

    public string Name { get; }
    public bool Enabled { get; set; }
    public ShadowRenderPass? ShadowPass { get; }

    #endregion

    #region 构造函数

    public Forward3DRenderPass(IDevice device, LightSystem lightSystem, MeshRenderer meshRenderer, ShadowRenderPass? shadowPass = null, VoxelRenderer? voxelRenderer = null)
    {
        _device = device;
        _lightSystem = lightSystem;
        _meshRenderer = meshRenderer;
        _voxelRenderer = voxelRenderer;
        ShadowPass = shadowPass;
        Name = "Forward3D";
        Enabled = true;
    }

    #endregion

    #region 公开方法

    public void Execute(RenderContext context, ICommandTable commandTable)
    {
        if (!Enabled)
        {
            return;
        }

        UpdateFrameParameters(context);
        UpdateLightParameters();

        _meshRenderer.Render(commandTable, context.View as ICamera);
        _voxelRenderer?.Render(commandTable);
    }

    #endregion

    #region 私有方法

    private void UpdateFrameParameters(RenderContext context)
    {
        if (_frameParameterBlock == null)
        {
            var builder = new ParameterBlockBuilder("FrameParams");
            builder.AddMatrix4x4("ViewMatrix");
            builder.AddMatrix4x4("ProjectionMatrix");
            builder.AddMatrix4x4("ViewProjectionMatrix");
            builder.AddFloat3("CameraPosition");
            builder.AddFloat("DeltaTime");
            _frameParameterBlock = builder.Build();
        }

        var view = context.View;
        var viewMatrix = view.ViewMatrix;
        var projMatrix = view.ProjectionMatrix;
        var viewProjMatrix = viewMatrix * projMatrix;

        _frameParameterBlock.SetMatrix4x4("ViewMatrix", MatrixToFloatArray(viewMatrix));
        _frameParameterBlock.SetMatrix4x4("ProjectionMatrix", MatrixToFloatArray(projMatrix));
        _frameParameterBlock.SetMatrix4x4("ViewProjectionMatrix", MatrixToFloatArray(viewProjMatrix));
        _frameParameterBlock.SetFloat("DeltaTime", context.DeltaTime);

        if (view is Camera3D camera3D)
        {
            _frameParameterBlock.SetFloat3("CameraPosition", camera3D.Position.X, camera3D.Position.Y, camera3D.Position.Z);
        }

        _frameParameterBlock.UploadToGpu(_device);
    }

    private void UpdateLightParameters()
    {
        if (_lightParameterBlock == null)
        {
            var builder = new ParameterBlockBuilder("LightParams");
            builder.AddFloat3("AmbientColor");
            builder.AddFloat("AmbientIntensity");
            builder.AddInt("LightCount");
            _lightParameterBlock = builder.Build();
        }

        _lightParameterBlock.SetFloat3("AmbientColor", _lightSystem.AmbientColor.X, _lightSystem.AmbientColor.Y, _lightSystem.AmbientColor.Z);
        _lightParameterBlock.SetFloat("AmbientIntensity", _lightSystem.AmbientIntensity);
        _lightParameterBlock.SetInt("LightCount", _lightSystem.LightCount);

        _lightParameterBlock.UploadToGpu(_device);
    }

    private static float[] MatrixToFloatArray(Matrix4x4 matrix)
    {
        return
        [
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        ];
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _frameParameterBlock?.Dispose();
        _lightParameterBlock?.Dispose();

        _isDisposed = true;
    }

    #endregion
}
