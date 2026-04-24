using System.Numerics;
using Gnosis.Graphic.Material;
using Gnosis.Graphic.Pipeline;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Terrain;

public sealed class TerrainRenderer : IRenderPass, IDisposable
{
    #region 字段

    private readonly IDevice _device;
    private readonly ITerrainSettings _settings;
    private readonly Dictionary<(int x, int z), TerrainChunk> _chunks;
    private readonly List<TerrainChunk> _dirtyChunks;
    private HeightMap? _heightMap;
    private ParameterBlock? _terrainParameterBlock;
    private ParameterBlock? _frameParameterBlock;
    private IResource? _heightMapTexture;
    private IResource? _terrainSampler;
    private bool _isDisposed;

    #endregion

    #region 属性

    public string Name { get; }
    public bool Enabled { get; set; }
    public HeightMap? HeightMap => _heightMap;
    public IReadOnlyDictionary<(int x, int z), TerrainChunk> Chunks => _chunks;
    public ITerrainSettings Settings => _settings;

    #endregion

    #region 构造函数

    public TerrainRenderer(IDevice device, ITerrainSettings? settings = null)
    {
        _device = device;
        _settings = settings ?? new TerrainSettings();
        _chunks = [];
        _dirtyChunks = [];
        Name = "TerrainPass";
        Enabled = true;
    }

    #endregion

    #region 公开方法

    public void Initialize()
    {
        CreateParameterBlocks();
        CreateSampler();
    }

    public void LoadHeightMap(HeightMap heightMap)
    {
        _heightMap = heightMap;
        RebuildAllChunks();
        UploadHeightMapTexture();
    }

    public void LoadHeightMapFromRaw(byte[] data, int width, int height, float heightScale = 1.0f)
    {
        _heightMap = HeightMap.FromRawData(data, width, height, heightScale);
        RebuildAllChunks();
        UploadHeightMapTexture();
    }

    public void LoadHeightMapFromR16(byte[] data, int width, int height, float heightScale = 1.0f / 65535.0f)
    {
        _heightMap = HeightMap.FromR16Data(data, width, height, heightScale);
        RebuildAllChunks();
        UploadHeightMapTexture();
    }

    public void GenerateTerrain(int heightMapResolution = 256, float noiseScale = 2.0f, int octaves = 6, float persistence = 0.5f, float lacunarity = 2.0f, int seed = 42)
    {
        _heightMap = HeightMap.GeneratePerlinNoise(heightMapResolution, heightMapResolution, noiseScale, octaves, persistence, lacunarity, seed);
        RebuildAllChunks();
        UploadHeightMapTexture();
    }

    public float SampleHeight(float worldX, float worldZ)
    {
        if (_heightMap == null)
        {
            return 0.0f;
        }

        var u = (worldX - _settings.TerrainOffset.X + _settings.TerrainSize * 0.5f) / _settings.TerrainSize;
        var v = (worldZ - _settings.TerrainOffset.Z + _settings.TerrainSize * 0.5f) / _settings.TerrainSize;

        u = Math.Clamp(u, 0.0f, 1.0f);
        v = Math.Clamp(v, 0.0f, 1.0f);

        return _heightMap.SampleHeight(u, v) * _settings.HeightScale;
    }

    public Vector3 SampleNormal(float worldX, float worldZ)
    {
        if (_heightMap == null)
        {
            return Vector3.UnitY;
        }

        var u = (worldX - _settings.TerrainOffset.X + _settings.TerrainSize * 0.5f) / _settings.TerrainSize;
        var v = (worldZ - _settings.TerrainOffset.Z + _settings.TerrainSize * 0.5f) / _settings.TerrainSize;

        u = Math.Clamp(u, 0.0f, 1.0f);
        v = Math.Clamp(v, 0.0f, 1.0f);

        return _heightMap.SampleNormal(u, v, _settings.TerrainSize);
    }

    public void RebuildDirtyChunks()
    {
        if (_heightMap == null)
        {
            return;
        }

        foreach (var chunk in _dirtyChunks)
        {
            chunk.BuildMeshFromHeightMap(_heightMap, _settings.HeightScale, _device);
        }

        _dirtyChunks.Clear();
    }

    public void RebuildAllChunks()
    {
        if (_heightMap == null)
        {
            return;
        }

        CreateChunks();

        foreach (var chunk in _chunks.Values)
        {
            chunk.BuildMeshFromHeightMap(_heightMap, _settings.HeightScale, _device);
        }

        _dirtyChunks.Clear();
    }

    public void MarkChunkDirty(int chunkX, int chunkZ)
    {
        if (_chunks.TryGetValue((chunkX, chunkZ), out var chunk) && !_dirtyChunks.Contains(chunk))
        {
            _dirtyChunks.Add(chunk);
        }
    }

    public void Execute(RenderContext context, ICommandTable commandTable)
    {
        if (!Enabled)
        {
            return;
        }

        RebuildDirtyChunks();
        UpdateParameters(context);

        foreach (var chunk in _chunks.Values)
        {
            if (!chunk.IsVisible || chunk.GpuMesh?.VertexBuffer == null)
            {
                continue;
            }

            commandTable.SetVertexBuffer(chunk.GpuMesh.VertexBuffer);

            if (chunk.GpuMesh.IndexBuffer != null)
            {
                commandTable.SetIndexBuffer(chunk.GpuMesh.IndexBuffer);
                commandTable.DrawIndexed(chunk.GpuMesh.IndexCount);
            }
            else
            {
                commandTable.Draw(chunk.GpuMesh.VertexCount);
            }
        }
    }

