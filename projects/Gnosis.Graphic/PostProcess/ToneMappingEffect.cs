using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.PostProcess;

public enum ToneMappingMode
{
    Off,
    ACES,
    Reinhard,
    Uncharted2,
    AgX
}

public sealed class ToneMappingEffect : PostProcessEffect
{
    #region 属性

    public ToneMappingMode Mode { get; set; }
    public float Exposure { get; set; }
    public float WhitePoint { get; set; }

    #endregion

    #region 构造函数

    public ToneMappingEffect() : base("ToneMapping", 0)
    {
        Mode = ToneMappingMode.ACES;
        Exposure = 1.0f;
        WhitePoint = 11.2f;
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
