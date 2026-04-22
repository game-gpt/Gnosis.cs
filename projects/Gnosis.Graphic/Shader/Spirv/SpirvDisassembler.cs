using System.Text;

namespace Gnosis.Graphic.Shader.Spirv;

public sealed class SpirvDisassembler
{
    #region Fields

    private readonly Dictionary<uint, string> _idNames = new();
    private readonly StringBuilder _sb = new();

    #endregion

    #region Public Methods

    public string Disassemble(byte[] spirvBinary)
    {
        _sb.Clear();
        _idNames.Clear();

        if (spirvBinary.Length < 20)
        {
            return "; 无效的 SPIR-V 二进制（太短）";
        }

        var words = new uint[spirvBinary.Length / 4];
        Buffer.BlockCopy(spirvBinary, 0, words, 0, spirvBinary.Length);

        var magic = words[0];
        if (magic != SpirvConstants.MagicNumber)
        {
            return $"; 无效的 SPIR-V 魔数: 0x{magic:X8}";
        }

        var version = words[1];
        var generator = words[2];
        var bound = words[3];
        var schema = words[4];

        _sb.AppendLine($"; SPIR-V 版本: {version >> 16}.{(version >> 8) & 0xFF}");
        _sb.AppendLine($"; 生成器: 0x{generator:X8}");
        _sb.AppendLine($"; Bound: {bound}");
        _sb.AppendLine($"; Schema: {schema}");
        _sb.AppendLine();

        var index = 5;
        while (index < words.Length)
        {
            var word0 = words[index];
            var wordCount = (int)(word0 >> 16);
            var opCode = word0 & 0xFFFF;

            if (wordCount == 0)
            {
                _sb.AppendLine($"; 无效指令: wordCount=0, opCode={opCode}");
                break;
            }

            var operandStart = index + 1;
            var operandEnd = Math.Min(index + wordCount, words.Length);
            var operands = new uint[operandEnd - operandStart];
            Array.Copy(words, operandStart, operands, 0, operands.Length);

            DisassembleInstruction(opCode, wordCount, operands);

            index += wordCount;
        }

        return _sb.ToString();
    }

    #endregion

    #region Private Methods