    #endregion

    #region 私有方法

    private void CreateChunks()
    {
        foreach (var chunk in _chunks.Values)
        {
            chunk.GpuMesh?.Dispose();
        }

        _chunks.Clear();
        _dirtyChunks.Clear();

        var chunkSizeX = _settings.TerrainSize / _settings.ChunksX;
        var chunkSizeZ = _settings.TerrainSize / _settings.ChunksZ;
        var patchesPerChunkX = _settings.PatchesPerChunk;
        var patchesPerChunkZ = _settings.PatchesPerChunk;

        for (var cz = 0; cz < _settings.ChunksZ; cz++)
        {
            for (var cx = 0; cx < _settings.ChunksX; cx++)
            {
                var worldOffset = new Vector3(
                    _settings.TerrainOffset.X - _settings.TerrainSize * 0.5f + cx * chunkSizeX,
                    _settings.TerrainOffset.Y,
                    _settings.TerrainOffset.Z - _settings.TerrainSize * 0.5f + cz * chunkSizeZ);

                var chunk = new TerrainChunk(
                    cx, cz,
                    patchesPerChunkX, patchesPerChunkZ,
                    chunkSizeX, chunkSizeZ,
                    worldOffset);

                _chunks[(cx, cz)] = chunk;
            }
        }
    }

    private void CreateParameterBlocks()
    {
        var terrainBuilder = new ParameterBlockBuilder("TerrainParams");
        terrainBuilder.AddFloat("TerrainSize");
        terrainBuilder.AddFloat("HeightScale");
        terrainBuilder.AddFloat3("TerrainOffset");
        terrainBuilder.AddFloat("LodDistance1");
        terrainBuilder.AddFloat("LodDistance2");
        terrainBuilder.AddFloat("LodDistance3");
        terrainBuilder.AddInt("EnableTessellation");
        terrainBuilder.AddFloat("TessellationFactor");
        _terrainParameterBlock = terrainBuilder.Build();

        var frameBuilder = new ParameterBlockBuilder("TerrainFrameParams");
        frameBuilder.AddMatrix4x4("ViewMatrix");
        frameBuilder.AddMatrix4x4("ProjectionMatrix");
        frameBuilder.AddMatrix4x4("ViewProjectionMatrix");
        frameBuilder.AddFloat3("CameraPosition");
        _frameParameterBlock = frameBuilder.Build();
    }

    private void CreateSampler()
    {
        var desc = new SamplerDesc
        {
            MagFilter = FilterMode.Linear,
            MinFilter = FilterMode.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge
        };

        _terrainSampler = _device.CreateSampler(desc);
    }

    private void UploadHeightMapTexture()
    {
        if (_heightMap == null)
        {
            return;
        }

        _heightMapTexture?.Dispose();

        var pixelData = new byte[_heightMap.Width * _heightMap.Height * sizeof(float)];
        for (var i = 0; i < _heightMap.Width * _heightMap.Height; i++)
        {
            var h = _heightMap[i % _heightMap.Width, i / _heightMap.Width];
            BitConverter.TryWriteBytes(pixelData.AsSpan(i * sizeof(float)), h);
        }

        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = (uint)_heightMap.Width,
            Height = (uint)_heightMap.Height,
            Format = ResourceFormat.R32G32B32A32Float,
            Usage = TextureUsage.ShaderResource,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
        };

        _heightMapTexture = _device.CreateTexture(desc);
    }

    private void UpdateParameters(RenderContext context)
    {
        if (_terrainParameterBlock == null || _frameParameterBlock == null)
        {
            return;
        }

        _terrainParameterBlock.SetFloat("TerrainSize", _settings.TerrainSize);
        _terrainParameterBlock.SetFloat("HeightScale", _settings.HeightScale);
        _terrainParameterBlock.SetFloat3("TerrainOffset", _settings.TerrainOffset.X, _settings.TerrainOffset.Y, _settings.TerrainOffset.Z);
        _terrainParameterBlock.SetFloat("LodDistance1", _settings.LodDistance1);
        _terrainParameterBlock.SetFloat("LodDistance2", _settings.LodDistance2);
        _terrainParameterBlock.SetFloat("LodDistance3", _settings.LodDistance3);
        _terrainParameterBlock.SetInt("EnableTessellation", _settings.EnableTessellation ? 1 : 0);
        _terrainParameterBlock.SetFloat("TessellationFactor", _settings.TessellationFactor);
        _terrainParameterBlock.UploadToGpu(_device);

        var view = context.View;
        var vpMatrix = view.ViewMatrix * view.ProjectionMatrix;

        _frameParameterBlock.SetMatrix4x4("ViewMatrix", MatrixToFloatArray(view.ViewMatrix));
        _frameParameterBlock.SetMatrix4x4("ProjectionMatrix", MatrixToFloatArray(view.ProjectionMatrix));
        _frameParameterBlock.SetMatrix4x4("ViewProjectionMatrix", MatrixToFloatArray(vpMatrix));

        if (view is Camera3D camera3D)
        {
            _frameParameterBlock.SetFloat3("CameraPosition", camera3D.Position.X, camera3D.Position.Y, camera3D.Position.Z);
        }

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

        foreach (var chunk in _chunks.Values)
        {
            chunk.GpuMesh?.Dispose();
        }

        _chunks.Clear();
        _dirtyChunks.Clear();

        _heightMapTexture?.Dispose();
        _terrainSampler?.Dispose();
        _terrainParameterBlock?.Dispose();
        _frameParameterBlock?.Dispose();

        _isDisposed = true;
    }

    #endregion
}
