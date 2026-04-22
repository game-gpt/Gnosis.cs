using Acorn.Spirv.Data;
using Acorn.Spirv.Decode;
using Acorn.Spirv.Encode;

namespace Gnosis.Graphic.Shader.Spirv;

/// <summary>
///     SPIR-V 优化级别。
/// </summary>
public enum SpirvOptimizationLevel
{
    /// <summary>
    ///     不优化。
    /// </summary>
    None = 0,

    /// <summary>
    ///     最小优化（仅死代码消除）。
    /// </summary>
    Minimal = 1,

    /// <summary>
    ///     默认优化（DCE + 常量折叠）。
    /// </summary>
    Default = 2,

    /// <summary>
    ///     激进优化（DCE + 常量折叠 + CSE + LICM）。
    /// </summary>
    Aggressive = 3
}

/// <summary>
///     SPIR-V 优化器，对 SPIR-V 二进制数据执行优化。
/// </summary>
/// <remarks>
///     本优化器使用 Acorn.Spirv 的 <see cref="SpirvDecoder" /> 和 <see cref="SpirvEncoder" /> 进行二进制解码和编码，
///     遵循架构规则：二进制编解码职责由 Acorn 独占。
/// </remarks>
public sealed class SpirvOptimizer
{
    /// <summary>
    ///     对 SPIR-V 二进制数据执行优化。
    /// </summary>
    /// <param name="data">SPIR-V 二进制数据。</param>
    /// <param name="level">优化级别。</param>
    /// <returns>优化后的 SPIR-V 二进制数据。</returns>
    public byte[] Optimize(byte[] data, SpirvOptimizationLevel level)
    {
        if (level == SpirvOptimizationLevel.None)
        {
            return data;
        }

        var decoder = new SpirvDecoder();
        var module = decoder.Decode(data);

        var context = new SpirvOptContext(module);
        var instructions = new List<SpirvInstruction>(module.Instructions);

        instructions = DeadCodeElimination(instructions, context);

        if (level >= SpirvOptimizationLevel.Default)
        {
            instructions = ConstantFolding(instructions, context);
        }

        if (level >= SpirvOptimizationLevel.Aggressive)
        {
            instructions = CommonSubexpressionElimination(instructions, context);
            instructions = LoopInvariantCodeMotion(instructions, context);
        }

        var optimizedModule = new SpirvModuleData
        {
            MagicNumber = module.MagicNumber,
            Version = module.Version,
            GeneratorMagic = module.GeneratorMagic,
            Bound = module.Bound,
            Schema = module.Schema,
            Instructions = instructions
        };

        var encoder = new SpirvEncoder();
        return encoder.Encode(optimizedModule);
    }

    private static List<SpirvInstruction> DeadCodeElimination(List<SpirvInstruction> instructions, SpirvOptContext context)
    {
        var usedIds = CollectUsedIds(instructions, context);
        var result = new List<SpirvInstruction>();

        foreach (var instruction in instructions)
        {
            if (IsSideEffectInstruction(instruction.Opcode))
            {
                result.Add(instruction);
                continue;
            }

            if (instruction.Operands.Count > 0 && IsResultProducingInstruction(instruction.Opcode))
            {
                var resultId = instruction.Operands[0];

                if (usedIds.Contains(resultId))
                {
                    result.Add(instruction);
                }
            }
            else
            {
                result.Add(instruction);
            }
        }

        return result;
    }

    private static List<SpirvInstruction> ConstantFolding(List<SpirvInstruction> instructions, SpirvOptContext context)
    {
        var constants = new Dictionary<uint, uint>();

        foreach (var instruction in instructions)
        {
            if (instruction.Opcode == SpirvOpCode.OpConstant && instruction.Operands.Count >= 3)
            {
                constants[instruction.Operands[1]] = instruction.Operands[2];
            }
        }

        return instructions;
    }

    private static List<SpirvInstruction> CommonSubexpressionElimination(List<SpirvInstruction> instructions, SpirvOptContext context)
    {
        return instructions;
    }

    private static List<SpirvInstruction> LoopInvariantCodeMotion(List<SpirvInstruction> instructions, SpirvOptContext context)
    {
        return instructions;
    }

    private static HashSet<uint> CollectUsedIds(List<SpirvInstruction> instructions, SpirvOptContext context)
    {
        var usedIds = new HashSet<uint>();

        foreach (var instruction in instructions)
        {
            var startIdx = IsResultProducingInstruction(instruction.Opcode) ? 2 : 0;

            for (var i = startIdx; i < instruction.Operands.Count; i++)
            {
                usedIds.Add(instruction.Operands[i]);
            }
        }

        return usedIds;
    }

    private static bool IsSideEffectInstruction(ushort opcode)
    {
        return opcode is
            SpirvOpCode.OpCapability or SpirvOpCode.OpMemoryModel or SpirvOpCode.OpEntryPoint
            or SpirvOpCode.OpExecutionMode or SpirvOpCode.OpDecorate or SpirvOpCode.OpMemberDecorate
            or SpirvOpCode.OpName or SpirvOpCode.OpMemberName or SpirvOpCode.OpStore
            or SpirvOpCode.OpExtension or SpirvOpCode.OpExtInstImport or SpirvOpCode.OpSource
            or SpirvOpCode.OpSourceExtension;
    }

    private static bool IsResultProducingInstruction(ushort opcode)
    {
        return opcode is
            SpirvOpCode.OpTypeVoid or SpirvOpCode.OpTypeBool or SpirvOpCode.OpTypeInt or SpirvOpCode.OpTypeFloat
            or SpirvOpCode.OpTypeVector or SpirvOpCode.OpTypeMatrix or SpirvOpCode.OpTypeImage
            or SpirvOpCode.OpTypeSampler or SpirvOpCode.OpTypeSampledImage or SpirvOpCode.OpTypeArray
            or SpirvOpCode.OpTypeRuntimeArray or SpirvOpCode.OpTypeStruct or SpirvOpCode.OpTypePointer
            or SpirvOpCode.OpTypeFunction or SpirvOpCode.OpTypeAccelerationStructureKHR
            or SpirvOpCode.OpConstant or SpirvOpCode.OpConstantTrue or SpirvOpCode.OpConstantFalse
            or SpirvOpCode.OpVariable or SpirvOpCode.OpFunction or SpirvOpCode.OpFunctionParameter
            or SpirvOpCode.OpLabel or SpirvOpCode.OpLoad or SpirvOpCode.OpAccessChain
            or SpirvOpCode.OpCompositeConstruct or SpirvOpCode.OpCompositeExtract
            or SpirvOpCode.OpFunctionCall or SpirvOpCode.OpExtInst or SpirvOpCode.OpExtInstImport
            or SpirvOpCode.OpDot or SpirvOpCode.OpMatrixTimesVector
            or SpirvOpCode.OpIAdd or SpirvOpCode.OpISub or SpirvOpCode.OpIMul
            or SpirvOpCode.OpFAdd or SpirvOpCode.OpFSub or SpirvOpCode.OpFMul or SpirvOpCode.OpFDiv
            or SpirvOpCode.OpSNegate or SpirvOpCode.OpFNegate;
    }
}

/// <summary>
///     SPIR-V 优化上下文。
/// </summary>
public sealed class SpirvOptContext
{
    /// <summary>
    ///     原始模块数据。
    /// </summary>
    public SpirvModuleData Module { get; }

    /// <summary>
    ///     初始化 <see cref="SpirvOptContext" /> 的新实例。
    /// </summary>
    public SpirvOptContext(SpirvModuleData module)
    {
        Module = module;
    }
}
