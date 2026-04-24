using Acorn.Spirv.Data;
using Acorn.Spirv.Decode;

namespace Gnosis.Graphic.Shader.Spirv;

/// <summary>
///     SPIR-V 验证器，验证 SPIR-V 二进制数据的正确性。
/// </summary>
/// <remarks>
///     本验证器使用 Acorn.Spirv 的 <see cref="SpirvDecoder" /> 进行二进制解码，
///     遵循架构规则：二进制编解码职责由 Acorn 独占。
/// </remarks>
public sealed class SpirvValidator
{
    /// <summary>
    ///     验证 SPIR-V 二进制数据。
    /// </summary>
    /// <param name="data">SPIR-V 二进制数据。</param>
    /// <returns>验证结果。</returns>
    public SpirvValidationResult Validate(byte[] data)
    {
        var errors = new List<string>();

        if (!ValidateHeader(data, errors))
        {
            return new SpirvValidationResult(false, errors);
        }

        try
        {
            var module = new SpirvDecoder(data).Decode();

            ValidateStructure(module, errors);
        }
        catch (InvalidDataException ex)
        {
            errors.Add(ex.Message);
        }

        return new SpirvValidationResult(errors.Count == 0, errors);
    }

    private static bool ValidateHeader(byte[] data, List<string> errors)
    {
        if (data.Length < 20)
        {
            errors.Add("SPIR-V 文件数据过短，无法读取文件头");
            return false;
        }

        var magic = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0, 4));

        if (magic != SpirvConstants.MagicNumber)
        {
            errors.Add($"SPIR-V 文件魔数不匹配，期望 0x07230203，实际 0x{magic:X8}");
            return false;
        }

        var version = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4, 4));
        var major = (version >> 16) & 0xFF;
        var minor = (version >> 8) & 0xFF;

        if (major != 1 || minor > 6)
        {
            errors.Add($"不支持的 SPIR-V 版本：{major}.{minor}");
        }

        return errors.Count == 0;
    }

    private static void ValidateStructure(SpirvModuleData module, List<string> errors)
    {
        var hasCapability = false;
        var hasMemoryModel = false;
        var declaredIds = new HashSet<uint>();

        foreach (var instruction in module.Instructions)
        {
            if (instruction.Opcode == SpirvOpCode.OpCapability)
            {
                hasCapability = true;
            }

            if (instruction.Opcode == SpirvOpCode.OpMemoryModel)
            {
                hasMemoryModel = true;
            }

            if (instruction.Operands.Count > 1)
            {
                var possibleResultId = instruction.Operands[0];

                if (IsResultProducingInstruction(instruction.Opcode))
                {
                    if (!declaredIds.Add(possibleResultId))
                    {
                        errors.Add($"ID %{possibleResultId} 被重复声明");
                    }
                }
            }
        }

        if (!hasCapability)
        {
            errors.Add("缺少 OpCapability 指令");
        }

        if (!hasMemoryModel)
        {
            errors.Add("缺少 OpMemoryModel 指令");
        }
    }

    private static bool IsResultProducingInstruction(SpirvOpCode opcode)
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
///     SPIR-V 验证结果。
/// </summary>
public sealed class SpirvValidationResult
{
    /// <summary>
    ///     是否验证通过。
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    ///     验证错误列表。
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>
    ///     初始化 <see cref="SpirvValidationResult" /> 的新实例。
    /// </summary>
    public SpirvValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }
}
