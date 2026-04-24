using System.Numerics;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Terrain;

public sealed class TerrainChunk
{
    #region 字段

    private readonly float[] _patchHeights;

    #endregion

    #region 属性

    public int ChunkX { get; }
    public int ChunkZ { get; }
    public int PatchCountX { get; }
    public int PatchCountZ { get; }
    public float WorldSizeX { get; }
    public float WorldSizeZ { get; }
    public Vector3 WorldOffset { get; }
    public GpuMesh? GpuMesh { get; internal set; }
    public bool IsVisible { get; internal set; }
    public bool IsDirty { get; internal set; }
    public float MinHeight { get; private set; }
    public float MaxHeight { get; private set; }

    #endregion

    #region 构造函数

    public TerrainChunk(int chunkX, int chunkZ, int patchCountX, int patchCountZ, float worldSizeX, float worldSizeZ, Vector3 worldOffset)
    {
        ChunkX = chunkX;
        ChunkZ = chunkZ;
        PatchCountX = patchCountX;
        PatchCountZ = patchCountZ;
        WorldSizeX = worldSizeX;
        WorldSizeZ = worldSizeZ;
        WorldOffset = worldOffset;
        _patchHeights = new float[(patchCountX + 1) * (patchCountZ + 1)];
        IsVisible = true;
        IsDirty = true;
        MinHeight = 0.0f;
        MaxHeight = 0.0f;
    }

    #endregion

    #region 公开方法

    public void SetPatchHeight(int x, int z, float height)
    {
        if (x < 0 || x > PatchCountX || z < 0 || z > PatchCountZ)
        {
            return;
        }

        _patchHeights[z * (PatchCountX + 1) + x] = height;
        IsDirty = true;
    }

    public float GetPatchHeight(int x, int z)
    {
        if (x < 0 || x > PatchCountX || z < 0 || z > PatchCountZ)
        {
            return 0.0f;
        }

        return _patchHeights[z * (PatchCountX + 1) + x];
    }

    public void UpdateBounds()
    {
        MinHeight = float.MaxValue;
        MaxHeight = float.MinValue;

        foreach (var h in _patchHeights)
        {
            if (h < MinHeight)
            {
                MinHeight = h;
            }

            if (h > MaxHeight)
            {
                MaxHeight = h;
            }
        }
    }

    public void BuildMeshFromHeightMap(HeightMap heightMap, float heightScale, IDevice device)
    {
        var vertexCountX = PatchCountX + 1;
        var vertexCountZ = PatchCountZ + 1;
        var vertices = new List<Vertex3D>();
        var indices = new List<uint>();

        var cellSizeX = WorldSizeX / PatchCountX;
        var cellSizeZ = WorldSizeZ / PatchCountZ;

        for (var z = 0; z < vertexCountZ; z++)
        {
            for (var x = 0; x < vertexCountX; x++)
            {
                var u = (float)(ChunkX * PatchCountX + x) / (heightMap.Width - 1);
                var v = (float)(ChunkZ * PatchCountZ + z) / (heightMap.Height - 1);

                u = Math.Clamp(u, 0.0f, 1.0f);
                v = Math.Clamp(v, 0.0f, 1.0f);

                var worldX = WorldOffset.X + x * cellSizeX;
                var worldZ = WorldOffset.Z + z * cellSizeZ;
                var worldY = heightMap.SampleHeight(u, v) * heightScale;

                var normal = heightMap.SampleNormal(u, v, WorldSizeX / heightMap.Width);

                vertices.Add(new Vertex3D(
                    new Vector3(worldX, worldY, worldZ),
                    normal,
                    new Vector2(u, v)));

                SetPatchHeight(x, z, worldY);
            }
        }

        for (var z = 0; z < PatchCountZ; z++)
        {
            for (var x = 0; x < PatchCountX; x++)
            {
                var i0 = (uint)(z * vertexCountX + x);
                var i1 = (uint)(z * vertexCountX + x + 1);
                var i2 = (uint)((z + 1) * vertexCountX + x);
                var i3 = (uint)((z + 1) * vertexCountX + x + 1);

                indices.Add(i0);
                indices.Add(i2);
                indices.Add(i1);

                indices.Add(i1);
                indices.Add(i2);
                indices.Add(i3);
            }
        }

        UpdateBounds();

        GpuMesh?.Dispose();
        GpuMesh = new GpuMesh(device, $"TerrainChunk_{ChunkX}_{ChunkZ}");
        GpuMesh.UploadVertices(vertices.ToArray());
        GpuMesh.UploadIndices(indices.ToArray());
        IsDirty = false;
    }

    #endregion
}
