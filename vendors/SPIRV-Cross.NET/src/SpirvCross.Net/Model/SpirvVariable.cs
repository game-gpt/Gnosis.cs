using Acorn.Spirv.Data;

namespace SpirvCross.Net.Model;

/// <summary>
///     SPIR-V 变量模型，表示全局或局部变量。
/// </summary>
public sealed class SpirvVariable
{
    /// <summary>
    ///     变量结果 ID。
    /// </summary>
    public uint ResultId { get; init; }

    /// <summary>
    ///     变量类型 ID（指针类型）。
    /// </summary>
    public uint TypeId { get; init; }

    /// <summary>
    ///     变量名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     存储类。
    /// </summary>
    public SpirvStorageClass StorageClass { get; init; }

    /// <summary>
    ///     初始化值 ID（可选）。
    /// </summary>
    public uint? InitializerId { get; init; }
}

/// <summary>
///     SPIR-V 装饰信息。
/// </summary>
public sealed class SpirvDecorationInfo
{
    /// <summary>
    ///     目标 ID。
    /// </summary>
    public uint TargetId { get; init; }

    /// <summary>
    ///     装饰类型。
    /// </summary>
    public SpirvDecoration Decoration { get; init; }

    /// <summary>
    ///     额外操作数。
    /// </summary>
    public List<uint> ExtraOperands { get; init; } = [];
}

/// <summary>
///     SPIR-V 入口点模型。
/// </summary>
public sealed class SpirvEntryPoint
{
    /// <summary>
    ///     执行模型。
    /// </summary>
    public SpirvExecutionModel ExecutionModel { get; init; }

    /// <summary>
    ///     入口点函数 ID。
    /// </summary>
    public uint FunctionId { get; init; }

    /// <summary>
    ///     入口点名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     接口变量 ID 列表。
    /// </summary>
    public List<uint> InterfaceIds { get; init; } = [];
}

/// <summary>
///     SPIR-V 指令模型，表示单条 SPIR-V 指令的高层抽象。
/// </summary>
public sealed class SpirvInstruction
{
    /// <summary>
    ///     操作码。
    /// </summary>
    public SpirvOpCode OpCode { get; init; }

    /// <summary>
    ///     结果类型 ID（可选）。
    /// </summary>
    public uint? ResultTypeId { get; init; }

    /// <summary>
    ///     结果 ID（可选）。
    /// </summary>
    public uint? ResultId { get; init; }

    /// <summary>
    ///     操作数列表。
    /// </summary>
    public List<uint> Operands { get; init; } = [];
}
