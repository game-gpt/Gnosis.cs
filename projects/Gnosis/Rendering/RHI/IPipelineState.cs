namespace Gnosis.Rendering.RHI;

public record PipelineStateDesc(
    ulong ShaderHandle,
    BlendMode BlendMode,
    bool DepthTest,
    CullMode CullMode
);

public interface IPipelineState
{
    BlendMode BlendMode { get; }
    bool DepthTest { get; }
    CullMode CullMode { get; }
    ulong ShaderHandle { get; }
}
