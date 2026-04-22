namespace Gnosis.Graphic.Shadow;

public sealed class ShadowSettings : IShadowSettings
{
    #region 属性

    public int Resolution { get; set; }
    public float Distance { get; set; }
    public int CascadeCount { get; set; }
    public float[] CascadeSplits { get; set; }
    public float Bias { get; set; }
    public float NormalBias { get; set; }
    public bool SoftShadows { get; set; }
    public int SoftShadowQuality { get; set; }

    #endregion

    #region 构造函数

    public ShadowSettings()
    {
        Resolution = 2048;
        Distance = 100.0f;
        CascadeCount = 4;
        CascadeSplits = [0.05f, 0.15f, 0.35f, 1.0f];
        Bias = 0.005f;
        NormalBias = 0.01f;
        SoftShadows = true;
        SoftShadowQuality = 2;
    }

    #endregion
}
