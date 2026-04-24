using Gnosis.Graphic.Shader;

namespace Gnosis.Graphic.RHI;

public record ShaderDesc
{
    public required byte[] Bytecode { get; init; }
    public required ShaderStage Stage { get; init; }
    public string EntryPoint { get; init; } = "main";
    public bool IsSpirv { get; init; }
}
