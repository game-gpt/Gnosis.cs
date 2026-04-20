namespace Gnosis.Compiler.Backend.ShaderIR;

public sealed record ExternalFunctionRef(
    string SymbolName,
    IReadOnlyList<ShaderIrType> ParameterTypes,
    ShaderIrType ReturnType)
{
    public uint ResultId { get; set; }
}
