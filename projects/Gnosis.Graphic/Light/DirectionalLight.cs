using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Light;

public sealed class DirectionalLight : IDirectionalLight
{
    #region 属性

    public string Name { get; }
    public LightType Type => LightType.Directional;
    public float[] Color { get; set; }
    public float Intensity { get; set; }
    public bool IsEnabled { get; set; }
    public bool CastShadows { get; set; }
    public float ShadowStrength { get; set; }
    public float ShadowBias { get; set; }
    public float ShadowNormalBias { get; set; }
    public float ShadowNearPlane { get; set; }
    public int ShadowResolution { get; set; }
    public float[] Direction { get; set; }
    public int CascadeCount { get; set; }
    public float[] CascadeSplits { get; set; }
    public float CascadeBlend { get; set; }

    #endregion

    #region 构造函数

    public DirectionalLight(string name)
    {
        Name = name;
        Color = [1.0f, 1.0f, 1.0f];
        Intensity = 1.0f;
        IsEnabled = true;
        CastShadows = true;
        ShadowStrength = 1.0f;
        ShadowBias = 0.005f;
        ShadowNormalBias = 0.01f;
        ShadowNearPlane = 0.1f;
        ShadowResolution = 2048;
        Direction = [0.0f, -1.0f, 0.0f];
        CascadeCount = 4;
        CascadeSplits = [0.05f, 0.15f, 0.35f, 1.0f];
        CascadeBlend = 0.1f;
    }

    #endregion
}
