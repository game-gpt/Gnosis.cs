namespace Gnosis.IR.Shader;

/// <summary>
/// Shader IR 参数定义
/// </summary>
public sealed class ShaderIrParameter
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderIrType Type { get; set; } = ShaderIrType.Void;

    public bool IsByRef { get; set; }

    #endregion
}
