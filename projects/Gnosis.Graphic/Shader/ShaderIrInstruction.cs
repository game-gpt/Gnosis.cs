using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader IR 指令定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class ShaderIrInstruction
{
    #region Properties

    public ShaderIrOpCode OpCode { get; set; }

    public ShaderIrType? ResultType { get; set; }

    public object[] Operands { get; set; } = [];

    #endregion
}
