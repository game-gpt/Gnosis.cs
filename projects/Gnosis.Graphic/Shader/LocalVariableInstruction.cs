using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// 局部变量指令定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class LocalVariableInstruction
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderIrType Type { get; set; }

    public int StackOffset { get; set; }

    #endregion
}
