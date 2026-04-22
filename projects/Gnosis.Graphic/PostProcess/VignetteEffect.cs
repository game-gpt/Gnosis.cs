using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.PostProcess;

public sealed class VignetteEffect : PostProcessEffect
{
    #region 属性

    public float Intensity { get; set; }
    public float Smoothness { get; set; }
    public float Roundness { get; set; }
    public float[] Center { get; set; }
    public float[] Color { get; set; }
    public bool Rounded { get; set; }

    #endregion

    #region 构造函数

    public VignetteEffect() : base("Vignette", 20)
    {
        Intensity = 0.3f;
        Smoothness = 0.5f;
        Roundness = 1.0f;
        Center = [0.5f, 0.5f];
        Color = [0.0f, 0.0f, 0.0f, 1.0f];
        Rounded = false;
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
