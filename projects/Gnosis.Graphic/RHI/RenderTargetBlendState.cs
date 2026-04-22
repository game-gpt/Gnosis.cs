namespace Gnosis.Graphic.RHI;

/// <summary>
/// 渲染目标混合状态
/// </summary>
public record RenderTargetBlendState
{
    public bool BlendEnable { get; init; }
    public BlendFactor SrcBlend { get; init; } = BlendFactor.One;
    public BlendFactor DstBlend { get; init; } = BlendFactor.Zero;
    public BlendOp BlendOp { get; init; } = BlendOp.Add;
    public BlendFactor SrcAlphaBlend { get; init; } = BlendFactor.One;
    public BlendFactor DstAlphaBlend { get; init; } = BlendFactor.Zero;
    public BlendOp AlphaBlendOp { get; init; } = BlendOp.Add;
    public ColorWriteMask ColorWriteMask { get; init; } = ColorWriteMask.All;
}
