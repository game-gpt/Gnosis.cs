using System.Numerics;
using System.Runtime.InteropServices;
using Gnosis.Graphic.Material;
using Gnosis.Graphic.Pipeline;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Sky;

public sealed class SkyRenderer : IRenderPass, IDisposable
{
    #region 常量

    private const int SkyVertexCount = 36;

    #endregion

    #region 字段

    private readonly IDevice _device;
    private readonly AtmosphericScattering _atmosphere;
    private readonly ISkySettings _settings;
    private GpuMesh? _skyboxMesh;
    private ParameterBlock? _atmosphereParameterBlock;
    private ParameterBlock? _frameParameterBlock;
    private bool _isDisposed;

    #endregion

    #region 属性

    public string Name { get; }
    public bool Enabled { get; set; }
    public AtmosphericScattering Atmosphere => _atmosphere;
    public ISkySettings Settings => _settings;

    #endregion

    #region 构造函数

    public SkyRenderer(IDevice device, ISkySettings? settings = null)
    {
        _device = device;
        _settings = settings ?? new SkySettings();
        _atmosphere = new AtmosphericScattering();
        _atmosphere.SunDirection = _settings.SunDirection;
        Name = "SkyPass";
        Enabled = true;
    }

    #endregion

    #region 公开方法

    public void Initialize()
    {
        CreateSkyboxMesh();
        CreateParameterBlocks();
    }

    public void UpdateSunDirection(Vector3 direction)
    {
        _atmosphere.SunDirection = direction;
        _settings.SunDirection = direction;
    }

    public void Execute(RenderContext context, ICommandTable commandTable)
    {
        if (!Enabled)
        {
            return;
        }

        UpdateParameters(context);

        if (_skyboxMesh?.VertexBuffer == null)
        {
            return;
        }

        commandTable.SetVertexBuffer(_skyboxMesh.VertexBuffer);

        if (_skyboxMesh.IndexBuffer != null)
        {
            commandTable.SetIndexBuffer(_skyboxMesh.IndexBuffer);
            commandTable.DrawIndexed(_skyboxMesh.IndexCount);
        }
        else
        {
            commandTable.Draw(_skyboxMesh.VertexCount);
        }
    }

    #endregion

    #region 私有方法

    private void CreateSkyboxMesh()
    {
        _skyboxMesh = new GpuMesh(_device, "SkyboxCube");

        var h = 1.0f;
        var vertices = new Vertex3D[SkyVertexCount];

        var faceData = new (Vector3 Normal, Vector3 Up, Vector3 Right)[]
        {
            (Vector3.UnitZ, Vector3.UnitY, -Vector3.UnitX),
            (-Vector3.UnitZ, Vector3.UnitY, Vector3.UnitX),
            (-Vector3.UnitX, Vector3.UnitZ, Vector3.UnitY),
            (-Vector3.UnitX, -Vector3.UnitZ, -Vector3.UnitY),
            (Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ),
            (-Vector3.UnitX, Vector3.UnitY, -Vector3.UnitZ)
        };

        for (var face = 0; face < 6; face++)
        {
            var (normal, up, right) = faceData[face];
            var center = normal * h;

            var v0 = center + right * h + up * h;
            var v1 = center - right * h + up * h;
            var v2 = center - right * h - up * h;
            var v3 = center + right * h - up * h;

            var baseIdx = face * 6;
            vertices[baseIdx + 0] = new Vertex3D(v0, normal, new Vector2(1, 1));
            vertices[baseIdx + 1] = new Vertex3D(v1, normal, new Vector2(0, 1));
            vertices[baseIdx + 2] = new Vertex3D(v2, normal, new Vector2(0, 0));
            vertices[baseIdx + 3] = new Vertex3D(v0, normal, new Vector2(1, 1));
            vertices[baseIdx + 4] = new Vertex3D(v2, normal, new Vector2(0, 0));
            vertices[baseIdx + 5] = new Vertex3D(v3, normal, new Vector2(1, 0));
        }

        _skyboxMesh.UploadVertices(vertices);
    }

    private void CreateParameterBlocks()
    {
        var atmosphereBuilder = new ParameterBlockBuilder("AtmosphereParams");
        atmosphereBuilder.AddFloat3("SunDirection");
        atmosphereBuilder.AddFloat3("SunIntensity");
        atmosphereBuilder.AddFloat3("RayleighScattering");
        atmosphereBuilder.AddFloat3("MieScattering");
        atmosphereBuilder.AddFloat("MieDirectionalG");
        atmosphereBuilder.AddFloat("PlanetRadius");
        atmosphereBuilder.AddFloat("AtmosphereRadius");
        atmosphereBuilder.AddFloat("HeightScale");
        atmosphereBuilder.AddInt("NumSamples");
        atmosphereBuilder.AddInt("NumLightSamples");
        _atmosphereParameterBlock = atmosphereBuilder.Build();

        var frameBuilder = new ParameterBlockBuilder("SkyFrameParams");
        frameBuilder.AddMatrix4x4("InverseViewProjection");
        frameBuilder.AddFloat3("CameraPosition");
        frameBuilder.AddFloat("SkyDistance");
        _frameParameterBlock = frameBuilder.Build();
    }

    private void UpdateParameters(RenderContext context)
    {
        if (_atmosphereParameterBlock == null || _frameParameterBlock == null)
        {
            return;
        }

        var data = _atmosphere.BuildAtmosphereData();

        _atmosphereParameterBlock.SetFloat3("SunDirection", data.SunDirection.X, data.SunDirection.Y, data.SunDirection.Z);
        _atmosphereParameterBlock.SetFloat3("SunIntensity", data.SunIntensity.X, data.SunIntensity.Y, data.SunIntensity.Z);
        _atmosphereParameterBlock.SetFloat3("RayleighScattering", data.RayleighScattering.X, data.RayleighScattering.Y, data.RayleighScattering.Z);
        _atmosphereParameterBlock.SetFloat3("MieScattering", data.MieScattering.X, data.MieScattering.Y, data.MieScattering.Z);
        _atmosphereParameterBlock.SetFloat("MieDirectionalG", data.MieDirectionalG);
        _atmosphereParameterBlock.SetFloat("PlanetRadius", data.PlanetRadius);
        _atmosphereParameterBlock.SetFloat("AtmosphereRadius", data.AtmosphereRadius);
        _atmosphereParameterBlock.SetFloat("HeightScale", data.HeightScale);
        _atmosphereParameterBlock.SetInt("NumSamples", data.NumSamples);
        _atmosphereParameterBlock.SetInt("NumLightSamples", data.NumLightSamples);
        _atmosphereParameterBlock.UploadToGpu(_device);

        var view = context.View;
        var vpMatrix = view.ViewMatrix * view.ProjectionMatrix;

        if (Matrix4x4.Invert(vpMatrix, out var invVP))
        {
            var m = MatrixToFloatArray(invVP);
            _frameParameterBlock.SetMatrix4x4("InverseViewProjection", m);
        }

        if (view is Camera3D camera3D)
        {
            _frameParameterBlock.SetFloat3("CameraPosition", camera3D.Position.X, camera3D.Position.Y, camera3D.Position.Z);
        }

        _frameParameterBlock.SetFloat("SkyDistance", _settings.SkyDistance);
        _frameParameterBlock.UploadToGpu(_device);
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

        _skyboxMesh?.Dispose();
        _atmosphereParameterBlock?.Dispose();
        _frameParameterBlock?.Dispose();

        _isDisposed = true;
    }

    #endregion
}
