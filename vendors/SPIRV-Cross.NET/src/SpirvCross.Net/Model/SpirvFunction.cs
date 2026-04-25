using Acorn.Spirv.Data;

namespace SpirvCross.Net.Model;

/// <summary>
///     SPIR-V 函数模型，表示从 SPIR-V 指令中提取的函数信息。
/// </summary>
public sealed class SpirvFunction
{
    /// <summary>
    ///     函数结果 ID。
    /// </summary>
    public uint ResultId { get; init; }

    /// <summary>
    ///     函数名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     返回类型 ID。
    /// </summary>
    public uint ReturnTypeId { get; init; }

    /// <summary>
    ///     函数类型 ID。
    /// </summary>
    public uint FunctionTypeId { get; init; }

    /// <summary>
    ///     参数列表。
    /// </summary>
    public List<SpirvFunctionParameter> Parameters { get; init; } = [];

    /// <summary>
    ///     局部变量列表。
    /// </summary>
    public List<SpirvVariable> LocalVariables { get; init; } = [];

    /// <summary>
    ///     指令列表（函数体）。
    /// </summary>
    public List<SpirvInstruction> Instructions { get; init; } = [];
}

/// <summary>
///     SPIR-V 函数参数。
/// </summary>
public sealed class SpirvFunctionParameter
{
    /// <summary>
    ///     参数结果 ID。
    /// </summary>
    public uint ResultId { get; init; }

    /// <summary>
    ///     参数类型 ID。
    /// </summary>
    public uint TypeId { get; init; }

    /// <summary>
    ///     参数名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
