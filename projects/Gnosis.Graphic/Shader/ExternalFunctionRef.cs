namespace Gnosis.Graphic.Shader;

public sealed record ExternalFunctionRef(
    string SymbolName,
    IReadOnlyList<ShaderIrType> ParameterTypes,
    ShaderIrType ReturnType)
{
    public uint ResultId { get; set; }
}
