namespace Gnosis.IR.Shader;

/// <summary>
/// Shader 模块 IR 定义
/// </summary>
public sealed class ShaderModuleIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderLanguage Language { get; set; }

    public ShaderTarget Target { get; set; }

    public List<ShaderStructIr> Structs { get; } = [];

    public List<ShaderResourceIr> Resources { get; } = [];

    public List<ShaderFunctionIr> Functions { get; } = [];

    public List<ShaderEntryPointIr> EntryPoints { get; } = [];

    public List<ShaderGlobalVariableIr> GlobalVariables { get; } = [];

    public List<TensorInstruction> TensorInstructions { get; } = [];

    public List<ExternalFunctionRef> ExternalFunctions { get; } = [];

    #endregion
}
