using System.Numerics;

namespace Gnosis.Graphic.Foliage;

public interface IFoliageSettings
{
    int MaxInstancesPerCell { get; set; }
    float CellSize { get; set; }
    float CullDistance { get; set; }
    bool EnableWind { get; set; }
    Vector3 WindDirection { get; set; }
    float WindStrength { get; set; }
    float WindSpeed { get; set; }
    bool EnableLod { get; set; }
    float LodDistance1 { get; set; }
    float LodDistance2 { get; set; }
}
