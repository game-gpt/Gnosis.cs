namespace Gnosis.IR.Shader;

public sealed class ShaderStructIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public List<ShaderStructFieldIr> Fields { get; } = [];

    public uint Size { get; set; }

    #endregion
}
