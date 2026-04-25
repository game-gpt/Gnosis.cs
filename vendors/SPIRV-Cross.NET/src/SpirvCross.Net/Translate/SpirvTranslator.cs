using Acorn.Spirv.Decode;
using SpirvCross.Net.Model;
using SpirvModuleData = Acorn.Spirv.Data.SpirvModuleData;
using AcornInstruction = Acorn.Spirv.Data.SpirvInstruction;
using SpirvOpCode = Acorn.Spirv.Data.SpirvOpCode;
using SpirvCapability = Acorn.Spirv.Data.SpirvCapability;
using SpirvExecutionModel = Acorn.Spirv.Data.SpirvExecutionModel;
using SpirvStorageClass = Acorn.Spirv.Data.SpirvStorageClass;
using SpirvDecoration = Acorn.Spirv.Data.SpirvDecoration;
using SpirvImageDim = Acorn.Spirv.Data.SpirvImageDim;

namespace SpirvCross.Net.Translate;

/// <summary>
///     SPIR-V 翻译器，将 SPIR-V 二进制字节码转换为语义模型并翻译为目标格式。
/// </summary>
/// <remarks>
///     翻译器分两步工作：
///     1. 解码：使用 Acorn.SpirV 解码 SPIR-V 二进制，构建 SpirvModule 语义模型
///     2. 翻译：将语义模型传递给目标格式生成器，产出目标代码
/// </remarks>
public sealed class SpirvTranslator
{
    private readonly SpirvModule _module;

    /// <summary>
    ///     从 SPIR-V 字节码创建翻译器。
    /// </summary>
    /// <param name="spirvBytecode">SPIR-V 二进制字节码。</param>
    public SpirvTranslator(byte[] spirvBytecode)
    {
        var decoder = new SpirvDecoder(spirvBytecode);
        var spirvData = decoder.Decode();
        _module = BuildModel(spirvData);
    }

    /// <summary>
    ///     从 SpirvModule 创建翻译器。
    /// </summary>
    /// <param name="module">预构建的 SPIR-V 语义模型。</param>
    public SpirvTranslator(SpirvModule module)
    {
        _module = module;
    }

    /// <summary>
    ///     获取 SPIR-V 语义模型。
    /// </summary>
    public SpirvModule Module => _module;

    /// <summary>
    ///     将 SPIR-V 翻译为目标格式。
    /// </summary>
    /// <param name="target">目标格式生成器。</param>
    /// <returns>目标格式代码字符串。</returns>
    public string Translate(Target.IShaderTarget target)
    {
        return target.Generate(_module);
    }

