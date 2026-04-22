namespace Gnosis.IR.Shader;

/// <summary>
/// Shader 特性 IR 定义
/// </summary>
public sealed class ShaderAttributeIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public List<object> Arguments { get; } = [];

    #endregion
}
