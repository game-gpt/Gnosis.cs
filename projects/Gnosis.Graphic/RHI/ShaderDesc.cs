using Gnosis.IR.Shader;

namespace Gnosis.Graphic.RHI;

/// <summary>
/// 着色器模块描述
/// </summary>
public record ShaderDesc
{
    public required byte[] Bytecode { get; init; }
    public required ShaderStage Stage { get; init; }
    public string EntryPoint { get; init; } = "main";
}
