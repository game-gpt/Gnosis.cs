namespace Gnosis.IR.Shader;

public sealed class ShaderEntryPointIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderExecutionModel ExecutionModel { get; set; }

    public string FunctionName { get; set; } = string.Empty;

    public List<string> InterfaceVariables { get; } = [];

    #endregion
}
