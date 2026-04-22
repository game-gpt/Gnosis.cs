namespace Gnosis.Graphic.Light;

public sealed class AreaLight : IAreaLight
{
    #region 属性

    public string Name { get; }
    public LightType Type => LightType.Area;
    public float[] Color { get; set; }
    public float Intensity { get; set; }
    public bool IsEnabled { get; set; }
    public bool CastShadows { get; set; }
    public float ShadowStrength { get; set; }
    public float ShadowBias { get; set; }
    public float ShadowNormalBias { get; set; }
    public float ShadowNearPlane { get; set; }
    public int ShadowResolution { get; set; }
    public float[] Position { get; set; }
    public float[] Direction { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public float Range { get; set; }

    #endregion

    #region 构造函数

    public AreaLight(string name)
    {
        Name = name;
        Color = [1.0f, 1.0f, 1.0f];
        Intensity = 1.0f;
        IsEnabled = true;
        CastShadows = false;
        ShadowStrength = 1.0f;
        ShadowBias = 0.005f;
        ShadowNormalBias = 0.01f;
        ShadowNearPlane = 0.1f;
        ShadowResolution = 1024;
        Position = [0.0f, 0.0f, 0.0f];
        Direction = [0.0f, -1.0f, 0.0f];
        Width = 1.0f;
        Height = 1.0f;
        Range = 10.0f;
    }

    #endregion
}
