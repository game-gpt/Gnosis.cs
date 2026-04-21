namespace Gnosis.Rendering.ShaderCompiler.Backend.Spirv;

public sealed class SpirvBuilder
{
    #region Fields

    private readonly List<uint> _instructions = new();
    private uint _nextId = 1;
    private uint _bound = 1;

    #endregion

    #region Properties

    public uint Bound => _bound;
    public IReadOnlyList<uint> Instructions => _instructions;

    #endregion

    #region Public Methods

    public uint AllocateId()
    {
        var id = _nextId++;
        _bound = _nextId;
        return id;
    }

    public void AddCapability(uint capability)
    {
        AddInstruction(SpirvConstants.Op.OpCapability, capability);
    }

    public void AddExtension(string extension)
    {
        var stringId = AllocateId();
        AddInstructionWithString(SpirvConstants.Op.OpExtension, extension);
    }

    public uint AddExtInstImport(string name)
    {
        var resultId = AllocateId();
        AddInstructionWithString(SpirvConstants.Op.OpExtInstImport, resultId, name);
        return resultId;
    }

    public void SetMemoryModel(uint addressingModel, uint memoryModel)
    {
        AddInstruction(SpirvConstants.Op.OpMemoryModel, addressingModel, memoryModel);
    }

    public void AddEntryPoint(uint executionModel, uint entryPointId, string name, uint[] interfaceIds)
    {
        var words = new List<uint> { SpirvConstants.Op.OpEntryPoint, executionModel, entryPointId };
        words.AddRange(StringToWords(name));
        words.AddRange(interfaceIds);
        words[0] |= (uint)(words.Count << 16);
        _instructions.AddRange(words);
    }

    public void AddExecutionMode(uint entryPointId, uint mode, params uint[] extraOperands)
    {
        var words = new List<uint>
        {
            SpirvConstants.Op.OpExecutionMode | ((uint)(3 + extraOperands.Length) << 16),
            entryPointId,
            mode
        };
        words.AddRange(extraOperands);
        _instructions.AddRange(words);
    }

