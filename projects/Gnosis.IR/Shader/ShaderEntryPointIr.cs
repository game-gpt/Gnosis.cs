namespace Gnosis.IR.Shader;

/// <summary>
/// Shader 入口点 IR 定义
/// </summary>
public sealed class ShaderEntryPointIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderExecutionModel ExecutionModel { get; set; }

    public string FunctionName { get; set; } = string.Empty;

    #endregion
}
