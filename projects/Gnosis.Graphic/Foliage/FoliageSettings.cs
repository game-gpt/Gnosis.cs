using System.Numerics;

namespace Gnosis.Graphic.Foliage;

public sealed class FoliageSettings : IFoliageSettings
{
    #region 属性

    public int MaxInstancesPerCell { get; set; }
    public float CellSize { get; set; }
    public float CullDistance { get; set; }
    public bool EnableWind { get; set; }
    public Vector3 WindDirection { get; set; }
    public float WindStrength { get; set; }
    public float WindSpeed { get; set; }
    public bool EnableLod { get; set; }
    public float LodDistance1 { get; set; }
    public float LodDistance2 { get; set; }

    #endregion

    #region 构造函数

    public FoliageSettings()
    {
        MaxInstancesPerCell = 1024;
        CellSize = 50.0f;
        CullDistance = 200.0f;
        EnableWind = true;
        WindDirection = Vector3.Normalize(new Vector3(1.0f, 0.0f, 0.5f));
        WindStrength = 0.3f;
        WindSpeed = 1.5f;
        EnableLod = true;
        LodDistance1 = 50.0f;
        LodDistance2 = 150.0f;
    }

    #endregion
}
