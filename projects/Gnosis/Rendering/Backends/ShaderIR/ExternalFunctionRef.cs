namespace Gnosis.Rendering.Backends.ShaderIR;

public sealed record ExternalFunctionRef(
    string SymbolName,
    IReadOnlyList<ShaderIrType> ParameterTypes,
    ShaderIrType ReturnType)
{
    public uint ResultId { get; set; }
}
