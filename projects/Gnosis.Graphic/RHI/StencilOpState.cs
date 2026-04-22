namespace Gnosis.Graphic.RHI;

/// <summary>
/// 模板操作状态
/// </summary>
public record StencilOpState
{
    public StencilOp FailOp { get; init; }
    public StencilOp PassOp { get; init; }
    public StencilOp DepthFailOp { get; init; }
    public CompareFunction CompareOp { get; init; } = CompareFunction.Always;
    public byte CompareMask { get; init; } = 0xFF;
    public byte WriteMask { get; init; } = 0xFF;
    public byte Reference { get; init; }
}
