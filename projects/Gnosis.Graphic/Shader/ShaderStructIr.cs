namespace Gnosis.Graphic.Shader;

public sealed record ShaderStructIr(
    string Name,
    IReadOnlyList<ShaderStructFieldIr> Fields,
    uint SizeInBytes)
{
    public uint ResultId { get; set; }
}
