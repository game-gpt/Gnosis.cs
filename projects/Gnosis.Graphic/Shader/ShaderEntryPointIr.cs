using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader 入口点 IR 定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class ShaderEntryPointIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderExecutionModel ExecutionModel { get; set; }

    public string FunctionName { get; set; } = string.Empty;

    #endregion
}
