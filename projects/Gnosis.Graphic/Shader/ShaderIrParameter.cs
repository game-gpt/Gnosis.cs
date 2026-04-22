using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader IR 参数定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class ShaderIrParameter
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderIrType Type { get; set; }

    public bool IsByRef { get; set; }

    #endregion
}