    public uint DeclareVoidType()
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpTypeVoid | (2u << 16), resultId);
        return resultId;
    }

    public uint DeclareBoolType()
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpTypeBool | (2u << 16), resultId);
        return resultId;
    }

    public uint DeclareIntType(uint bitWidth, uint signedness)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpTypeInt | (4u << 16), resultId, bitWidth, signedness);
        return resultId;
    }

    public uint DeclareFloatType(uint bitWidth)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpTypeFloat | (3u << 16), resultId, bitWidth);
        return resultId;
    }

    public uint DeclareVectorType(uint elementType, uint componentCount)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpTypeVector | (4u << 16), resultId, elementType, componentCount);
        return resultId;
    }

    public uint DeclareMatrixType(uint columnType, uint columnCount)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpTypeMatrix | (4u << 16), resultId, columnType, columnCount);
        return resultId;
    }

    public uint DeclareStructType(uint[] memberTypeIds)
    {
        var resultId = AllocateId();
        var words = new List<uint> { SpirvConstants.Op.OpTypeStruct | ((uint)(2 + memberTypeIds.Length) << 16), resultId };
        words.AddRange(memberTypeIds);
        _instructions.AddRange(words);
        return resultId;
    }

    public uint DeclareImageType(uint sampledType, uint dim, uint depth, uint arrayed, uint ms, uint format)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpTypeImage | (8u << 16), resultId, sampledType, dim, depth, arrayed, ms, format);
        return resultId;
    }

    public uint DeclareSamplerType()
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpTypeSampler | (2u << 16), resultId);
        return resultId;
    }

    public uint DeclareSampledImageType(uint imageType)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpTypeSampledImage | (3u << 16), resultId, imageType);
        return resultId;
    }

    public uint DeclarePointerType(uint pointeeType, uint storageClass)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpTypePointer | (4u << 16), resultId, storageClass, pointeeType);
        return resultId;
    }

    public uint DeclareFunctionType(uint returnType, uint[] parameterTypes)
    {
        var resultId = AllocateId();
        var words = new List<uint> { SpirvConstants.Op.OpTypeFunction | ((uint)(3 + parameterTypes.Length) << 16), resultId, returnType };
        words.AddRange(parameterTypes);
        _instructions.AddRange(words);
        return resultId;
    }

    public uint DeclareAccelerationStructureType()
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpTypeAccelerationStructureKHR | (2u << 16), resultId);
        return resultId;
    }

    public void AddDecorate(uint targetId, uint decoration, uint? extra = null)
    {
        if (extra.HasValue)
        {
            AddInstruction(SpirvConstants.Op.OpDecorate | (4u << 16), targetId, decoration, extra.Value);
        }
        else
        {
            AddInstruction(SpirvConstants.Op.OpDecorate | (3u << 16), targetId, decoration);
        }
    }

    public void AddMemberDecorate(uint structType, uint memberIndex, uint decoration, uint extra)
    {
        AddInstruction(SpirvConstants.Op.OpMemberDecorate | (5u << 16), structType, memberIndex, decoration, extra);
    }

    public void AddName(uint targetId, string name)
    {
        AddInstructionWithString(SpirvConstants.Op.OpName, targetId, name);
    }

    public void AddMemberName(uint structType, uint memberIndex, string name)
    {
        AddInstructionWithString(SpirvConstants.Op.OpMemberName, structType, memberIndex, name);
    }

    public uint AddVariable(uint resultType, uint storageClass, uint? initializer = null)
    {
        var resultId = AllocateId();
        if (initializer.HasValue)
        {
            AddInstruction(SpirvConstants.Op.OpVariable | (4u << 16), resultType, resultId, storageClass, initializer.Value);
        }
        else
        {
            AddInstruction(SpirvConstants.Op.OpVariable | (4u << 16), resultType, resultId, storageClass);
        }
        return resultId;
    }

    public uint AddFunction(uint resultType, uint functionId, uint functionControl, uint functionType)
    {
        AddInstruction(SpirvConstants.Op.OpFunction | (5u << 16), resultType, functionId, functionControl, functionType);
        return functionId;
    }

    public uint AddFunctionParameter(uint resultType)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpFunctionParameter | (3u << 16), resultType, resultId);
        return resultId;
    }

    public void EndFunction()
    {
        AddInstruction(SpirvConstants.Op.OpFunctionEnd | (1u << 16));
    }

    public uint AddLabel()
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpLabel | (2u << 16), resultId);
        return resultId;
    }

    public uint AddLoad(uint resultType, uint pointer)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpLoad | (4u << 16), resultType, resultId, pointer);
        return resultId;
    }

    public void AddStore(uint pointer, uint value)
    {
        AddInstruction(SpirvConstants.Op.OpStore | (3u << 16), pointer, value);
    }

    public uint AddAccessChain(uint resultType, uint baseId, uint[] indexIds)
    {
        var resultId = AllocateId();
        var words = new List<uint> { SpirvConstants.Op.OpAccessChain | ((uint)(4 + indexIds.Length) << 16), resultType, resultId, baseId };
        words.AddRange(indexIds);
        _instructions.AddRange(words);
        return resultId;
    }

    public uint AddCompositeConstruct(uint resultType, uint[] constituentIds)
    {
        var resultId = AllocateId();
        var words = new List<uint> { SpirvConstants.Op.OpCompositeConstruct | ((uint)(3 + constituentIds.Length) << 16), resultType, resultId };
        words.AddRange(constituentIds);
        _instructions.AddRange(words);
        return resultId;
    }

    public uint AddCompositeExtract(uint resultType, uint compositeId, uint[] indices)
    {
        var resultId = AllocateId();
        var words = new List<uint> { SpirvConstants.Op.OpCompositeExtract | ((uint)(4 + indices.Length) << 16), resultType, resultId, compositeId };
        words.AddRange(indices);
        _instructions.AddRange(words);
        return resultId;
    }

    public uint AddVectorShuffle(uint resultType, uint vec1, uint vec2, uint[] components)
    {
        var resultId = AllocateId();
        var words = new List<uint> { SpirvConstants.Op.OpVectorShuffle | ((uint)(5 + components.Length) << 16), resultType, resultId, vec1, vec2 };
        words.AddRange(components);
        _instructions.AddRange(words);
        return resultId;
    }

    public uint AddArithmetic(uint opCode, uint resultType, uint left, uint right)
    {
        var resultId = AllocateId();
        AddInstruction(opCode | (5u << 16), resultType, resultId, left, right);
        return resultId;
    }

    public uint AddUnaryOp(uint opCode, uint resultType, uint operand)
    {
        var resultId = AllocateId();
        AddInstruction(opCode | (4u << 16), resultType, resultId, operand);
        return resultId;
    }

    public uint AddDot(uint resultType, uint left, uint right)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpDot | (5u << 16), resultType, resultId, left, right);
        return resultId;
    }

    public uint AddExtInst(uint resultType, uint setId, uint instruction, uint[] operandIds)
    {
        var resultId = AllocateId();
        var words = new List<uint> { SpirvConstants.Op.OpExtInst | ((uint)(5 + operandIds.Length) << 16), resultType, resultId, setId, instruction };
        words.AddRange(operandIds);
        _instructions.AddRange(words);
        return resultId;
    }

    public uint AddSampledImage(uint resultType, uint image, uint sampler)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpSampledImage | (5u << 16), resultType, resultId, image, sampler);
        return resultId;
    }

    public uint AddImageSampleImplicitLod(uint resultType, uint sampledImage, uint coordinate)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpImageSampleImplicitLod | (5u << 16), resultType, resultId, sampledImage, coordinate);
        return resultId;
    }

    public uint AddFunctionCall(uint resultType, uint functionId, uint[] argumentIds)
    {
        var resultId = AllocateId();
        var words = new List<uint> { SpirvConstants.Op.OpFunctionCall | ((uint)(4 + argumentIds.Length) << 16), resultType, resultId, functionId };
        words.AddRange(argumentIds);
        _instructions.AddRange(words);
        return resultId;
    }

    public void AddBranch(uint targetLabel)
    {
        AddInstruction(SpirvConstants.Op.OpBranch | (2u << 16), targetLabel);
    }

    public void AddBranchConditional(uint condition, uint trueLabel, uint falseLabel)
    {
        AddInstruction(SpirvConstants.Op.OpBranchConditional | (4u << 16), condition, trueLabel, falseLabel);
    }

    public void AddReturn()
    {
        AddInstruction(SpirvConstants.Op.OpReturn | (1u << 16));
    }

    public void AddReturnValue(uint valueId)
    {
        AddInstruction(SpirvConstants.Op.OpReturnValue | (2u << 16), valueId);
    }

    public void AddKill()
    {
        AddInstruction(SpirvConstants.Op.OpKill | (1u << 16));
    }

    public void AddSelectionMerge(uint mergeLabel, uint selectionControl = 0)
    {
        AddInstruction(SpirvConstants.Op.OpSelectionMerge | (3u << 16), mergeLabel, selectionControl);
    }

    public void AddLoopMerge(uint mergeLabel, uint continueLabel, uint loopControl = 0)
    {
        AddInstruction(SpirvConstants.Op.OpLoopMerge | (4u << 16), mergeLabel, continueLabel, loopControl);
    }

    public uint AddConvert(uint opCode, uint resultType, uint valueId)
    {
        var resultId = AllocateId();
        AddInstruction(opCode | (4u << 16), resultType, resultId, valueId);
        return resultId;
    }

    public uint AddConstant(uint resultType, uint value)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpConstant | (4u << 16), resultType, resultId, value);
        return resultId;
    }

    public uint AddSpecConstant(uint resultType, uint value)
    {
        var resultId = AllocateId();
        AddInstruction(SpirvConstants.Op.OpSpecConstant | (4u << 16), resultType, resultId, value);
        return resultId;
    }

    public void AddSource(uint sourceLanguage, uint version)
    {
        AddInstruction(SpirvConstants.Op.OpSource | (3u << 16), sourceLanguage, version);
    }

    public byte[] ToByteArray()
    {
        var header = new uint[]
        {
            SpirvConstants.MagicNumber,
            SpirvConstants.Version15,
            SpirvConstants.GeneratorMagicNumber,
            _bound,
            SpirvConstants.Schema
        };

        var result = new byte[(header.Length + _instructions.Count) * 4];
        Buffer.BlockCopy(header, 0, result, 0, header.Length * 4);
        Buffer.BlockCopy(_instructions.ToArray(), 0, result, header.Length * 4, _instructions.Count * 4);
        return result;
    }

    #endregion

    #region Private Methods

    private void AddInstruction(uint word0, params uint[] operands)
    {
        _instructions.Add(word0);
        _instructions.AddRange(operands);
    }

    private void AddInstructionWithString(uint opCode, uint resultId, string str)
    {
        var words = new List<uint> { opCode, resultId };
        words.AddRange(StringToWords(str));
        words[0] |= (uint)(words.Count << 16);
        _instructions.AddRange(words);
    }

    private void AddInstructionWithString(uint opCode, uint id1, uint id2, string str)
    {
        var words = new List<uint> { opCode, id1, id2 };
        words.AddRange(StringToWords(str));
        words[0] |= (uint)(words.Count << 16);
        _instructions.AddRange(words);
    }

    private void AddInstructionWithString(uint opCode, string str)
    {
        var words = new List<uint> { opCode };
        words.AddRange(StringToWords(str));
        words[0] |= (uint)(words.Count << 16);
        _instructions.AddRange(words);
    }

    private static List<uint> StringToWords(string str)
    {
        var words = new List<uint>();
        var bytes = System.Text.Encoding.UTF8.GetBytes(str + "\0");
        for (int i = 0; i < bytes.Length; i += 4)
        {
            uint word = 0;
            for (int j = 0; j < 4 && i + j < bytes.Length; j++)
            {
                word |= (uint)bytes[i + j] << (j * 8);
            }
            words.Add(word);
        }
        return words;
    }

    #endregion
}
