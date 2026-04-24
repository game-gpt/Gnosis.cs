using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Pipeline;

public sealed class VoxelRenderer
{
    #region 常量

    private const int MaxVoxelsPerChunk = 32768;

    #endregion

    #region 字段

    private readonly IDevice _device;
    private readonly Dictionary<string, VoxelChunk> _chunks = [];
    private readonly List<VoxelChunk> _dirtyChunks = [];
    private GpuMesh? _unitCubeMesh;
    private bool _isDisposed;

    #endregion

    #region 属性

    public IReadOnlyDictionary<string, VoxelChunk> Chunks => _chunks;
    public float VoxelSize { get; set; }

    #endregion

    #region 构造函数

    public VoxelRenderer(IDevice device, float voxelSize = 1.0f)
    {
        _device = device;
        VoxelSize = voxelSize;
    }

    #endregion

    #region 公开方法 - 体素管理

    public VoxelChunk CreateChunk(string name, int sizeX, int sizeY, int sizeZ)
    {
        var chunk = new VoxelChunk(name, sizeX, sizeY, sizeZ);
        _chunks[name] = chunk;
        return chunk;
    }

    public bool RemoveChunk(string name)
    {
        if (_chunks.Remove(name, out var chunk))
        {
            chunk.GpuMesh?.Dispose();
            return true;
        }

        return false;
    }

    public VoxelChunk? GetChunk(string name)
    {
        return _chunks.GetValueOrDefault(name);
    }

    public void SetVoxel(string chunkName, int x, int y, int z, uint color)
    {
        if (_chunks.TryGetValue(chunkName, out var chunk))
        {
            chunk.SetVoxel(x, y, z, color);

            if (!_dirtyChunks.Contains(chunk))
            {
                _dirtyChunks.Add(chunk);
            }
        }
    }

    public uint GetVoxel(string chunkName, int x, int y, int z)
    {
        if (_chunks.TryGetValue(chunkName, out var chunk))
        {
            return chunk.GetVoxel(x, y, z);
        }

        return 0;
    }

    #endregion

    #region 公开方法 - 渲染

    public void RebuildDirtyChunks()
    {
        foreach (var chunk in _dirtyChunks)
        {
            RebuildChunkMesh(chunk);
        }

        _dirtyChunks.Clear();
    }

    public void RebuildAllChunks()
    {
        foreach (var chunk in _chunks.Values)
        {
            RebuildChunkMesh(chunk);
        }

        _dirtyChunks.Clear();
    }

