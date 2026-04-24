using System.Numerics;
using System.Runtime.InteropServices;
using Gnosis.Graphic.Material;
using Gnosis.Graphic.Pipeline;
using Gnosis.Graphic.RHI;
using Gnosis.Graphic.Terrain;

namespace Gnosis.Graphic.Foliage;

public sealed class GrassRenderer : IRenderPass, IDisposable
{
    #region 常量

    private const int GrassBladeVertices = 7;
    private const int GrassBladeIndices = 9;

    #endregion

    #region 字段

    private readonly IDevice _device;
    private readonly IFoliageSettings _settings;
    private readonly List<FoliageInstance> _instances;
    private GpuMesh? _bladeMesh;
    private IResource? _instanceBuffer;
    private ParameterBlock? _grassParameterBlock;
    private ParameterBlock? _frameParameterBlock;
    private bool _instanceBufferDirty;
    private bool _isDisposed;

    #endregion

    #region 属性

    public string Name { get; }
    public bool Enabled { get; set; }
    public IReadOnlyList<FoliageInstance> Instances => _instances;
    public int InstanceCount => _instances.Count;
    public IFoliageSettings Settings => _settings;

    #endregion

    #region 构造函数

    public GrassRenderer(IDevice device, IFoliageSettings? settings = null)
    {
        _device = device;
        _settings = settings ?? new FoliageSettings();
        _instances = [];
        _instanceBufferDirty = true;
        Name = "GrassPass";
        Enabled = true;
    }

    #endregion

    #region 公开方法

    public void Initialize()
    {
        CreateBladeMesh();
        CreateParameterBlocks();
    }

    public void AddInstance(FoliageInstance instance)
    {
        _instances.Add(instance);
        _instanceBufferDirty = true;
    }

    public void AddInstances(ReadOnlySpan<FoliageInstance> instances)
    {
        foreach (var instance in instances)
        {
            _instances.Add(instance);
        }

        _instanceBufferDirty = true;
    }

    public bool RemoveInstance(int index)
    {
        if (index < 0 || index >= _instances.Count)
        {
            return false;
        }

        _instances.RemoveAt(index);
        _instanceBufferDirty = true;
        return true;
    }

    public void ClearInstances()
    {
        _instances.Clear();
        _instanceBufferDirty = true;
    }

    public void GenerateGrassOnTerrain(TerrainRenderer terrain, float density, int seed = 42)
    {
        _instances.Clear();

        if (terrain.HeightMap == null)
        {
            return;
        }

        var random = new Random(seed);
        var terrainSize = terrain.Settings.TerrainSize;
        var halfSize = terrainSize * 0.5f;

        var instanceCount = (int)(terrainSize * terrainSize * density);
        instanceCount = Math.Min(instanceCount, 500000);

        for (var i = 0; i < instanceCount; i++)
        {
            var x = (random.NextSingle() * 2.0f - 1.0f) * halfSize + terrain.Settings.TerrainOffset.X;
            var z = (random.NextSingle() * 2.0f - 1.0f) * halfSize + terrain.Settings.TerrainOffset.Z;

            var y = terrain.SampleHeight(x, z);

            var scale = 0.8f + random.NextSingle() * 0.4f;
            var rotationY = random.NextSingle() * 360.0f;

            var greenVar = 0.7f + random.NextSingle() * 0.3f;
            var color = FoliageInstance.PackColor(0.2f, greenVar, 0.1f, 1.0f);

            _instances.Add(new FoliageInstance(
                new Vector3(x, y, z),
                scale,
                new Vector3(0.0f, rotationY, 0.0f),
                color,
                0));
        }

        _instanceBufferDirty = true;
    }

    public void GenerateGrassInArea(Vector3 center, float radius, float density, int seed = 42)
    {
        var random = new Random(seed);
        var instanceCount = (int)(MathF.PI * radius * radius * density);
        instanceCount = Math.Min(instanceCount, 100000);

        for (var i = 0; i < instanceCount; i++)
        {
            var angle = random.NextSingle() * MathF.PI * 2.0f;
            var dist = MathF.Sqrt(random.NextSingle()) * radius;

            var x = center.X + MathF.Cos(angle) * dist;
            var z = center.Z + MathF.Sin(angle) * dist;
            var y = center.Y;

            var scale = 0.8f + random.NextSingle() * 0.4f;
            var rotationY = random.NextSingle() * 360.0f;

            var greenVar = 0.7f + random.NextSingle() * 0.3f;
            var color = FoliageInstance.PackColor(0.2f, greenVar, 0.1f, 1.0f);

            _instances.Add(new FoliageInstance(
                new Vector3(x, y, z),
                scale,
                new Vector3(0.0f, rotationY, 0.0f),
                color,
                0));
        }

        _instanceBufferDirty = true;
    }

    public void UpdateInstanceBuffer()
    {
        if (!_instanceBufferDirty || _instances.Count == 0)
        {
            return;
        }

        var dataSize = (uint)(_instances.Count * Marshal.SizeOf<FoliageInstance>());
        var data = MemoryMarshal.AsBytes<FoliageInstance>(_instances.ToArray());

        _instanceBuffer?.Dispose();
        _instanceBuffer = _device.CreateBuffer(new BufferDesc
        {
            Size = dataSize,
            Usage = BufferUsage.VertexBuffer | BufferUsage.TransferDst,
            HostVisible = true,
            InitialData = data.ToArray()
        });

        _instanceBufferDirty = false;
    }