    private void DisassembleInstruction(uint opCode, int wordCount, uint[] operands)
    {
        var name = GetOpCodeName(opCode);

        switch (opCode)
        {
            case SpirvConstants.Op.OpCapability:
                _sb.AppendLine($"{name} {GetCapabilityName(operands[0])}");
                break;

            case SpirvConstants.Op.OpExtension:
                _sb.AppendLine($"{name} \"{ExtractString(operands)}\"");
                break;

            case SpirvConstants.Op.OpExtInstImport:
                RegisterId(operands[0], $"ext_{operands[0]}");
                _sb.AppendLine($"{name} %{_idNames[operands[0]]} = \"{ExtractString(operands, 1)}\"");
                break;

            case SpirvConstants.Op.OpMemoryModel:
                _sb.AppendLine($"{name} {GetAddressingModelName(operands[0])} {GetMemoryModelName(operands[1])}");
                break;

            case SpirvConstants.Op.OpEntryPoint:
                var ifIds = operands.Length > 3 ? operands[3..] : [];
                _sb.AppendLine($"{name} {GetExecutionModelName(operands[0])} %{operands[1]} \"{ExtractString(operands, 2)}\" {FormatIds(ifIds)}");
                break;

            case SpirvConstants.Op.OpExecutionMode:
                _sb.AppendLine($"{name} %{operands[0]} {GetExecutionModeName(operands[1])}{FormatExtraOperands(operands, 2)}");
                break;

            case SpirvConstants.Op.OpSource:
                _sb.AppendLine($"{name} {GetSourceLanguageName(operands[0])} {operands[1]}");
                break;

            case SpirvConstants.Op.OpName:
                RegisterId(operands[0], ExtractString(operands, 1));
                break;

            case SpirvConstants.Op.OpMemberName:
                _sb.AppendLine($"{name} %{operands[0]} {operands[1]} \"{ExtractString(operands, 2)}\"");
                break;

            case SpirvConstants.Op.OpDecorate:
                _sb.AppendLine($"{name} %{operands[0]} {GetDecorationName(operands[1])}{FormatExtraOperands(operands, 2)}");
                break;

            case SpirvConstants.Op.OpMemberDecorate:
                _sb.AppendLine($"{name} %{operands[0]} {operands[1]} {GetDecorationName(operands[2])}{FormatExtraOperands(operands, 3)}");
                break;

            case SpirvConstants.Op.OpTypeVoid:
                RegisterId(operands[0], "void");
                _sb.AppendLine($"{name} %{_idNames[operands[0]]}");
                break;

            case SpirvConstants.Op.OpTypeBool:
                RegisterId(operands[0], "bool");
                _sb.AppendLine($"{name} %{_idNames[operands[0]]}");
                break;

            case SpirvConstants.Op.OpTypeInt:
                var signed = operands[2] != 0;
                RegisterId(operands[0], $"{(signed ? "i" : "u")}{operands[1]}");
                _sb.AppendLine($"{name} %{_idNames[operands[0]]} {operands[1]} {operands[2]}");
                break;

            case SpirvConstants.Op.OpTypeFloat:
                RegisterId(operands[0], $"f{operands[1]}");
                _sb.AppendLine($"{name} %{_idNames[operands[0]]} {operands[1]}");
                break;

            case SpirvConstants.Op.OpTypeVector:
                RegisterId(operands[0], $"vec_{operands[0]}");
                _sb.AppendLine($"{name} %{_idNames[operands[0]]} %{operands[1]} {operands[2]}");
                break;

            case SpirvConstants.Op.OpTypeMatrix:
                RegisterId(operands[0], $"mat_{operands[0]}");
                _sb.AppendLine($"{name} %{_idNames[operands[0]]} %{operands[1]} {operands[2]}");
                break;

            case SpirvConstants.Op.OpTypeStruct:
                RegisterId(operands[0], $"struct_{operands[0]}");
                var memberIds = operands.Length > 2 ? operands[2..] : [];
                _sb.AppendLine($"{name} %{_idNames[operands[0]]} {FormatIds(memberIds)}");
                break;

            case SpirvConstants.Op.OpTypePointer:
                RegisterId(operands[0], $"ptr_{operands[0]}");
                _sb.AppendLine($"{name} %{_idNames[operands[0]]} {GetStorageClassName(operands[1])} %{operands[2]}");
                break;

            case SpirvConstants.Op.OpTypeFunction:
                RegisterId(operands[0], $"fn_{operands[0]}");
                var paramIds = operands.Length > 2 ? operands[2..] : [];
                _sb.AppendLine($"{name} %{_idNames[operands[0]]} %{operands[1]} {FormatIds(paramIds)}");
                break;

            case SpirvConstants.Op.OpVariable:
                RegisterId(operands[1], $"var_{operands[1]}");
                var initStr = operands.Length > 3 ? $" %{operands[3]}" : "";
                _sb.AppendLine($"{name} %{_idNames[operands[1]]} {GetStorageClassName(operands[2])}{initStr}");
                break;

            case SpirvConstants.Op.OpFunction:
                RegisterId(operands[1], $"func_{operands[1]}");
                _sb.AppendLine($"{name} %{_idNames[operands[1]]} {operands[2]} %{operands[3]} %{operands[4]}");
                break;

            case SpirvConstants.Op.OpFunctionParameter:
                RegisterId(operands[1], $"param_{operands[1]}");
                _sb.AppendLine($"{name} %{_idNames[operands[1]]}");
                break;

            case SpirvConstants.Op.OpFunctionEnd:
                _sb.AppendLine(name);
                break;

            case SpirvConstants.Op.OpLabel:
                RegisterId(operands[0], $"label_{operands[0]}");
                _sb.AppendLine($"{name} %{_idNames[operands[0]]}");
                break;

            case SpirvConstants.Op.OpLoad:
                RegisterId(operands[1], $"load_{operands[1]}");
                _sb.AppendLine($"%{_idNames[operands[1]]} = {name} %{operands[0]} %{operands[2]}");
                break;

            case SpirvConstants.Op.OpStore:
                _sb.AppendLine($"{name} %{operands[0]} %{operands[1]}");
                break;

            case SpirvConstants.Op.OpAccessChain:
                RegisterId(operands[1], $"access_{operands[1]}");
                var idxIds = operands.Length > 3 ? operands[3..] : [];
                _sb.AppendLine($"%{_idNames[operands[1]]} = {name} %{operands[0]} %{operands[2]} {FormatIds(idxIds)}");
                break;

            case SpirvConstants.Op.OpCompositeConstruct:
                RegisterId(operands[1], $"comp_{operands[1]}");
                var constituentIds = operands.Length > 2 ? operands[2..] : [];
                _sb.AppendLine($"%{_idNames[operands[1]]} = {name} %{operands[0]} {FormatIds(constituentIds)}");
                break;

            case SpirvConstants.Op.OpCompositeExtract:
                RegisterId(operands[1], $"extract_{operands[1]}");
                var extractIndices = operands.Length > 3 ? operands[3..] : [];
                _sb.AppendLine($"%{_idNames[operands[1]]} = {name} %{operands[0]} %{operands[2]} {FormatIds(extractIndices)}");
                break;

            case SpirvConstants.Op.OpVectorShuffle:
                RegisterId(operands[1], $"shuffle_{operands[1]}");
                var compIds = operands.Length > 4 ? operands[4..] : [];
                _sb.AppendLine($"%{_idNames[operands[1]]} = {name} %{operands[0]} %{operands[2]} %{operands[3]} {FormatIds(compIds)}");
                break;

            default:
                if (operands.Length >= 2 && HasResultId(opCode))
                {
                    RegisterId(operands[1], $"id_{operands[1]}");
                    _sb.AppendLine($"%{_idNames[operands[1]]} = {name} {FormatOperands(operands)}");
                }
                else
                {
                    _sb.AppendLine($"{name} {FormatOperands(operands)}");
                }
                break;
        }
    }

