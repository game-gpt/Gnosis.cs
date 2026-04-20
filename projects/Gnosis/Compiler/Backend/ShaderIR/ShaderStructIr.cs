namespace Gnosis.Compiler.Backend.ShaderIR;

public sealed record ShaderStructIr(
    string Name,
    IReadOnlyList<ShaderStructFieldIr> Fields,
    uint SizeInBytes)
{
    public uint ResultId { get; set; }
}
