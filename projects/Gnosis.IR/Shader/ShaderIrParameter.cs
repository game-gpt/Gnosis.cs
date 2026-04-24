namespace Gnosis.IR.Shader;

public sealed class ShaderIrParameter
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderIrType Type { get; set; } = ShaderIrType.Void;

    public uint ResultId { get; set; }

    public bool IsByRef { get; set; }

    #endregion
}
