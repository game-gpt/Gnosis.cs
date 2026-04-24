using System.Numerics;
using System.Runtime.InteropServices;
using Gnosis.Graphic.Material;
using Gnosis.Graphic.Pipeline;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Sky;

public sealed class SkyboxRenderer : IRenderPass, IDisposable
{
    #region 常量

    private const int CubeVertexCount = 36;

    #endregion

    #region 字段

    private readonly IDevice _device;
    private GpuMesh? _cubeMesh;
    private IResource? _cubemapTexture;
    private IResource? _cubemapSampler;
    private ParameterBlock? _skyboxParameterBlock;
    private bool _isDisposed;

    #endregion

    #region 属性

    public string Name { get; }
    public bool Enabled { get; set; }
    public IResource? CubemapTexture => _cubemapTexture;

    #endregion

    #region 构造函数

    public SkyboxRenderer(IDevice device)
    {
        _device = device;
        Name = "SkyboxPass";
        Enabled = true;
    }

    #endregion

    #region 公开方法

    public void Initialize()
    {
        CreateCubeMesh();
        CreateParameterBlock();
        CreateSampler();
    }

    public void LoadCubemap(byte[] positiveX, byte[] negativeX, byte[] positiveY, byte[] negativeY, byte[] positiveZ, byte[] negativeZ, uint width, uint height)
    {
        _cubemapTexture?.Dispose();

        var desc = new TextureDesc
        {
            Dimension = TextureDimension.TextureCube,
            Width = width,
            Height = height,
            Format = ResourceFormat.R8G8B8A8Unorm,
            Usage = TextureUsage.ShaderResource,
            MipLevels = 1,
            ArrayLayers = 6,
            SampleCount = 1
        };

        _cubemapTexture = _device.CreateTexture(desc);
    }

    public void LoadCubemapFromSingleTexture(byte[] equirectangularData, uint width, uint height)
    {
        _cubemapTexture?.Dispose();

        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = width,
            Height = height,
            Format = ResourceFormat.R8G8B8A8Unorm,
            Usage = TextureUsage.ShaderResource,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
        };

        _cubemapTexture = _device.CreateTexture(desc);
    }

    public void Execute(RenderContext context, ICommandTable commandTable)
    {
        if (!Enabled || _cubemapTexture == null)
        {
            return;
        }

        UpdateParameters(context);

        if (_cubeMesh?.VertexBuffer == null)
        {
            return;
        }

        commandTable.SetVertexBuffer(_cubeMesh.VertexBuffer);

        if (_cubeMesh.IndexBuffer != null)
        {
            commandTable.SetIndexBuffer(_cubeMesh.IndexBuffer);
            commandTable.DrawIndexed(_cubeMesh.IndexCount);
        }
        else
        {
            commandTable.Draw(_cubeMesh.VertexCount);
        }
    }

    #endregion

    #region 私有方法

    private void CreateCubeMesh()
    {
        _cubeMesh = new GpuMesh(_device, "SkyboxCube");

        var h = 1.0f;
        var vertices = new Vertex3D[CubeVertexCount];

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

        _cubeMesh.UploadVertices(vertices);
    }

    private void CreateParameterBlock()
    {
        var builder = new ParameterBlockBuilder("SkyboxParams");
        builder.AddMatrix4x4("InverseViewProjection");
        builder.AddFloat3("CameraPosition");
        builder.AddFloat("SkyDistance");
        _skyboxParameterBlock = builder.Build();
    }

    private void CreateSampler()
    {
        var desc = new SamplerDesc
        {
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat
        };

        _cubemapSampler = _device.CreateSampler(desc);
    }

    private void UpdateParameters(RenderContext context)
    {
        if (_skyboxParameterBlock == null)
        {
            return;
        }

        var view = context.View;
        var vpMatrix = view.ViewMatrix * view.ProjectionMatrix;

        if (Matrix4x4.Invert(vpMatrix, out var invVP))
        {
            var m = MatrixToFloatArray(invVP);
            _skyboxParameterBlock.SetMatrix4x4("InverseViewProjection", m);
        }

        if (view is Camera3D camera3D)
        {
            _skyboxParameterBlock.SetFloat3("CameraPosition", camera3D.Position.X, camera3D.Position.Y, camera3D.Position.Z);
        }

        _skyboxParameterBlock.SetFloat("SkyDistance", 1000.0f);
        _skyboxParameterBlock.UploadToGpu(_device);
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

        _cubeMesh?.Dispose();
        _cubemapTexture?.Dispose();
        _cubemapSampler?.Dispose();
        _skyboxParameterBlock?.Dispose();

        _isDisposed = true;
    }

    #endregion
}
