namespace Gnosis.Graphic.RHI;

/// <summary>
/// 子通道依赖描述
/// </summary>
public record SubPassDependency
{
    public uint SrcSubPass { get; init; }
    public uint DstSubPass { get; init; }
    public PipelineStageFlag SrcStage { get; init; }
    public PipelineStageFlag DstStage { get; init; }
    public AccessFlag SrcAccess { get; init; }
    public AccessFlag DstAccess { get; init; }
}
