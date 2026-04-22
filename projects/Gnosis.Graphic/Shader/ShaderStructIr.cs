using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader 结构体 IR 定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class ShaderStructIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public List<ShaderStructFieldIr> Fields { get; } = [];

    #endregion
}
