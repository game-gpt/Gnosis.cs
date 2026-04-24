using System.Numerics;

namespace Gnosis.Graphic.Terrain;

public sealed class TerrainSettings : ITerrainSettings
{
    #region 属性

    public float TerrainSize { get; set; }
    public float HeightScale { get; set; }
    public int ChunksX { get; set; }
    public int ChunksZ { get; set; }
    public int PatchesPerChunk { get; set; }
    public float LodDistance1 { get; set; }
    public float LodDistance2 { get; set; }
    public float LodDistance3 { get; set; }
    public bool EnableTessellation { get; set; }
    public float TessellationFactor { get; set; }
    public Vector3 TerrainOffset { get; set; }

    #endregion

    #region 构造函数

    public TerrainSettings()
    {
        TerrainSize = 512.0f;
        HeightScale = 100.0f;
        ChunksX = 4;
        ChunksZ = 4;
        PatchesPerChunk = 64;
        LodDistance1 = 50.0f;
        LodDistance2 = 150.0f;
        LodDistance3 = 300.0f;
        EnableTessellation = false;
        TessellationFactor = 1.0f;
        TerrainOffset = Vector3.Zero;
    }

    #endregion
}
