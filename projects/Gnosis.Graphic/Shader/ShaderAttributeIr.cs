using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader 特性 IR 定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class ShaderAttributeIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public List<object> Arguments { get; } = [];

    #endregion
}
