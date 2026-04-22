namespace Gnosis.IR.Shader;

/// <summary>
/// 局部变量指令定义
/// </summary>
public sealed class LocalVariableInstruction
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderIrType Type { get; set; }

    public int StackOffset { get; set; }

    #endregion
}
