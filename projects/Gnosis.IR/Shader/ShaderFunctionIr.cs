namespace Gnosis.IR.Shader;

/// <summary>
/// Shader 函数 IR 定义
/// </summary>
public sealed class ShaderFunctionIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderIrType ReturnType { get; set; } = ShaderIrType.Void;

    public List<ShaderIrParameter> Parameters { get; } = [];

    public List<ShaderIrInstruction> Instructions { get; } = [];

    public bool IsEntryPoint { get; set; }

    public ShaderExecutionModel? EntryPointModel { get; set; }

    public List<ShaderAttributeIr> Attributes { get; } = [];

    #endregion
}
