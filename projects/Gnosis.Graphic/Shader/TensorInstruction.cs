using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// 张量指令定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class TensorInstruction
{
    #region Properties

    public TensorOpCode OpCode { get; set; }

    public List<TensorDimensionIr> Dimensions { get; } = [];

    public List<ShaderIrInstruction> Instructions { get; } = [];

    #endregion
}