    private void RegisterId(uint id, string name)
    {
        if (!_idNames.ContainsKey(id))
        {
            _idNames[id] = name;
        }
    }

    private string FormatIds(uint[] ids)
    {
        return string.Join(" ", ids.Select(id => $"%{(_idNames.TryGetValue(id, out var name) ? name : id)}"));
    }

    private string FormatOperands(uint[] operands)
    {
        return string.Join(" ", operands.Select(op => $"%{op}"));
    }

    private string FormatExtraOperands(uint[] operands, int start)
    {
        if (start >= operands.Length)
        {
            return "";
        }

        return " " + string.Join(" ", operands[start..]);
    }

    private string ExtractString(uint[] operands, int startWordIndex = 0)
    {
        var bytes = new List<byte>();

        for (var i = startWordIndex; i < operands.Length; i++)
        {
            var word = operands[i];
            bytes.Add((byte)(word & 0xFF));
            bytes.Add((byte)((word >> 8) & 0xFF));
            bytes.Add((byte)((word >> 16) & 0xFF));
            bytes.Add((byte)((word >> 24) & 0xFF));
        }

        var nullIndex = bytes.IndexOf(0);
        if (nullIndex >= 0)
        {
            bytes = bytes.Take(nullIndex).ToList();
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private static bool HasResultId(uint opCode)
    {
        return opCode is SpirvConstants.Op.OpTypeVoid or SpirvConstants.Op.OpTypeBool
            or SpirvConstants.Op.OpTypeInt or SpirvConstants.Op.OpTypeFloat
            or SpirvConstants.Op.OpTypeVector or SpirvConstants.Op.OpTypeMatrix
            or SpirvConstants.Op.OpTypeStruct or SpirvConstants.Op.OpTypePointer
            or SpirvConstants.Op.OpTypeFunction or SpirvConstants.Op.OpTypeImage
            or SpirvConstants.Op.OpTypeSampler or SpirvConstants.Op.OpTypeSampledImage
            or SpirvConstants.Op.OpVariable or SpirvConstants.Op.OpFunction
            or SpirvConstants.Op.OpFunctionParameter or SpirvConstants.Op.OpLabel
            or SpirvConstants.Op.OpLoad or SpirvConstants.Op.OpAccessChain
            or SpirvConstants.Op.OpCompositeConstruct or SpirvConstants.Op.OpCompositeExtract
            or SpirvConstants.Op.OpVectorShuffle or SpirvConstants.Op.OpConstant
            or SpirvConstants.Op.OpSpecConstant or SpirvConstants.Op.OpExtInst
            or SpirvConstants.Op.OpSampledImage or SpirvConstants.Op.OpImageSampleImplicitLod
            or SpirvConstants.Op.OpFunctionCall or SpirvConstants.Op.OpConvertFToS
            or SpirvConstants.Op.OpConvertSToF or SpirvConstants.Op.OpTypeAccelerationStructureKHR;
    }

    #endregion

    #region Name Resolution

    private static string GetOpCodeName(uint opCode) => opCode switch
    {
        SpirvConstants.Op.OpNop => "OpNop",
        SpirvConstants.Op.OpCapability => "OpCapability",
        SpirvConstants.Op.OpExtension => "OpExtension",
        SpirvConstants.Op.OpExtInstImport => "OpExtInstImport",
        SpirvConstants.Op.OpMemoryModel => "OpMemoryModel",
        SpirvConstants.Op.OpEntryPoint => "OpEntryPoint",
        SpirvConstants.Op.OpExecutionMode => "OpExecutionMode",
        SpirvConstants.Op.OpSource => "OpSource",
        SpirvConstants.Op.OpName => "OpName",
        SpirvConstants.Op.OpMemberName => "OpMemberName",
        SpirvConstants.Op.OpDecorate => "OpDecorate",
        SpirvConstants.Op.OpMemberDecorate => "OpMemberDecorate",
        SpirvConstants.Op.OpTypeVoid => "OpTypeVoid",
        SpirvConstants.Op.OpTypeBool => "OpTypeBool",
        SpirvConstants.Op.OpTypeInt => "OpTypeInt",
        SpirvConstants.Op.OpTypeFloat => "OpTypeFloat",
        SpirvConstants.Op.OpTypeVector => "OpTypeVector",
        SpirvConstants.Op.OpTypeMatrix => "OpTypeMatrix",
        SpirvConstants.Op.OpTypeStruct => "OpTypeStruct",
        SpirvConstants.Op.OpTypePointer => "OpTypePointer",
        SpirvConstants.Op.OpTypeFunction => "OpTypeFunction",
        SpirvConstants.Op.OpTypeImage => "OpTypeImage",
        SpirvConstants.Op.OpTypeSampler => "OpTypeSampler",
        SpirvConstants.Op.OpTypeSampledImage => "OpTypeSampledImage",
        SpirvConstants.Op.OpTypeAccelerationStructureKHR => "OpTypeAccelerationStructureKHR",
        SpirvConstants.Op.OpConstant => "OpConstant",
        SpirvConstants.Op.OpSpecConstant => "OpSpecConstant",
        SpirvConstants.Op.OpVariable => "OpVariable",
        SpirvConstants.Op.OpFunction => "OpFunction",
        SpirvConstants.Op.OpFunctionParameter => "OpFunctionParameter",
        SpirvConstants.Op.OpFunctionEnd => "OpFunctionEnd",
        SpirvConstants.Op.OpFunctionCall => "OpFunctionCall",
        SpirvConstants.Op.OpLabel => "OpLabel",
        SpirvConstants.Op.OpLoad => "OpLoad",
        SpirvConstants.Op.OpStore => "OpStore",
        SpirvConstants.Op.OpAccessChain => "OpAccessChain",
        SpirvConstants.Op.OpCompositeConstruct => "OpCompositeConstruct",
        SpirvConstants.Op.OpCompositeExtract => "OpCompositeExtract",
        SpirvConstants.Op.OpVectorShuffle => "OpVectorShuffle",
        SpirvConstants.Op.OpBranch => "OpBranch",
        SpirvConstants.Op.OpBranchConditional => "OpBranchConditional",
        SpirvConstants.Op.OpReturn => "OpReturn",
        SpirvConstants.Op.OpReturnValue => "OpReturnValue",
        SpirvConstants.Op.OpKill => "OpKill",
        SpirvConstants.Op.OpSelectionMerge => "OpSelectionMerge",
        SpirvConstants.Op.OpLoopMerge => "OpLoopMerge",
        SpirvConstants.Op.OpExtInst => "OpExtInst",
        SpirvConstants.Op.OpSampledImage => "OpSampledImage",
        SpirvConstants.Op.OpImageSampleImplicitLod => "OpImageSampleImplicitLod",
        SpirvConstants.Op.OpImageFetch => "OpImageFetch",
        SpirvConstants.Op.OpConvertFToS => "OpConvertFToS",
        SpirvConstants.Op.OpConvertSToF => "OpConvertSToF",
        SpirvConstants.Op.OpIAdd => "OpIAdd",
        SpirvConstants.Op.OpISub => "OpISub",
        SpirvConstants.Op.OpIMul => "OpIMul",
        SpirvConstants.Op.OpSDiv => "OpSDiv",
        SpirvConstants.Op.OpFAdd => "OpFAdd",
        SpirvConstants.Op.OpFSub => "OpFSub",
        SpirvConstants.Op.OpFMul => "OpFMul",
        SpirvConstants.Op.OpFDiv => "OpFDiv",
        SpirvConstants.Op.OpFNegate => "OpFNegate",
        SpirvConstants.Op.OpSNegate => "OpSNegate",
        SpirvConstants.Op.OpIEqual => "OpIEqual",
        SpirvConstants.Op.OpINotEqual => "OpINotEqual",
        SpirvConstants.Op.OpSLessThan => "OpSLessThan",
        SpirvConstants.Op.OpSGreaterThan => "OpSGreaterThan",
        SpirvConstants.Op.OpSLessEqual => "OpSLessEqual",
        SpirvConstants.Op.OpSGreaterEqual => "OpSGreaterEqual",
        SpirvConstants.Op.OpFOrdEqual => "OpFOrdEqual",
        SpirvConstants.Op.OpFOrdNotEqual => "OpFOrdNotEqual",
        SpirvConstants.Op.OpFOrdLessThan => "OpFOrdLessThan",
        SpirvConstants.Op.OpFOrdGreaterThan => "OpFOrdGreaterThan",
        SpirvConstants.Op.OpFOrdLessEqual => "OpFOrdLessEqual",
        SpirvConstants.Op.OpFOrdGreaterEqual => "OpFOrdGreaterEqual",
        SpirvConstants.Op.OpLogicalAnd => "OpLogicalAnd",
        SpirvConstants.Op.OpLogicalOr => "OpLogicalOr",
        SpirvConstants.Op.OpLogicalNot => "OpLogicalNot",
        SpirvConstants.Op.OpDot => "OpDot",
        SpirvConstants.Op.OpMatrixTimesVector => "OpMatrixTimesVector",
        _ => $"Op{opCode}"
    };

    private static string GetCapabilityName(uint capability) => capability switch
    {
        SpirvConstants.Capability.Shader => "Shader",
        SpirvConstants.Capability.RayTracingKHR => "RayTracingKHR",
        _ => $"Capability_{capability}"
    };

    private static string GetAddressingModelName(uint model) => model switch
    {
        SpirvConstants.AddressingModel.Logical => "Logical",
        _ => $"AddressingModel_{model}"
    };

    private static string GetMemoryModelName(uint model) => model switch
    {
        SpirvConstants.MemoryModel.GLSL450 => "GLSL450",
        _ => $"MemoryModel_{model}"
    };

    private static string GetExecutionModelName(uint model) => model switch
    {
        SpirvConstants.ExecutionModel.Vertex => "Vertex",
        SpirvConstants.ExecutionModel.Fragment => "Fragment",
        SpirvConstants.ExecutionModel.GLCompute => "GLCompute",
        SpirvConstants.ExecutionModel.RayGenerationKHR => "RayGenerationKHR",
        _ => $"ExecutionModel_{model}"
    };

    private static string GetExecutionModeName(uint mode) => mode switch
    {
        SpirvConstants.ExecutionMode.OriginUpperLeft => "OriginUpperLeft",
        SpirvConstants.ExecutionMode.LocalSize => "LocalSize",
        _ => $"ExecutionMode_{mode}"
    };

    private static string GetSourceLanguageName(uint lang) => lang switch
    {
        SpirvConstants.SourceLanguage.GLSL => "GLSL",
        _ => $"SourceLanguage_{lang}"
    };

    private static string GetDecorationName(uint decoration) => decoration switch
    {
        SpirvConstants.Decoration.Location => "Location",
        SpirvConstants.Decoration.BuiltIn => "BuiltIn",
        SpirvConstants.Decoration.DescriptorSet => "DescriptorSet",
        SpirvConstants.Decoration.Binding => "Binding",
        SpirvConstants.Decoration.Offset => "Offset",
        SpirvConstants.Decoration.Block => "Block",
        SpirvConstants.Decoration.BufferBlock => "BufferBlock",
        SpirvConstants.Decoration.NonWritable => "NonWritable",
        SpirvConstants.Decoration.NonReadable => "NonReadable",
        _ => $"Decoration_{decoration}"
    };

    private static string GetStorageClassName(uint storageClass) => storageClass switch
    {
        SpirvConstants.StorageClass.UniformConstant => "UniformConstant",
        SpirvConstants.StorageClass.Input => "Input",
        SpirvConstants.StorageClass.Uniform => "Uniform",
        SpirvConstants.StorageClass.Output => "Output",
        SpirvConstants.StorageClass.Function => "Function",
        SpirvConstants.StorageClass.PushConstant => "PushConstant",
        SpirvConstants.StorageClass.StorageBuffer => "StorageBuffer",
        _ => $"StorageClass_{storageClass}"
    };

    #endregion
}
