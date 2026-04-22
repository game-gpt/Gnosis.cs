using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader 函数 IR 定义（已迁移至 Gnosis.IR.Shader）
/// </summary>
public sealed class ShaderFunctionIr
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderIrType ReturnType { get; set; }

    public List<ShaderIrParameter> Parameters { get; } = [];

    public List<ShaderIrInstruction> Instructions { get; } = [];

    public bool IsEntryPoint { get; set; }

    public ShaderExecutionModel? EntryPointModel { get; set; }

    public List<ShaderAttributeIr> Attributes { get; } = [];

    #endregion
}
