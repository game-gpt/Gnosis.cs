namespace Gnosis.Renderer;

public record PipelineStateDesc(
    ulong ShaderHandle,
    Enums.BlendMode BlendMode,
    bool DepthTest,
    Enums.CullMode CullMode
);

public interface IPipelineState
{
    Enums.BlendMode BlendMode { get; }
    bool DepthTest { get; }
    Enums.CullMode CullMode { get; }
    ulong ShaderHandle { get; }
}
