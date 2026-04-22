using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// 外部函数引用定义（已迁移至 Gnosis.IR.Shader）
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