    /// <summary>
    ///     将 SPIR-V 翻译为目标格式并写入文件。
    /// </summary>
    /// <param name="target">目标格式生成器。</param>
    /// <param name="outputPath">输出文件路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task TranslateToFileAsync(Target.IShaderTarget target, string outputPath,
        CancellationToken cancellationToken = default)
    {
        var code = Translate(target);
        await File.WriteAllTextAsync(outputPath, code, cancellationToken);
    }

    private static SpirvModule BuildModel(SpirvModuleData data)
    {
        var module = new SpirvModule
        {
            Version = data.Version,
            GeneratorMagic = data.GeneratorMagic
        };

        foreach (var instruction in data.Instructions)
        {
            ProcessInstruction(module, instruction);
        }

        return module;
    }

    private static void ProcessInstruction(SpirvModule module, AcornInstruction instruction)
    {
        switch (instruction.Opcode)
        {
            case SpirvOpCode.OpCapability:
                module.Capabilities.Add((SpirvCapability)instruction.Operands[0]);
                break;

            case SpirvOpCode.OpExtension:
                break;

            case SpirvOpCode.OpEntryPoint:
                ProcessEntryPoint(module, instruction);
                break;

            case SpirvOpCode.OpName:
                module.Names[instruction.Operands[0]] = DecodeString(instruction.Operands, 1);
                break;

            case SpirvOpCode.OpTypeVoid:
                module.Types[instruction.Operands[0]] = new SpirvVoidType
                {
                    ResultId = instruction.Operands[0]
                };
                break;

            case SpirvOpCode.OpTypeBool:
                module.Types[instruction.Operands[0]] = new SpirvBoolType
                {
                    ResultId = instruction.Operands[0]
                };
                break;

            case SpirvOpCode.OpTypeInt:
                module.Types[instruction.Operands[0]] = new SpirvIntType
                {
                    ResultId = instruction.Operands[0],
                    BitWidth = instruction.Operands[1],
                    IsSigned = instruction.Operands[2] != 0
                };
                break;

            case SpirvOpCode.OpTypeFloat:
                module.Types[instruction.Operands[0]] = new SpirvFloatType
                {
                    ResultId = instruction.Operands[0],
                    BitWidth = instruction.Operands[1]
                };
                break;

            case SpirvOpCode.OpTypeVector:
                module.Types[instruction.Operands[0]] = new SpirvVectorType
                {
                    ResultId = instruction.Operands[0],
                    ComponentTypeId = instruction.Operands[1],
                    ComponentCount = instruction.Operands[2]
                };
                break;

            case SpirvOpCode.OpTypeMatrix:
                module.Types[instruction.Operands[0]] = new SpirvMatrixType
                {
                    ResultId = instruction.Operands[0],
                    ColumnTypeId = instruction.Operands[1],
                    ColumnCount = instruction.Operands[2]
                };
                break;

            case SpirvOpCode.OpTypeArray:
                module.Types[instruction.Operands[0]] = new SpirvArrayType
                {
                    ResultId = instruction.Operands[0],
                    ElementTypeId = instruction.Operands[1],
                    ElementCount = instruction.Operands[2]
                };
                break;

            case SpirvOpCode.OpTypeStruct:
                ProcessStructType(module, instruction);
                break;

            case SpirvOpCode.OpTypePointer:
                module.Types[instruction.Operands[0]] = new SpirvPointerType
                {
                    ResultId = instruction.Operands[0],
                    StorageClass = (SpirvStorageClass)instruction.Operands[1],
                    PointeeTypeId = instruction.Operands[2]
                };
                break;

            case SpirvOpCode.OpTypeFunction:
                ProcessFunctionType(module, instruction);
                break;

            case SpirvOpCode.OpTypeImage:
                ProcessImageType(module, instruction);
                break;

            case SpirvOpCode.OpTypeSampler:
                module.Types[instruction.Operands[0]] = new SpirvSamplerType
                {
                    ResultId = instruction.Operands[0]
                };
                break;

            case SpirvOpCode.OpTypeSampledImage:
                module.Types[instruction.Operands[0]] = new SpirvSampledImageType
                {
                    ResultId = instruction.Operands[0],
                    ImageTypeId = instruction.Operands[1]
                };
                break;

            case SpirvOpCode.OpVariable:
                ProcessVariable(module, instruction);
                break;

            case SpirvOpCode.OpDecorate:
                ProcessDecorate(module, instruction);
                break;

            case SpirvOpCode.OpMemberDecorate:
                ProcessMemberDecorate(module, instruction);
                break;

            case SpirvOpCode.OpFunction:
                ProcessFunction(module, instruction);
                break;
        }
    }

    private static void ProcessEntryPoint(SpirvModule module, AcornInstruction instruction)
    {
        var entryPoint = new SpirvEntryPoint
        {
            ExecutionModel = (SpirvExecutionModel)instruction.Operands[0],
            FunctionId = instruction.Operands[1],
            Name = DecodeString(instruction.Operands, 2)
        };

        module.EntryPoints.Add(entryPoint);
    }

    private static void ProcessStructType(SpirvModule module, AcornInstruction instruction)
    {
        var resultId = instruction.Operands[0];
        var fields = new List<SpirvStructField>();

        for (var i = 1; i < instruction.Operands.Count; i++)
        {
            fields.Add(new SpirvStructField
            {
                TypeId = instruction.Operands[i]
            });
        }

        module.Types[resultId] = new SpirvStructType
        {
            ResultId = resultId,
            Fields = fields
        };
    }

    private static void ProcessFunctionType(SpirvModule module, AcornInstruction instruction)
    {
        var resultId = instruction.Operands[0];
        var returnTypeId = instruction.Operands[1];
        var paramTypeIds = new List<uint>();

        for (var i = 2; i < instruction.Operands.Count; i++)
        {
            paramTypeIds.Add(instruction.Operands[i]);
        }

        module.Types[resultId] = new SpirvFunctionType
        {
            ResultId = resultId,
            ReturnTypeId = returnTypeId,
            ParameterTypeIds = paramTypeIds
        };
    }

    private static void ProcessImageType(SpirvModule module, AcornInstruction instruction)
    {
        module.Types[instruction.Operands[0]] = new SpirvImageType
        {
            ResultId = instruction.Operands[0],
            SampledTypeId = instruction.Operands[1],
            Dim = (SpirvImageDim)instruction.Operands[2],
            IsDepth = instruction.Operands[3] != 0,
            IsArrayed = instruction.Operands[4] != 0,
            IsMultisampled = instruction.Operands[5] != 0,
            Sampled = instruction.Operands[6]
        };
    }

    private static void ProcessVariable(SpirvModule module, AcornInstruction instruction)
    {
        var resultTypeId = instruction.Operands[0];
        var resultId = instruction.Operands[1];
        var storageClass = (SpirvStorageClass)instruction.Operands[2];
        uint? initializerId = instruction.Operands.Count > 3
            ? instruction.Operands[3]
            : null;

        module.GlobalVariables[resultId] = new SpirvVariable
        {
            ResultId = resultId,
            TypeId = resultTypeId,
            StorageClass = storageClass,
            InitializerId = initializerId
        };
    }

    private static void ProcessDecorate(SpirvModule module, AcornInstruction instruction)
    {
        var targetId = instruction.Operands[0];
        var decoration = (SpirvDecoration)instruction.Operands[1];
        var extraOperands = instruction.Operands.Skip(2).ToList();

        if (!module.Decorations.ContainsKey(targetId))
        {
            module.Decorations[targetId] = [];
        }

        module.Decorations[targetId].Add(new SpirvDecorationInfo
        {
            TargetId = targetId,
            Decoration = decoration,
            ExtraOperands = extraOperands
        });
    }

    private static void ProcessMemberDecorate(SpirvModule module, AcornInstruction instruction)
    {
        var targetId = instruction.Operands[0];
        var memberIndex = instruction.Operands[1];
        var decoration = (SpirvDecoration)instruction.Operands[2];
        var extraOperands = instruction.Operands.Skip(3).ToList();

        if (!module.Decorations.ContainsKey(targetId))
        {
            module.Decorations[targetId] = [];
        }

        module.Decorations[targetId].Add(new SpirvDecorationInfo
        {
            TargetId = targetId,
            Decoration = decoration,
            ExtraOperands = [memberIndex, ..extraOperands]
        });
    }

    private static void ProcessFunction(SpirvModule module, AcornInstruction instruction)
    {
        var resultTypeId = instruction.Operands[0];
        var resultId = instruction.Operands[1];
        var functionTypeId = instruction.Operands[3];

        module.Functions[resultId] = new SpirvFunction
        {
            ResultId = resultId,
            ReturnTypeId = resultTypeId,
            FunctionTypeId = functionTypeId
        };
    }

    private static string DecodeString(IReadOnlyList<uint> operands, int startIndex)
    {
        var bytes = new List<byte>();
        var i = startIndex;

        while (i < operands.Count)
        {
            var word = operands[i++];
            bytes.Add((byte)(word & 0xFF));
            bytes.Add((byte)((word >> 8) & 0xFF));
            bytes.Add((byte)((word >> 16) & 0xFF));
            bytes.Add((byte)((word >> 24) & 0xFF));

            if (bytes.Any(b => b == 0))
            {
                break;
            }
        }

        var nullIndex = bytes.IndexOf((byte)0);
        if (nullIndex >= 0)
        {
            bytes = bytes.Take(nullIndex).ToList();
        }

        return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }
}