    public void Render(ICommandTable commandTable)
    {
        EnsureUnitCubeMesh();

        foreach (var chunk in _chunks.Values)
        {
            if (chunk.GpuMesh == null || !chunk.IsVisible)
            {
                continue;
            }

            commandTable.SetVertexBuffer(chunk.GpuMesh.VertexBuffer!);

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

    private void EnsureUnitCubeMesh()
    {
        if (_unitCubeMesh != null)
        {
            return;
        }

        _unitCubeMesh = new GpuMesh(_device, "VoxelUnitCube");

        var h = VoxelSize * 0.5f;
        var vertices = new Vertex3D[24];
        var normals = new[]
        {
            Vector3.UnitZ, -Vector3.UnitZ,
            Vector3.UnitX, -Vector3.UnitX,
            Vector3.UnitY, -Vector3.UnitY
        };

        var faceVertices = new[]
        {
            new Vector3(-h, -h, h), new Vector3(h, -h, h), new Vector3(h, h, h), new Vector3(-h, h, h),
            new Vector3(h, -h, -h), new Vector3(-h, -h, -h), new Vector3(-h, h, -h), new Vector3(h, h, -h),
            new Vector3(h, -h, h), new Vector3(h, -h, -h), new Vector3(h, h, -h), new Vector3(h, h, h),
            new Vector3(-h, -h, -h), new Vector3(-h, -h, h), new Vector3(-h, h, h), new Vector3(-h, h, -h),
            new Vector3(-h, h, h), new Vector3(h, h, h), new Vector3(h, h, -h), new Vector3(-h, h, -h),
            new Vector3(-h, -h, -h), new Vector3(h, -h, -h), new Vector3(h, -h, h), new Vector3(-h, -h, h)
        };

        var uvs = new[]
        {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
        };

        for (var face = 0; face < 6; face++)
        {
            for (var v = 0; v < 4; v++)
            {
                vertices[face * 4 + v] = new Vertex3D(faceVertices[face * 4 + v], normals[face], uvs[v]);
            }
        }

        _unitCubeMesh.UploadVertices(vertices);

        var indices = new uint[]
        {
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23
        };

        _unitCubeMesh.UploadIndices(indices);
    }

    private void RebuildChunkMesh(VoxelChunk chunk)
    {
        chunk.GpuMesh?.Dispose();

        var vertices = new List<Vertex3D>();
        var indices = new List<uint>();
        uint vertexOffset = 0;

        for (var z = 0; z < chunk.SizeZ; z++)
        {
            for (var y = 0; y < chunk.SizeY; y++)
            {
                for (var x = 0; x < chunk.SizeX; x++)
                {
                    var voxel = chunk.GetVoxel(x, y, z);
                    if (voxel == 0)
                    {
                        continue;
                    }

                    var center = new Vector3(x, y, z) * VoxelSize;

                    var h = VoxelSize * 0.5f;
                    var faceNormals = new[]
                    {
                        Vector3.UnitZ, -Vector3.UnitZ,
                        Vector3.UnitX, -Vector3.UnitX,
                        Vector3.UnitY, -Vector3.UnitY
                    };

                    var faceOffsets = new Vector3[]
                    {
                        new(0, 0, h), new(0, 0, -h),
                        new(h, 0, 0), new(-h, 0, 0),
                        new(0, h, 0), new(0, -h, 0)
                    };

                    var faceVertexOffsets = new Vector3[][]
                    {
                        [new(-h, -h, 0), new(h, -h, 0), new(h, h, 0), new(-h, h, 0)],
                        [new(h, -h, 0), new(-h, -h, 0), new(-h, h, 0), new(h, h, 0)],
                        [new(0, -h, h), new(0, -h, -h), new(0, h, -h), new(0, h, h)],
                        [new(0, -h, -h), new(0, -h, h), new(0, h, h), new(0, h, -h)],
                        [new(-h, 0, h), new(h, 0, h), new(h, 0, -h), new(-h, 0, -h)],
                        [new(-h, 0, -h), new(h, 0, -h), new(h, 0, h), new(-h, 0, h)]
                    };

                    var faceUv = new Vector2[] { new(0, 0), new(1, 0), new(1, 1), new(0, 1) };

                    for (var face = 0; face < 6; face++)
                    {
                        var nx = x + (int)faceNormals[face].X;
                        var ny = y + (int)faceNormals[face].Y;
                        var nz = z + (int)faceNormals[face].Z;

                        if (nx >= 0 && nx < chunk.SizeX &&
                            ny >= 0 && ny < chunk.SizeY &&
                            nz >= 0 && nz < chunk.SizeZ &&
                            chunk.GetVoxel(nx, ny, nz) != 0)
                        {
                            continue;
                        }

                        var faceCenter = center + faceOffsets[face];

                        for (var v = 0; v < 4; v++)
                        {
                            vertices.Add(new Vertex3D(
                                faceCenter + faceVertexOffsets[face][v],
                                faceNormals[face],
                                faceUv[v]));
                        }

                        indices.Add(vertexOffset);
                        indices.Add(vertexOffset + 1);
                        indices.Add(vertexOffset + 2);
                        indices.Add(vertexOffset);
                        indices.Add(vertexOffset + 2);
                        indices.Add(vertexOffset + 3);

                        vertexOffset += 4;
                    }
                }
            }
        }

        if (vertices.Count == 0)
        {
            chunk.IsVisible = false;
            return;
        }

        chunk.IsVisible = true;

        var mesh = new GpuMesh(_device, $"VoxelChunk_{chunk.Name}");
        mesh.UploadVertices(vertices.ToArray());
        mesh.UploadIndices(indices.ToArray());
        chunk.GpuMesh = mesh;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _unitCubeMesh?.Dispose();

        foreach (var chunk in _chunks.Values)
        {
            chunk.GpuMesh?.Dispose();
        }

        _chunks.Clear();
        _dirtyChunks.Clear();

        _isDisposed = true;
    }

    #endregion
}

public sealed class VoxelChunk
{
    #region 字段

    private readonly uint[,,] _voxels;

    #endregion

    #region 属性

    public string Name { get; }
    public int SizeX { get; }
    public int SizeY { get; }
    public int SizeZ { get; }
    public GpuMesh? GpuMesh { get; internal set; }
    public bool IsVisible { get; internal set; }
    public Vector3 Offset { get; set; }

    #endregion

    #region 构造函数

    public VoxelChunk(string name, int sizeX, int sizeY, int sizeZ)
    {
        Name = name;
        SizeX = sizeX;
        SizeY = sizeY;
        SizeZ = sizeZ;
        _voxels = new uint[sizeX, sizeY, sizeZ];
        IsVisible = true;
    }

    #endregion

    #region 公开方法

    public void SetVoxel(int x, int y, int z, uint color)
    {
        if (x >= 0 && x < SizeX && y >= 0 && y < SizeY && z >= 0 && z < SizeZ)
        {
            _voxels[x, y, z] = color;
        }
    }

    public uint GetVoxel(int x, int y, int z)
    {
        if (x >= 0 && x < SizeX && y >= 0 && y < SizeY && z >= 0 && z < SizeZ)
        {
            return _voxels[x, y, z];
        }

        return 0;
    }

    public void Fill(uint color)
    {
        for (var x = 0; x < SizeX; x++)
        {
            for (var y = 0; y < SizeY; y++)
            {
                for (var z = 0; z < SizeZ; z++)
                {
                    _voxels[x, y, z] = color;
                }
            }
        }
    }

    public void Clear()
    {
        Array.Clear(_voxels);
    }

    public int GetActiveVoxelCount()
    {
        var count = 0;
        for (var x = 0; x < SizeX; x++)
        {
            for (var y = 0; y < SizeY; y++)
            {
                for (var z = 0; z < SizeZ; z++)
                {
                    if (_voxels[x, y, z] != 0)
                    {
                        count++;
                    }
                }
            }
        }

        return count;
    }

    #endregion
}
