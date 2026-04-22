namespace Gnosis.IR.Shader;

/// <summary>
/// 外部函数引用定义
/// </summary>
public sealed class ExternalFunctionRef
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public string Library { get; set; } = string.Empty;

    public ShaderIrType ReturnType { get; set; }

    public List<ShaderIrType> ParameterTypes { get; } = [];

    #endregion
}
