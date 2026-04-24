namespace Gnosis.IR.Shader;

/// <summary>
/// Shader IR 指令定义
/// </summary>
public class ShaderIrInstruction
{
    #region Properties

    public ShaderIrOpCode OpCode { get; set; }

    public ShaderIrType? ResultType { get; set; }

    public object[] Operands { get; set; } = [];

    #endregion
}
