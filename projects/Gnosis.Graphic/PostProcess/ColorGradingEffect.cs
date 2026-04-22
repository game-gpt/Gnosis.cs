using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.PostProcess;

public sealed class ColorGradingEffect : PostProcessEffect
{
    #region 属性

    public float Temperature { get; set; }
    public float Tint { get; set; }
    public float Saturation { get; set; }
    public float Contrast { get; set; }
    public float Brightness { get; set; }
    public float[] ColorFilter { get; set; }
    public float HueShift { get; set; }
    public bool UseLut { get; set; }
    public IResource? LutTexture { get; set; }
    public float LutContribution { get; set; }

    #endregion

    #region 构造函数

    public ColorGradingEffect() : base("ColorGrading", 5)
    {
        Temperature = 0.0f;
        Tint = 0.0f;
        Saturation = 1.0f;
        Contrast = 1.0f;
        Brightness = 0.0f;
        ColorFilter = [1.0f, 1.0f, 1.0f, 1.0f];
        HueShift = 0.0f;
        UseLut = false;
        LutTexture = null;
        LutContribution = 1.0f;
    }

    #endregion

    #region 公开方法

    public override void Setup(ICommandTable commandTable, uint width, uint height)
    {
    }

    public override void Execute(ICommandTable commandTable, IResource inputTexture, IResource outputTexture)
    {
    }

    #endregion
}
