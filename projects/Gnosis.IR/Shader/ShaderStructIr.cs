namespace Gnosis.IR.Shader;

/// <summary>
/// Shader 结构体 IR 定义
/// </summary>
public sealed class ShaderStructIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public List<ShaderStructFieldIr> Fields { get; } = [];

    #endregion
}
