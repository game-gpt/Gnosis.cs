namespace Gnosis.IR.Shader;

/// <summary>
/// 张量指令定义
/// </summary>
public sealed class TensorInstruction
{
    #region Properties

    public TensorOpCode OpCode { get; set; }

    public List<TensorDimensionIr> Dimensions { get; } = [];

    public List<ShaderIrInstruction> Instructions { get; } = [];

    #endregion
}
