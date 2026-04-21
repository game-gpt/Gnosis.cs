namespace Gnosis.Rendering.Backends.ShaderIR;

public sealed record ShaderStructIr(
    string Name,
    IReadOnlyList<ShaderStructFieldIr> Fields,
    uint SizeInBytes)
{
    public uint ResultId { get; set; }
}
