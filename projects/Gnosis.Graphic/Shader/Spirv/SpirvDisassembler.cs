using System.Text;
using Acorn.Spirv.Data;
using Acorn.Spirv.Decode;

namespace Gnosis.Graphic.Shader.Spirv;

/// <summary>
///     SPIR-V 反汇编器，将 SPIR-V 二进制数据反汇编为人类可读的文本格式。
/// </summary>
/// <remarks>
///     本反汇编器使用 Acorn.Spirv 的 <see cref="SpirvDecoder" /> 进行二进制解码，
///     遵循架构规则：二进制编解码职责由 Acorn 独占。
/// </remarks>
public sealed class SpirvDisassembler
{
    /// <summary>
    ///     将 SPIR-V 二进制数据反汇编为可读文本。
    /// </summary>
    /// <param name="data">SPIR-V 二进制数据。</param>
    /// <returns>反汇编文本。</returns>
    public string Disassemble(byte[] data)
    {
        var module = new SpirvDecoder(data).Decode();

        var sb = new StringBuilder();

        sb.AppendLine($"; SPIR-V");
        sb.AppendLine($"; Version: {FormatVersion(module.Version)}");
        sb.AppendLine($"; Generator: 0x{module.GeneratorMagic:X8}");
        sb.AppendLine($"; Bound: {module.Bound}");
        sb.AppendLine($"; Schema: {module.Schema}");
        sb.AppendLine();

        foreach (var instruction in module.Instructions)
        {
            sb.AppendLine(FormatInstruction(instruction));
        }

        return sb.ToString();
    }

    private static string FormatVersion(uint version)
    {
        var major = (version >> 16) & 0xFF;
        var minor = (version >> 8) & 0xFF;
        var patch = version & 0xFF;
        return patch > 0 ? $"{major}.{minor}.{patch}" : $"{major}.{minor}";
    }

    private static string FormatInstruction(SpirvInstruction instruction)
    {
        var opName = GetOpName(instruction.Opcode);
        var operands = FormatOperands(instruction);
        return string.IsNullOrEmpty(operands) ? opName : $"{opName} {operands}";
    }

    private static string FormatOperands(SpirvInstruction instruction)
    {
        if (instruction.Operands.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        foreach (var operand in instruction.Operands)
        {
            parts.Add($"0x{operand:X8}");
        }

        return string.Join(", ", parts);
    }

    private static string GetOpName(SpirvOpCode opcode)
    {
        return opcode switch
        {
            SpirvOpCode.OpNop => "OpNop",
            SpirvOpCode.OpUndef => "OpUndef",
            SpirvOpCode.OpSource => "OpSource",
            SpirvOpCode.OpSourceExtension => "OpSourceExtension",
            SpirvOpCode.OpName => "OpName",
            SpirvOpCode.OpMemberName => "OpMemberName",
            SpirvOpCode.OpString => "OpString",
            SpirvOpCode.OpLine => "OpLine",
            SpirvOpCode.OpExtension => "OpExtension",
            SpirvOpCode.OpExtInstImport => "OpExtInstImport",
            SpirvOpCode.OpExtInst => "OpExtInst",
            SpirvOpCode.OpMemoryModel => "OpMemoryModel",
            SpirvOpCode.OpEntryPoint => "OpEntryPoint",
            SpirvOpCode.OpExecutionMode => "OpExecutionMode",
            SpirvOpCode.OpCapability => "OpCapability",
            SpirvOpCode.OpTypeVoid => "OpTypeVoid",
            SpirvOpCode.OpTypeBool => "OpTypeBool",
            SpirvOpCode.OpTypeInt => "OpTypeInt",
            SpirvOpCode.OpTypeFloat => "OpTypeFloat",
            SpirvOpCode.OpTypeVector => "OpTypeVector",
            SpirvOpCode.OpTypeMatrix => "OpTypeMatrix",
            SpirvOpCode.OpTypeImage => "OpTypeImage",
            SpirvOpCode.OpTypeSampler => "OpTypeSampler",
            SpirvOpCode.OpTypeSampledImage => "OpTypeSampledImage",
            SpirvOpCode.OpTypeArray => "OpTypeArray",
            SpirvOpCode.OpTypeRuntimeArray => "OpTypeRuntimeArray",
            SpirvOpCode.OpTypeStruct => "OpTypeStruct",
            SpirvOpCode.OpTypePointer => "OpTypePointer",
            SpirvOpCode.OpTypeFunction => "OpTypeFunction",
            SpirvOpCode.OpTypeForwardPointer => "OpTypeForwardPointer",
            SpirvOpCode.OpConstantTrue => "OpConstantTrue",
            SpirvOpCode.OpConstantFalse => "OpConstantFalse",
            SpirvOpCode.OpConstant => "OpConstant",
            SpirvOpCode.OpFunction => "OpFunction",
            SpirvOpCode.OpFunctionParameter => "OpFunctionParameter",
            SpirvOpCode.OpFunctionEnd => "OpFunctionEnd",
            SpirvOpCode.OpFunctionCall => "OpFunctionCall",
            SpirvOpCode.OpVariable => "OpVariable",
            SpirvOpCode.OpLoad => "OpLoad",
            SpirvOpCode.OpStore => "OpStore",
            SpirvOpCode.OpAccessChain => "OpAccessChain",
            SpirvOpCode.OpDecorate => "OpDecorate",
            SpirvOpCode.OpMemberDecorate => "OpMemberDecorate",
            SpirvOpCode.OpCompositeConstruct => "OpCompositeConstruct",
            SpirvOpCode.OpCompositeExtract => "OpCompositeExtract",
            SpirvOpCode.OpDot => "OpDot",
            SpirvOpCode.OpFAdd => "OpFAdd",
            SpirvOpCode.OpFSub => "OpFSub",
            SpirvOpCode.OpFMul => "OpFMul",
            SpirvOpCode.OpFDiv => "OpFDiv",
            SpirvOpCode.OpIAdd => "OpIAdd",
            SpirvOpCode.OpISub => "OpISub",
            SpirvOpCode.OpIMul => "OpIMul",
            SpirvOpCode.OpLabel => "OpLabel",
            SpirvOpCode.OpBranch => "OpBranch",
            SpirvOpCode.OpBranchConditional => "OpBranchConditional",
            SpirvOpCode.OpReturn => "OpReturn",
            SpirvOpCode.OpReturnValue => "OpReturnValue",
            SpirvOpCode.OpTypeAccelerationStructureKHR => "OpTypeAccelerationStructureKHR",
            _ => $"Op{opcode}"
        };
    }
}
