using System.Numerics;

namespace Gnosis.Graphic.Terrain;

public interface ITerrainSettings
{
    float TerrainSize { get; set; }
    float HeightScale { get; set; }
    int ChunksX { get; set; }
    int ChunksZ { get; set; }
    int PatchesPerChunk { get; set; }
    float LodDistance1 { get; set; }
    float LodDistance2 { get; set; }
    float LodDistance3 { get; set; }
    bool EnableTessellation { get; set; }
    float TessellationFactor { get; set; }
    Vector3 TerrainOffset { get; set; }
}
