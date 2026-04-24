namespace Gnosis.IR.Shader;

public sealed class LocalVariableInstruction
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderIrType Type { get; set; } = ShaderIrType.Void;

    public ShaderIrType ResultType { get; set; } = ShaderIrType.Void;

    public uint ResultId { get; set; }

    public int StackOffset { get; set; }

    #endregion
}