    public void Execute(RenderContext context, ICommandTable commandTable)
    {
        if (!Enabled || _instances.Count == 0)
        {
            return;
        }

        UpdateInstanceBuffer();
        UpdateParameters(context);

        if (_bladeMesh?.VertexBuffer == null || _instanceBuffer == null)
        {
            return;
        }

        commandTable.SetVertexBuffer(_bladeMesh.VertexBuffer);

        if (_bladeMesh.IndexBuffer != null)
        {
            commandTable.SetIndexBuffer(_bladeMesh.IndexBuffer);
            commandTable.DrawIndexed(_bladeMesh.IndexCount, (uint)_instances.Count);
        }
        else
        {
            commandTable.Draw(_bladeMesh.VertexCount, (uint)_instances.Count);
        }
    }

    #endregion

    #region 私有方法

    private void CreateBladeMesh()
    {
        _bladeMesh = new GpuMesh(_device, "GrassBlade");

        var vertices = new Vertex3D[GrassBladeVertices];
        var bladeHeight = 1.0f;
        var bladeWidth = 0.3f;

        vertices[0] = new Vertex3D(new Vector3(-bladeWidth * 0.5f, 0.0f, 0.0f), Vector3.UnitY, new Vector2(0.0f, 0.0f));
        vertices[1] = new Vertex3D(new Vector3(bladeWidth * 0.5f, 0.0f, 0.0f), Vector3.UnitY, new Vector2(1.0f, 0.0f));
        vertices[2] = new Vertex3D(new Vector3(-bladeWidth * 0.35f, bladeHeight * 0.33f, 0.0f), Vector3.UnitY, new Vector2(0.0f, 0.33f));
        vertices[3] = new Vertex3D(new Vector3(bladeWidth * 0.35f, bladeHeight * 0.33f, 0.0f), Vector3.UnitY, new Vector2(1.0f, 0.33f));
        vertices[4] = new Vertex3D(new Vector3(-bladeWidth * 0.2f, bladeHeight * 0.66f, 0.0f), Vector3.UnitY, new Vector2(0.0f, 0.66f));
        vertices[5] = new Vertex3D(new Vector3(bladeWidth * 0.2f, bladeHeight * 0.66f, 0.0f), Vector3.UnitY, new Vector2(1.0f, 0.66f));
        vertices[6] = new Vertex3D(new Vector3(0.0f, bladeHeight, 0.0f), Vector3.UnitY, new Vector2(0.5f, 1.0f));

        _bladeMesh.UploadVertices(vertices);

        var indices = new uint[]
        {
            0, 1, 2,
            1, 3, 2,
            2, 3, 4,
            3, 5, 4,
            4, 5, 6
        };

        _bladeMesh.UploadIndices(indices);
    }

    private void CreateParameterBlocks()
    {
        var grassBuilder = new ParameterBlockBuilder("GrassParams");
        grassBuilder.AddFloat3("WindDirection");
        grassBuilder.AddFloat("WindStrength");
        grassBuilder.AddFloat("WindSpeed");
        grassBuilder.AddFloat("CullDistance");
        grassBuilder.AddInt("EnableWind");
        grassBuilder.AddInt("EnableLod");
        grassBuilder.AddFloat("LodDistance1");
        grassBuilder.AddFloat("LodDistance2");
        _grassParameterBlock = grassBuilder.Build();

        var frameBuilder = new ParameterBlockBuilder("GrassFrameParams");
        frameBuilder.AddMatrix4x4("ViewMatrix");
        frameBuilder.AddMatrix4x4("ProjectionMatrix");
        frameBuilder.AddMatrix4x4("ViewProjectionMatrix");
        frameBuilder.AddFloat3("CameraPosition");
        frameBuilder.AddFloat("Time");
        _frameParameterBlock = frameBuilder.Build();
    }

    private void UpdateParameters(RenderContext context)
    {
        if (_grassParameterBlock == null || _frameParameterBlock == null)
        {
            return;
        }

        _grassParameterBlock.SetFloat3("WindDirection", _settings.WindDirection.X, _settings.WindDirection.Y, _settings.WindDirection.Z);
        _grassParameterBlock.SetFloat("WindStrength", _settings.WindStrength);
        _grassParameterBlock.SetFloat("WindSpeed", _settings.WindSpeed);
        _grassParameterBlock.SetFloat("CullDistance", _settings.CullDistance);
        _grassParameterBlock.SetInt("EnableWind", _settings.EnableWind ? 1 : 0);
        _grassParameterBlock.SetInt("EnableLod", _settings.EnableLod ? 1 : 0);
        _grassParameterBlock.SetFloat("LodDistance1", _settings.LodDistance1);
        _grassParameterBlock.SetFloat("LodDistance2", _settings.LodDistance2);
        _grassParameterBlock.UploadToGpu(_device);

        var view = context.View;
        var vpMatrix = view.ViewMatrix * view.ProjectionMatrix;

        _frameParameterBlock.SetMatrix4x4("ViewMatrix", MatrixToFloatArray(view.ViewMatrix));
        _frameParameterBlock.SetMatrix4x4("ProjectionMatrix", MatrixToFloatArray(view.ProjectionMatrix));
        _frameParameterBlock.SetMatrix4x4("ViewProjectionMatrix", MatrixToFloatArray(vpMatrix));

        if (view is Camera3D camera3D)
        {
            _frameParameterBlock.SetFloat3("CameraPosition", camera3D.Position.X, camera3D.Position.Y, camera3D.Position.Z);
        }

        _frameParameterBlock.SetFloat("Time", context.DeltaTime * context.FrameIndex * 0.001f);
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

        _bladeMesh?.Dispose();
        _instanceBuffer?.Dispose();
        _grassParameterBlock?.Dispose();
        _frameParameterBlock?.Dispose();

        _instances.Clear();

        _isDisposed = true;
    }

    #endregion
}
