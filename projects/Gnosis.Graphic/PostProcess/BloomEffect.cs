using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.PostProcess;

public sealed class BloomEffect : PostProcessEffect
{
    #region 属性

    public float Intensity { get; set; }
    public float Threshold { get; set; }
    public float SoftThreshold { get; set; }
    public float Scatter { get; set; }
    public int MaxIterations { get; set; }
    public float DownsampleScale { get; set; }

    #endregion

    #region 构造函数

    public BloomEffect() : base("Bloom", 10)
    {
        Intensity = 0.5f;
        Threshold = 1.0f;
        SoftThreshold = 0.5f;
        Scatter = 0.7f;
        MaxIterations = 6;
        DownsampleScale = 2.0f;
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
