using System.Text;
using Acorn.Spirv.Data;
using Acorn.Spirv.Encode;

namespace Gnosis.Graphic.Shader.Spirv;

/// <summary>
///     SPIR-V 指令构建器，基于 Acorn.Spirv 数据类型增量构建 SPIR-V 二进制模块。
/// </summary>
/// <remarks>
///     本构建器使用 Acorn.Spirv 的 <see cref="SpirvInstruction" /> 和 <see cref="SpirvEncoder" /> 进行二进制编码，
///     遵循架构规则：二进制编解码职责由 Acorn 独占。
/// </remarks>
public sealed class SpirvBuilder
{
    private readonly List<SpirvInstruction> _instructions = [];
    private uint _nextId = 1;

    /// <summary>
    ///     当前分配的 ID 数量（Bound 值）。
    /// </summary>
    public uint Bound => _nextId;

    /// <summary>
    ///     分配一个新的结果 ID。
    /// </summary>
    /// <returns>新分配的 ID。</returns>
    public uint AllocateId()
    {
        return _nextId++;
    }

    /// <summary>
    ///     添加 Capability 声明。
    /// </summary>
    /// <param name="capability">能力声明值。</param>
    public void AddCapability(uint capability)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpCapability,
            WordCount = 2,
            Operands = [capability]
        });
    }

    /// <summary>
    ///     设置内存模型。
    /// </summary>
    /// <param name="addressingModel">寻址模型。</param>
    /// <param name="memoryModel">内存模型。</param>
    public void SetMemoryModel(uint addressingModel, uint memoryModel)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpMemoryModel,
            WordCount = 3,
            Operands = [addressingModel, memoryModel]
        });
    }

    /// <summary>
    ///     添加入口点声明。
    /// </summary>
    /// <param name="executionModel">执行模型。</param>
    /// <param name="entryPointId">入口点函数 ID。</param>
    /// <param name="name">入口点名称。</param>
    /// <param name="interfaceIds">接口 ID 列表。</param>
    public void AddEntryPoint(uint executionModel, uint entryPointId, string name, uint[]? interfaceIds = null)
    {
        var nameWords = StringToWords(name);
        var operands = new List<uint> { executionModel, entryPointId };
        operands.AddRange(nameWords);

        if (interfaceIds is not null)
        {
            operands.AddRange(interfaceIds);
        }

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpEntryPoint,
            WordCount = (ushort)(1 + operands.Count),
            Operands = operands.ToArray()
        });
    }

    /// <summary>
    ///     添加执行模式。
    /// </summary>
    /// <param name="entryPointId">入口点 ID。</param>
    /// <param name="executionMode">执行模式。</param>
    /// <param name="extraOperands">额外操作数。</param>
    public void AddExecutionMode(uint entryPointId, uint executionMode, uint[]? extraOperands = null)
    {
        var operands = new List<uint> { entryPointId, executionMode };

        if (extraOperands is not null)
        {
            operands.AddRange(extraOperands);
        }

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpExecutionMode,
            WordCount = (ushort)(1 + operands.Count),
            Operands = operands.ToArray()
        });
    }

    /// <summary>
    ///     添加扩展指令导入。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="name">扩展指令集名称。</param>
    public void AddExtInstImport(uint resultId, string name)
    {
        var nameWords = StringToWords(name);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpExtInstImport,
            WordCount = (ushort)(2 + nameWords.Length),
            Operands = [resultId, ..nameWords]
        });
    }

    /// <summary>
    ///     声明 void 类型。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    public void DeclareVoidType(uint resultId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeVoid,
            WordCount = 2,
            Operands = [resultId]
        });
    }

    /// <summary>
    ///     声明 bool 类型。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    public void DeclareBoolType(uint resultId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeBool,
            WordCount = 2,
            Operands = [resultId]
        });
    }

    /// <summary>
    ///     声明整数类型。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="bitWidth">位宽。</param>
    /// <param name="isSigned">是否有符号。</param>
    public void DeclareIntType(uint resultId, uint bitWidth, bool isSigned)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeInt,
            WordCount = 4,
            Operands = [resultId, bitWidth, isSigned ? 1u : 0u]
        });
    }

    /// <summary>
    ///     声明浮点类型。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="bitWidth">位宽。</param>
    public void DeclareFloatType(uint resultId, uint bitWidth)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeFloat,
            WordCount = 3,
            Operands = [resultId, bitWidth]
        });
    }

    /// <summary>
    ///     声明向量类型。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="componentTypeId">分量类型 ID。</param>
    /// <param name="componentCount">分量数量。</param>
    public void DeclareVectorType(uint resultId, uint componentTypeId, uint componentCount)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeVector,
            WordCount = 4,
            Operands = [resultId, componentTypeId, componentCount]
        });
    }

    /// <summary>
    ///     声明矩阵类型。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="columnTypeId">列类型 ID。</param>
    /// <param name="columnCount">列数量。</param>
    public void DeclareMatrixType(uint resultId, uint columnTypeId, uint columnCount)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeMatrix,
            WordCount = 4,
            Operands = [resultId, columnTypeId, columnCount]
        });
    }

    /// <summary>
    ///     声明数组类型。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="elementTypeId">元素类型 ID。</param>
    /// <param name="lengthId">长度 ID。</param>
    public void DeclareArrayType(uint resultId, uint elementTypeId, uint lengthId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeArray,
            WordCount = 4,
            Operands = [resultId, elementTypeId, lengthId]
        });
    }

    /// <summary>
    ///     声明运行时数组类型。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="elementTypeId">元素类型 ID。</param>
    public void DeclareRuntimeArrayType(uint resultId, uint elementTypeId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeRuntimeArray,
            WordCount = 3,
            Operands = [resultId, elementTypeId]
        });
    }

    /// <summary>
    ///     声明结构体类型。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="memberTypeIds">成员类型 ID 列表。</param>
    public void DeclareStructType(uint resultId, uint[] memberTypeIds)
    {
        var operands = new uint[1 + memberTypeIds.Length];
        operands[0] = resultId;
        Array.Copy(memberTypeIds, 0, operands, 1, memberTypeIds.Length);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeStruct,
            WordCount = (ushort)(2 + memberTypeIds.Length),
            Operands = operands
        });
    }

    /// <summary>
    ///     声明指针类型。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="storageClass">存储类。</param>
    /// <param name="typeId">指向的类型 ID。</param>
    public void DeclarePointerType(uint resultId, uint storageClass, uint typeId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypePointer,
            WordCount = 4,
            Operands = [resultId, storageClass, typeId]
        });
    }

    /// <summary>
    ///     声明函数类型。
    /// </summary>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="returnTypeId">返回类型 ID。</param>
    /// <param name="parameterTypeIds">参数类型 ID 列表。</param>
    public void DeclareFunctionType(uint resultId, uint returnTypeId, uint[]? parameterTypeIds = null)
    {
        var operands = new List<uint> { resultId, returnTypeId };

        if (parameterTypeIds is not null)
        {
            operands.AddRange(parameterTypeIds);
        }

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeFunction,
            WordCount = (ushort)(1 + operands.Count),
            Operands = operands.ToArray()
        });
    }

    /// <summary>
    ///     声明图像类型。
    /// </summary>
    public void DeclareImageType(uint resultId, uint sampledTypeId, uint dim, uint depth, uint arrayed, uint ms, uint sampled, uint imageFormat)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeImage,
            WordCount = 9,
            Operands = [resultId, sampledTypeId, dim, depth, arrayed, ms, sampled, imageFormat]
        });
    }

    /// <summary>
    ///     声明采样器类型。
    /// </summary>
    public void DeclareSamplerType(uint resultId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeSampler,
            WordCount = 2,
            Operands = [resultId]
        });
    }

    /// <summary>
    ///     声明采样图像类型。
    /// </summary>
    public void DeclareSampledImageType(uint resultId, uint imageTypeId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeSampledImage,
            WordCount = 3,
            Operands = [resultId, imageTypeId]
        });
    }

    /// <summary>
    ///     声明加速结构类型（光线追踪）。
    /// </summary>
    public void DeclareAccelerationStructureType(uint resultId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpTypeAccelerationStructureKHR,
            WordCount = 2,
            Operands = [resultId]
        });
    }

    /// <summary>
    ///     添加装饰。
    /// </summary>
    /// <param name="targetId">目标 ID。</param>
    /// <param name="decoration">装饰类型。</param>
    /// <param name="extraOperands">额外操作数。</param>
    public void AddDecorate(uint targetId, uint decoration, uint[]? extraOperands = null)
    {
        var operands = new List<uint> { targetId, decoration };

        if (extraOperands is not null)
        {
            operands.AddRange(extraOperands);
        }

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpDecorate,
            WordCount = (ushort)(1 + operands.Count),
            Operands = operands.ToArray()
        });
    }

    /// <summary>
    ///     添加成员装饰。
    /// </summary>
    /// <param name="structTypeId">结构体类型 ID。</param>
    /// <param name="memberIndex">成员索引。</param>
    /// <param name="decoration">装饰类型。</param>
    /// <param name="extraOperands">额外操作数。</param>
    public void AddMemberDecorate(uint structTypeId, uint memberIndex, uint decoration, uint[]? extraOperands = null)
    {
        var operands = new List<uint> { structTypeId, memberIndex, decoration };

        if (extraOperands is not null)
        {
            operands.AddRange(extraOperands);
        }

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpMemberDecorate,
            WordCount = (ushort)(1 + operands.Count),
            Operands = operands.ToArray()
        });
    }

    /// <summary>
    ///     添加名称声明。
    /// </summary>
    /// <param name="targetId">目标 ID。</param>
    /// <param name="name">名称。</param>
    public void AddName(uint targetId, string name)
    {
        var nameWords = StringToWords(name);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpName,
            WordCount = (ushort)(2 + nameWords.Length),
            Operands = [targetId, ..nameWords]
        });
    }

    /// <summary>
    ///     添加成员名称声明。
    /// </summary>
    /// <param name="structTypeId">结构体类型 ID。</param>
    /// <param name="memberIndex">成员索引。</param>
    /// <param name="name">名称。</param>
    public void AddMemberName(uint structTypeId, uint memberIndex, string name)
    {
        var nameWords = StringToWords(name);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpMemberName,
            WordCount = (ushort)(3 + nameWords.Length),
            Operands = [structTypeId, memberIndex, ..nameWords]
        });
    }

    /// <summary>
    ///     添加源语言声明。
    /// </summary>
    public void AddSource(uint sourceLanguage, uint version)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpSource,
            WordCount = 3,
            Operands = [sourceLanguage, version]
        });
    }

    /// <summary>
    ///     添加扩展声明。
    /// </summary>
    public void AddExtension(string name)
    {
        var nameWords = StringToWords(name);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpExtension,
            WordCount = (ushort)(1 + nameWords.Length),
            Operands = nameWords
        });
    }

    /// <summary>
    ///     添加常量声明。
    /// </summary>
    /// <param name="typeId">类型 ID。</param>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="literalOperands">字面量操作数。</param>
    public void AddConstant(uint typeId, uint resultId, uint[] literalOperands)
    {
        var operands = new uint[2 + literalOperands.Length];
        operands[0] = typeId;
        operands[1] = resultId;
        Array.Copy(literalOperands, 0, operands, 2, literalOperands.Length);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpConstant,
            WordCount = (ushort)(3 + literalOperands.Length),
            Operands = operands
        });
    }

    /// <summary>
    ///     添加布尔常量 true。
    /// </summary>
    public void AddConstantTrue(uint typeId, uint resultId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpConstantTrue,
            WordCount = 3,
            Operands = [typeId, resultId]
        });
    }

    /// <summary>
    ///     添加布尔常量 false。
    /// </summary>
    public void AddConstantFalse(uint typeId, uint resultId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpConstantFalse,
            WordCount = 3,
            Operands = [typeId, resultId]
        });
    }

    /// <summary>
    ///     添加变量声明。
    /// </summary>
    /// <param name="resultTypeId">结果指针类型 ID。</param>
    /// <param name="resultId">结果 ID。</param>
    /// <param name="storageClass">存储类。</param>
    /// <param name="initializerId">初始化器 ID（可选）。</param>
    public void AddVariable(uint resultTypeId, uint resultId, uint storageClass, uint? initializerId = null)
    {
        var operands = new List<uint> { resultTypeId, resultId, storageClass };

        if (initializerId.HasValue)
        {
            operands.Add(initializerId.Value);
        }

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpVariable,
            WordCount = (ushort)(1 + operands.Count),
            Operands = operands.ToArray()
        });
    }

    /// <summary>
    ///     添加函数声明。
    /// </summary>
    /// <param name="resultTypeId">返回类型 ID。</param>
    /// <param name="resultId">函数 ID。</param>
    /// <param name="functionControl">函数控制标志。</param>
    /// <param name="functionTypeId">函数类型 ID。</param>
    public void AddFunction(uint resultTypeId, uint resultId, uint functionControl, uint functionTypeId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpFunction,
            WordCount = 5,
            Operands = [resultTypeId, resultId, functionControl, functionTypeId]
        });
    }

    /// <summary>
    ///     添加函数参数。
    /// </summary>
    public void AddFunctionParameter(uint resultTypeId, uint resultId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpFunctionParameter,
            WordCount = 3,
            Operands = [resultTypeId, resultId]
        });
    }

    /// <summary>
    ///     添加函数结束。
    /// </summary>
    public void AddFunctionEnd()
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpFunctionEnd,
            WordCount = 1,
            Operands = []
        });
    }

    /// <summary>
    ///     添加标签。
    /// </summary>
    public void AddLabel(uint resultId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpLabel,
            WordCount = 2,
            Operands = [resultId]
        });
    }

    /// <summary>
    ///     添加 Load 指令。
    /// </summary>
    public uint AddLoad(uint resultTypeId, uint pointerId)
    {
        var resultId = AllocateId();

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpLoad,
            WordCount = 4,
            Operands = [resultTypeId, resultId, pointerId]
        });

        return resultId;
    }

    /// <summary>
    ///     添加 Store 指令。
    /// </summary>
    public void AddStore(uint pointerId, uint objectId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpStore,
            WordCount = 3,
            Operands = [pointerId, objectId]
        });
    }

    /// <summary>
    ///     添加 AccessChain 指令。
    /// </summary>
    public uint AddAccessChain(uint resultTypeId, uint baseId, uint[] indexIds)
    {
        var resultId = AllocateId();
        var operands = new uint[3 + indexIds.Length];
        operands[0] = resultTypeId;
        operands[1] = resultId;
        operands[2] = baseId;
        Array.Copy(indexIds, 0, operands, 3, indexIds.Length);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpAccessChain,
            WordCount = (ushort)(4 + indexIds.Length),
            Operands = operands
        });

        return resultId;
    }

    /// <summary>
    ///     添加 Return 指令。
    /// </summary>
    public void AddReturn()
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpReturn,
            WordCount = 1,
            Operands = []
        });
    }

    /// <summary>
    ///     添加 ReturnValue 指令。
    /// </summary>
    public void AddReturnValue(uint valueId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpReturnValue,
            WordCount = 2,
            Operands = [valueId]
        });
    }

    /// <summary>
    ///     添加分支指令。
    /// </summary>
    public void AddBranch(uint targetLabelId)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpBranch,
            WordCount = 2,
            Operands = [targetLabelId]
        });
    }

    /// <summary>
    ///     添加条件分支指令。
    /// </summary>
    public void AddBranchConditional(uint conditionId, uint trueLabelId, uint falseLabelId, uint[]? branchWeights = null)
    {
        var operands = new List<uint> { conditionId, trueLabelId, falseLabelId };

        if (branchWeights is not null)
        {
            operands.AddRange(branchWeights);
        }

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpBranchConditional,
            WordCount = (ushort)(1 + operands.Count),
            Operands = operands.ToArray()
        });
    }

    /// <summary>
    ///     添加 SelectionMerge 指令。
    /// </summary>
    public void AddSelectionMerge(uint mergeLabelId, uint selectionControl)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpSelectionMerge,
            WordCount = 3,
            Operands = [mergeLabelId, selectionControl]
        });
    }

    /// <summary>
    ///     添加 LoopMerge 指令。
    /// </summary>
    public void AddLoopMerge(uint mergeLabelId, uint continueLabelId, uint loopControl)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpLoopMerge,
            WordCount = 4,
            Operands = [mergeLabelId, continueLabelId, loopControl]
        });
    }

    /// <summary>
    ///     添加算术指令（通用）。
    /// </summary>
    public uint AddArithmetic(ushort opcode, uint resultTypeId, uint operand1Id, uint operand2Id)
    {
        var resultId = AllocateId();

        _instructions.Add(new SpirvInstruction
        {
            Opcode = opcode,
            WordCount = 5,
            Operands = [resultTypeId, resultId, operand1Id, operand2Id]
        });

        return resultId;
    }

    /// <summary>
    ///     添加一元指令（通用）。
    /// </summary>
    public uint AddUnaryOp(ushort opcode, uint resultTypeId, uint operandId)
    {
        var resultId = AllocateId();

        _instructions.Add(new SpirvInstruction
        {
            Opcode = opcode,
            WordCount = 4,
            Operands = [resultTypeId, resultId, operandId]
        });

        return resultId;
    }

    /// <summary>
    ///     添加 Dot 指令。
    /// </summary>
    public uint AddDot(uint resultTypeId, uint vector1Id, uint vector2Id)
    {
        var resultId = AllocateId();

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpDot,
            WordCount = 5,
            Operands = [resultTypeId, resultId, vector1Id, vector2Id]
        });

        return resultId;
    }

    /// <summary>
    ///     添加 MatrixTimesVector 指令。
    /// </summary>
    public uint AddMatrixTimesVector(uint resultTypeId, uint matrixId, uint vectorId)
    {
        var resultId = AllocateId();

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpMatrixTimesVector,
            WordCount = 5,
            Operands = [resultTypeId, resultId, matrixId, vectorId]
        });

        return resultId;
    }

    /// <summary>
    ///     添加 CompositeConstruct 指令。
    /// </summary>
    public uint AddCompositeConstruct(uint resultTypeId, uint[] constituentIds)
    {
        var resultId = AllocateId();
        var operands = new uint[2 + constituentIds.Length];
        operands[0] = resultTypeId;
        operands[1] = resultId;
        Array.Copy(constituentIds, 0, operands, 2, constituentIds.Length);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpCompositeConstruct,
            WordCount = (ushort)(3 + constituentIds.Length),
            Operands = operands
        });

        return resultId;
    }

    /// <summary>
    ///     添加 CompositeExtract 指令。
    /// </summary>
    public uint AddCompositeExtract(uint resultTypeId, uint compositeId, uint[] indices)
    {
        var resultId = AllocateId();
        var operands = new uint[3 + indices.Length];
        operands[0] = resultTypeId;
        operands[1] = resultId;
        operands[2] = compositeId;
        Array.Copy(indices, 0, operands, 3, indices.Length);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpCompositeExtract,
            WordCount = (ushort)(4 + indices.Length),
            Operands = operands
        });

        return resultId;
    }

    /// <summary>
    ///     添加函数调用指令。
    /// </summary>
    public uint AddFunctionCall(uint resultTypeId, uint functionId, uint[] argumentIds)
    {
        var resultId = AllocateId();
        var operands = new uint[3 + argumentIds.Length];
        operands[0] = resultTypeId;
        operands[1] = resultId;
        operands[2] = functionId;
        Array.Copy(argumentIds, 0, operands, 3, argumentIds.Length);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpFunctionCall,
            WordCount = (ushort)(4 + argumentIds.Length),
            Operands = operands
        });

        return resultId;
    }

    /// <summary>
    ///     添加扩展指令调用。
    /// </summary>
    public uint AddExtInst(uint resultTypeId, uint setId, uint instruction, uint[] operandIds)
    {
        var resultId = AllocateId();
        var operands = new uint[4 + operandIds.Length];
        operands[0] = resultTypeId;
        operands[1] = resultId;
        operands[2] = setId;
        operands[3] = instruction;
        Array.Copy(operandIds, 0, operands, 4, operandIds.Length);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpExtInst,
            WordCount = (ushort)(5 + operandIds.Length),
            Operands = operands
        });

        return resultId;
    }

    /// <summary>
    ///     添加 Kill 指令（Fragment 着色器丢弃）。
    /// </summary>
    public void AddKill()
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpKill,
            WordCount = 1,
            Operands = []
        });
    }

    /// <summary>
    ///     添加向量洗牌指令。
    /// </summary>
    public uint AddVectorShuffle(uint resultTypeId, uint vec1Id, uint vec2Id, uint[] components)
    {
        var resultId = AllocateId();
        var operands = new uint[4 + components.Length];
        operands[0] = resultTypeId;
        operands[1] = resultId;
        operands[2] = vec1Id;
        operands[3] = vec2Id;
        Array.Copy(components, 0, operands, 4, components.Length);

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpVectorShuffle,
            WordCount = (ushort)(5 + components.Length),
            Operands = operands
        });

        return resultId;
    }

    /// <summary>
    ///     添加图像采样指令（隐式 LOD）。
    /// </summary>
    public uint AddImageSampleImplicitLod(uint resultTypeId, uint sampledImageId, uint coordinateId)
    {
        var resultId = AllocateId();

        _instructions.Add(new SpirvInstruction
        {
            Opcode = SpirvOpCode.OpImageSampleImplicitLod,
            WordCount = 5,
            Operands = [resultTypeId, resultId, sampledImageId, coordinateId]
        });

        return resultId;
    }

    /// <summary>
    ///     添加类型转换指令。
    /// </summary>
    public uint AddConvert(ushort opcode, uint resultTypeId, uint valueId)
    {
        var resultId = AllocateId();

        _instructions.Add(new SpirvInstruction
        {
            Opcode = opcode,
            WordCount = 4,
            Operands = [resultTypeId, resultId, valueId]
        });

        return resultId;
    }

    /// <summary>
    ///     添加自动分配 ID 的扩展指令导入。
    /// </summary>
    /// <param name="name">扩展指令集名称。</param>
    /// <returns>导入的结果 ID。</returns>
    public uint AddExtInstImport(string name)
    {
        var resultId = AllocateId();
        AddExtInstImport(resultId, name);
        return resultId;
    }

    /// <summary>
    ///     添加自动分配 ID 的变量声明。
    /// </summary>
    /// <param name="resultTypeId">结果指针类型 ID。</param>
    /// <param name="storageClass">存储类。</param>
    /// <returns>变量的结果 ID。</returns>
    public uint AddVariable(uint resultTypeId, uint storageClass)
    {
        var resultId = AllocateId();
        AddVariable(resultTypeId, resultId, storageClass);
        return resultId;
    }

    /// <summary>
    ///     添加自动分配 ID 的函数参数。
    /// </summary>
    /// <param name="resultTypeId">参数类型 ID。</param>
    /// <returns>参数的结果 ID。</returns>
    public uint AddFunctionParameter(uint resultTypeId)
    {
        var resultId = AllocateId();
        AddFunctionParameter(resultTypeId, resultId);
        return resultId;
    }

    /// <summary>
    ///     添加自动分配 ID 的标签。
    /// </summary>
    /// <returns>标签的结果 ID。</returns>
    public uint AddLabel()
    {
        var resultId = AllocateId();
        AddLabel(resultId);
        return resultId;
    }

    /// <summary>
    ///     结束函数（OpFunctionEnd 的便捷方法）。
    /// </summary>
    public void EndFunction()
    {
        AddFunctionEnd();
    }

    /// <summary>
    ///     添加自动分配 ID 的常量声明（32 位值）。
    /// </summary>
    /// <param name="typeId">类型 ID。</param>
    /// <param name="value">常量值。</param>
    /// <returns>常量的结果 ID。</returns>
    public uint AddConstant(uint typeId, uint value)
    {
        var resultId = AllocateId();
        AddConstant(typeId, resultId, [value]);
        return resultId;
    }

    /// <summary>
    ///     添加自定义指令。
    /// </summary>
    /// <param name="opcode">操作码。</param>
    /// <param name="operands">操作数。</param>
    public void AddRawInstruction(ushort opcode, uint[] operands)
    {
        _instructions.Add(new SpirvInstruction
        {
            Opcode = opcode,
            WordCount = (ushort)(1 + operands.Length),
            Operands = operands
        });
    }

    /// <summary>
    ///     将构建的模块导出为字节数组。
    /// </summary>
    /// <param name="version">SPIR-V 版本号。</param>
    /// <param name="generatorMagic">生成器魔数。</param>
    /// <param name="schema">Schema 值。</param>
    /// <returns>SPIR-V 二进制数据。</returns>
    public byte[] ToByteArray(uint version = 0x00010000, uint generatorMagic = 0x00470000, uint schema = 0)
    {
        var moduleData = new SpirvModuleData
        {
            MagicNumber = SpirvConstants.MagicNumber,
            Version = version,
            GeneratorMagic = generatorMagic,
            Bound = _nextId,
            Schema = schema,
            Instructions = _instructions
        };

        var encoder = new SpirvEncoder();
        return encoder.Encode(moduleData);
    }

    /// <summary>
    ///     将字符串转换为 SPIR-V 字序列（null 终止，4 字节对齐）。
    /// </summary>
    /// <param name="value">字符串值。</param>
    /// <returns>字列表。</returns>
    public static uint[] StringToWords(string value)
    {
        var encoder = new SpirvEncoder();
        return encoder.EncodeString(value).ToArray();
    }
}
