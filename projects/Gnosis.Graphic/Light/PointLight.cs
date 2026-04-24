using System.Numerics;

namespace Gnosis.Graphic.Light;

public sealed class PointLight : IPointLight
{
    #region 属性

    public string Name { get; }
    public LightType Type => LightType.Point;
    public Vector3 Color { get; set; }
    public float Intensity { get; set; }
    public bool IsEnabled { get; set; }
    public bool CastShadows { get; set; }
    public float ShadowStrength { get; set; }
    public float ShadowBias { get; set; }
    public float ShadowNormalBias { get; set; }
    public float ShadowNearPlane { get; set; }
    public int ShadowResolution { get; set; }
    public Vector3 Position { get; set; }
    public float Range { get; set; }
    public float Attenuation { get; set; }

    #endregion

    #region 构造函数

    public PointLight(string name)
    {
        Name = name;
        Color = new Vector3(1.0f, 1.0f, 1.0f);
        Intensity = 1.0f;
        IsEnabled = true;
        CastShadows = false;
        ShadowStrength = 1.0f;
        ShadowBias = 0.005f;
        ShadowNormalBias = 0.01f;
        ShadowNearPlane = 0.1f;
        ShadowResolution = 1024;
        Position = Vector3.Zero;
        Range = 10.0f;
        Attenuation = 1.0f;
    }

    #endregion
}
