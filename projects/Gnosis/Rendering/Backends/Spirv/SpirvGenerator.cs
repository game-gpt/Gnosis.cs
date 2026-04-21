using Gnosis.Rendering.Backends.ShaderIR;

namespace Gnosis.Rendering.Backends.Spirv;

public sealed class SpirvGenerator
{
    #region Fields

    private readonly SpirvBuilder _builder;
    private readonly SpirvTypeCache _typeCache;
    private readonly Dictionary<string, uint> _functionIds = new();
    private readonly Dictionary<uint, uint> _variableIds = new();
    private uint _glslStd450Id;

    #endregion

    #region Constructors

    public SpirvGenerator()
    {
        _builder = new SpirvBuilder();
        _typeCache = new SpirvTypeCache(_builder);
    }

    #endregion

    #region Public Methods

    public byte[] Generate(ShaderModuleIr module)
    {
        _builder.AddCapability(SpirvConstants.Capability.Shader);

        var hasRayTracing = module.EntryPoints.Any(ep =>
            ep.ExecutionModel is ShaderExecutionModel.RayGenerationKHR
                or ShaderExecutionModel.ClosestHitKHR
                or ShaderExecutionModel.MissKHR
                or ShaderExecutionModel.AnyHitKHR);

        if (hasRayTracing)
        {
            _builder.AddCapability(SpirvConstants.Capability.RayTracingKHR);
            _builder.AddExtension("SPV_KHR_ray_tracing");
        }

        _glslStd450Id = _builder.AddExtInstImport("GLSL.std.450");

        _builder.SetMemoryModel(
            SpirvConstants.AddressingModel.Logical,
            SpirvConstants.MemoryModel.GLSL450);

        _builder.AddSource(SpirvConstants.SourceLanguage.GLSL, 450);

        GenerateStructs(module.Structs);
        GenerateGlobalVariables(module.GlobalVariables);
        GenerateFunctions(module.Functions);
        GenerateEntryPoints(module.EntryPoints);
        GenerateDecorations(module.GlobalVariables, module.Structs);

        return _builder.ToByteArray();
    }

    #endregion

    #region Private Methods - Structs

    private void GenerateStructs(IReadOnlyList<ShaderStructIr> structs)
    {
        foreach (var structIr in structs)
        {
            var memberTypeIds = structIr.Fields.Select(f => MapType(f.Type)).ToArray();
            var structId = _typeCache.GetStructType(memberTypeIds, structIr.Name);
            _builder.AddName(structId, structIr.Name);

            for (var i = 0; i < structIr.Fields.Count; i++)
            {
                _builder.AddMemberName(structId, (uint)i, structIr.Fields[i].Name);
                _builder.AddMemberDecorate(structId, (uint)i, SpirvConstants.Decoration.Offset, structIr.Fields[i].Offset);
            }
        }
    }

    #endregion

    #region Private Methods - Global Variables

    private void GenerateGlobalVariables(IReadOnlyList<ShaderGlobalVariableIr> globals)
    {
        foreach (var global in globals)
        {
            var typeId = MapType(global.Type);
            var ptrTypeId = _typeCache.GetPointerType(typeId, MapStorageClass(global.Storage));
            var varId = _builder.AddVariable(ptrTypeId, MapStorageClass(global.Storage));
            _builder.AddName(varId, global.Name);
            _variableIds[global.ResultId] = varId;
        }
    }

    #endregion

    #region Private Methods - Functions

    private void GenerateFunctions(IReadOnlyList<ShaderFunctionIr> functions)
    {
        foreach (var function in functions)
        {
            GenerateFunction(function);
        }
    }

    private void GenerateFunction(ShaderFunctionIr function)
    {
        var returnType = MapType(function.ReturnType);
        var paramTypes = function.Parameters.Select(p => MapType(p.Type)).ToArray();
        var funcType = _typeCache.GetFunctionType(returnType, paramTypes);

        var funcId = _builder.AllocateId();
        _functionIds[function.Name] = funcId;
        _builder.AddName(funcId, function.Name);

        _builder.AddFunction(returnType, funcId, 0, funcType);

        foreach (var param in function.Parameters)
        {
            var paramTypeId = MapType(param.Type);
            var paramId = _builder.AddFunctionParameter(paramTypeId);
            _variableIds[param.ResultId] = paramId;
        }

        var labelId = _builder.AddLabel();

        foreach (var local in function.LocalVariables)
        {
            var localTypeId = MapType(local.ResultType);
            var ptrTypeId = _typeCache.GetPointerType(localTypeId, SpirvConstants.StorageClass.Function);
            var localVarId = _builder.AddVariable(ptrTypeId, SpirvConstants.StorageClass.Function);
            _builder.AddName(localVarId, local.Name);
            _variableIds[local.ResultId] = localVarId;
        }

        foreach (var instruction in function.Instructions)
        {
            EmitInstruction(instruction);
        }

        _builder.EndFunction();
    }

    #endregion

    #region Private Methods - Entry Points

    private void GenerateEntryPoints(IReadOnlyList<ShaderEntryPointIr> entryPoints)
    {
        foreach (var entry in entryPoints)
        {
            if (!_functionIds.TryGetValue(entry.FunctionName, out var funcId))
            {
                continue;
            }

            var execModel = MapExecutionModel(entry.ExecutionModel);
            var interfaceIds = entry.InterfaceVariables
                .Where(v => _variableIds.Values.Contains(_variableIds.Values.FirstOrDefault()))
                .Select(v => _variableIds.Values.FirstOrDefault())
                .Where(id => id != 0)
                .ToArray();

            _builder.AddEntryPoint(execModel, funcId, entry.FunctionName, interfaceIds);

            if (entry.ExecutionModel == ShaderExecutionModel.Fragment)
            {
                _builder.AddExecutionMode(funcId, SpirvConstants.ExecutionMode.OriginUpperLeft);
            }
            else if (entry.ExecutionModel == ShaderExecutionModel.GLCompute)
            {
                _builder.AddExecutionMode(funcId, SpirvConstants.ExecutionMode.LocalSize, 1, 1, 1);
            }
        }
    }

    #endregion

    #region Private Methods - Decorations

    private void GenerateDecorations(IReadOnlyList<ShaderGlobalVariableIr> globals, IReadOnlyList<ShaderStructIr> structs)
    {
        foreach (var global in globals)
        {
            if (!_variableIds.TryGetValue(global.ResultId, out var varId))
            {
                continue;
            }

            if (global.Location.HasValue)
            {
                _builder.AddDecorate(varId, SpirvConstants.Decoration.Location, global.Location.Value);
            }

            if (global.Builtin != null)
            {
                var builtinValue = MapBuiltin(global.Builtin);
                _builder.AddDecorate(varId, SpirvConstants.Decoration.BuiltIn, builtinValue);
            }

            if (global.Resource != null)
            {
                _builder.AddDecorate(varId, SpirvConstants.Decoration.DescriptorSet, global.Resource.DescriptorSet);
                _builder.AddDecorate(varId, SpirvConstants.Decoration.Binding, global.Resource.Binding);

                if (global.Resource.Kind == ShaderResourceKind.UniformBuffer)
                {
                    var structTypeId = _typeCache.GetStructType(
                        global.Resource.Type is ShaderIrType.StructType s
                            ? s.Fields.Select(f => MapType(f.Type)).ToArray()
                            : [],
                        global.Resource.Type is ShaderIrType.StructType s2 ? s2.Name : "unknown");
                    _builder.AddDecorate(structTypeId, SpirvConstants.Decoration.Block);
                }
            }
        }
    }

    #endregion

    #region Private Methods - Instruction Emission

    private void EmitInstruction(ShaderIrInstruction instruction)
    {
        switch (instruction)
        {
            case LabelInstruction label:
                _builder.AddLabel();
                break;

            case ArithmeticInstruction arith:
                EmitArithmetic(arith);
                break;

            case CompareInstruction cmp:
                EmitCompare(cmp);
                break;

            case LogicalInstruction log:
                EmitLogical(log);
                break;

            case VectorSwizzleInstruction swizzle:
                EmitVectorSwizzle(swizzle);
                break;

            case VectorConstructInstruction construct:
                EmitVectorConstruct(construct);
                break;

            case VectorBuiltinInstruction vecBuiltin:
                EmitVectorBuiltin(vecBuiltin);
                break;

            case MatrixInstruction mat:
                EmitMatrix(mat);
                break;

            case TextureSampleInstruction texSample:
                EmitTextureSample(texSample);
                break;

            case TextureLoadInstruction texLoad:
                EmitTextureLoad(texLoad);
                break;

            case TextureStoreInstruction texStore:
                EmitTextureStore(texStore);
                break;

            case BranchInstruction branch:
                _builder.AddBranch(branch.TargetLabelId);
                break;

            case BranchConditionalInstruction branchCond:
                _builder.AddBranchConditional(branchCond.ConditionId, branchCond.TrueLabelId, branchCond.FalseLabelId);
                break;

            case ReturnInstruction ret:
                if (ret.ValueId.HasValue)
                {
                    _builder.AddReturnValue(ret.ValueId.Value);
                }
                else
                {
                    _builder.AddReturn();
                }
                break;

            case DiscardInstruction:
                _builder.AddKill();
                break;

            case SelectionMergeInstruction selMerge:
                _builder.AddSelectionMerge(selMerge.MergeLabelId);
                break;

            case LoopMergeInstruction loopMerge:
                _builder.AddLoopMerge(loopMerge.MergeLabelId, loopMerge.ContinueLabelId);
                break;

            case LoadInstruction load:
                EmitLoad(load);
                break;

            case StoreInstruction store:
                _builder.AddStore(store.PointerId, store.ValueId);
                break;

            case AccessChainInstruction access:
                EmitAccessChain(access);
                break;

            case CompositeConstructInstruction comp:
                EmitCompositeConstruct(comp);
                break;

            case CompositeExtractInstruction extract:
                EmitCompositeExtract(extract);
                break;

            case CallInstruction call:
                EmitCall(call);
                break;

            case CallBuiltinInstruction callBuiltin:
                EmitCallBuiltin(callBuiltin);
                break;

            case ConvertInstruction convert:
                EmitConvert(convert);
                break;

            case PhiInstruction phi:
                break;
        }
    }

    private void EmitArithmetic(ArithmeticInstruction arith)
    {
        var resultType = MapType(arith.ResultType);
        var left = arith.LeftId;
        var right = arith.RightId;

        var op = arith.OpCode switch
        {
            ShaderIrOpCode.Add => IsFloatType(arith.ResultType) ? SpirvConstants.Op.OpFAdd : SpirvConstants.Op.OpIAdd,
            ShaderIrOpCode.Sub => IsFloatType(arith.ResultType) ? SpirvConstants.Op.OpFSub : SpirvConstants.Op.OpISub,
            ShaderIrOpCode.Mul => IsFloatType(arith.ResultType) ? SpirvConstants.Op.OpFMul : SpirvConstants.Op.OpIMul,
            ShaderIrOpCode.Div => IsFloatType(arith.ResultType) ? SpirvConstants.Op.OpFDiv : SpirvConstants.Op.OpSDiv,
            ShaderIrOpCode.Mod => SpirvConstants.Op.OpFDiv,
            ShaderIrOpCode.Negate => IsFloatType(arith.ResultType) ? SpirvConstants.Op.OpFNegate : SpirvConstants.Op.OpSNegate,
            _ => SpirvConstants.Op.OpFAdd
        };

        if (arith.OpCode == ShaderIrOpCode.Negate)
        {
            var resultId = _builder.AddUnaryOp(op, resultType, left);
            _variableIds[arith.ResultId] = resultId;
        }
        else
        {
            var resultId = _builder.AddArithmetic(op, resultType, left, right);
            _variableIds[arith.ResultId] = resultId;
        }
    }

    private void EmitCompare(CompareInstruction cmp)
    {
        var resultType = _typeCache.GetBoolType();
        var left = cmp.LeftId;
        var right = cmp.RightId;

        var op = cmp.OpCode switch
        {
            ShaderIrOpCode.Equal => IsFloatType(cmp.ResultType) ? SpirvConstants.Op.OpFOrdEqual : SpirvConstants.Op.OpIEqual,
            ShaderIrOpCode.NotEqual => IsFloatType(cmp.ResultType) ? SpirvConstants.Op.OpFOrdNotEqual : SpirvConstants.Op.OpINotEqual,
            ShaderIrOpCode.LessThan => IsFloatType(cmp.ResultType) ? SpirvConstants.Op.OpFOrdLessThan : SpirvConstants.Op.OpSLessThan,
            ShaderIrOpCode.GreaterThan => IsFloatType(cmp.ResultType) ? SpirvConstants.Op.OpFOrdGreaterThan : SpirvConstants.Op.OpSGreaterThan,
            ShaderIrOpCode.LessEqual => IsFloatType(cmp.ResultType) ? SpirvConstants.Op.OpFOrdLessEqual : SpirvConstants.Op.OpSLessEqual,
            ShaderIrOpCode.GreaterEqual => IsFloatType(cmp.ResultType) ? SpirvConstants.Op.OpFOrdGreaterEqual : SpirvConstants.Op.OpSGreaterEqual,
            _ => SpirvConstants.Op.OpFOrdEqual
        };

        var resultId = _builder.AddArithmetic(op, resultType, left, right);
        _variableIds[cmp.ResultId] = resultId;
    }

    private void EmitLogical(LogicalInstruction log)
    {
        var resultType = _typeCache.GetBoolType();

        var op = log.OpCode switch
        {
            ShaderIrOpCode.LogicalAnd => SpirvConstants.Op.OpLogicalAnd,
            ShaderIrOpCode.LogicalOr => SpirvConstants.Op.OpLogicalOr,
            ShaderIrOpCode.LogicalNot => SpirvConstants.Op.OpLogicalNot,
            _ => SpirvConstants.Op.OpLogicalAnd
        };

        if (log.OpCode == ShaderIrOpCode.LogicalNot)
        {
            var resultId = _builder.AddUnaryOp(op, resultType, log.LeftId);
            _variableIds[log.ResultId] = resultId;
        }
        else
        {
            var resultId = _builder.AddArithmetic(op, resultType, log.LeftId, log.RightId);
            _variableIds[log.ResultId] = resultId;
        }
    }

    private void EmitVectorSwizzle(VectorSwizzleInstruction swizzle)
    {
        var resultType = MapType(swizzle.ResultType);
        var resultId = _builder.AddVectorShuffle(resultType, swizzle.VectorId, swizzle.VectorId, swizzle.Components.Select(c => (uint)c).ToArray());
        _variableIds[swizzle.ResultId] = resultId;
    }

    private void EmitVectorConstruct(VectorConstructInstruction construct)
    {
        var resultType = MapType(construct.ResultType);
        var resultId = _builder.AddCompositeConstruct(resultType, construct.ComponentIds);
        _variableIds[construct.ResultId] = resultId;
    }

    private void EmitVectorBuiltin(VectorBuiltinInstruction vecBuiltin)
    {
        var resultType = MapType(vecBuiltin.ResultType);

        if (vecBuiltin.OpCode == ShaderIrOpCode.Dot)
        {
            var resultId = _builder.AddDot(resultType, vecBuiltin.OperandIds[0], vecBuiltin.OperandIds[1]);
            _variableIds[vecBuiltin.ResultId] = resultId;
            return;
        }

        var glslInstruction = vecBuiltin.OpCode switch
        {
            ShaderIrOpCode.Cross => SpirvConstants.GLSLstd450.Cross,
            ShaderIrOpCode.Normalize => SpirvConstants.GLSLstd450.Normalize,
            ShaderIrOpCode.Length => SpirvConstants.GLSLstd450.Length,
            _ => SpirvConstants.GLSLstd450.Normalize
        };

        var extResultId = _builder.AddExtInst(resultType, _glslStd450Id, glslInstruction, vecBuiltin.OperandIds);
        _variableIds[vecBuiltin.ResultId] = extResultId;
    }

    private void EmitMatrix(MatrixInstruction mat)
    {
        var resultType = MapType(mat.ResultType);

        if (mat.OpCode == ShaderIrOpCode.MatrixMultiply)
        {
            var resultId = _builder.AddArithmetic(SpirvConstants.Op.OpMatrixTimesVector, resultType, mat.LeftId, mat.RightId);
            _variableIds[mat.ResultId] = resultId;
            return;
        }

        var glslInstruction = mat.OpCode switch
        {
            ShaderIrOpCode.MatrixTranspose => SpirvConstants.Op.OpMatrixTimesVector,
            ShaderIrOpCode.MatrixInverse => SpirvConstants.GLSLstd450.MatrixInverse,
            _ => 0u
        };

        if (mat.OpCode == ShaderIrOpCode.MatrixInverse)
        {
            var resultId = _builder.AddExtInst(resultType, _glslStd450Id, glslInstruction, [mat.LeftId]);
            _variableIds[mat.ResultId] = resultId;
        }
    }

    private void EmitTextureSample(TextureSampleInstruction texSample)
    {
        var resultType = MapType(texSample.ResultType);
        var resultId = _builder.AddImageSampleImplicitLod(resultType, texSample.SampledImageId, texSample.CoordinateId);
        _variableIds[texSample.ResultId] = resultId;
    }

    private void EmitTextureLoad(TextureLoadInstruction texLoad)
    {
        var resultType = MapType(texLoad.ResultType);
        var resultId = _builder.AddArithmetic(SpirvConstants.Op.OpImageFetch, resultType, texLoad.ImageId, texLoad.CoordinateId);
        _variableIds[texLoad.ResultId] = resultId;
    }

    private void EmitTextureStore(TextureStoreInstruction texStore)
    {
        _builder.AddStore(texStore.ImageId, texStore.ValueId);
    }

    private void EmitLoad(LoadInstruction load)
    {
        var resultType = MapType(load.ResultType);

        if (load.Value.HasValue)
        {
            var resultId = load.ResultType switch
            {
                ShaderIrType.BoolType => _builder.AddConstant(resultType, load.Value.Value),
                ShaderIrType.IntType => _builder.AddConstant(resultType, load.Value.Value),
                ShaderIrType.FloatType => _builder.AddConstant(resultType, load.Value.Value),
                _ => _builder.AddConstant(resultType, load.Value.Value)
            };
            _variableIds[load.ResultId] = resultId;
            return;
        }

        var ptrId = _variableIds.TryGetValue(load.PointerId, out var pid) ? pid : load.PointerId;
        var resultId2 = _builder.AddLoad(resultType, ptrId);
        _variableIds[load.ResultId] = resultId2;
    }

    private void EmitAccessChain(AccessChainInstruction access)
    {
        var resultType = MapType(access.ResultType);
        var baseId = _variableIds.TryGetValue(access.BaseId, out var bid) ? bid : access.BaseId;
        var resultId = _builder.AddAccessChain(resultType, baseId, access.IndexIds);
        _variableIds[access.ResultId] = resultId;
    }

    private void EmitCompositeConstruct(CompositeConstructInstruction comp)
    {
        var resultType = MapType(comp.ResultType);
        var resultId = _builder.AddCompositeConstruct(resultType, comp.ConstituentIds);
        _variableIds[comp.ResultId] = resultId;
    }

    private void EmitCompositeExtract(CompositeExtractInstruction extract)
    {
        var resultType = MapType(extract.ResultType);
        var resultId = _builder.AddCompositeExtract(resultType, extract.CompositeId, extract.Indices.Select(i => (uint)i).ToArray());
        _variableIds[extract.ResultId] = resultId;
    }

    private void EmitCall(CallInstruction call)
    {
        var resultType = MapType(call.ResultType);
        if (_functionIds.TryGetValue(call.FunctionName, out var funcId))
        {
            var resultId = _builder.AddFunctionCall(resultType, funcId, call.ArgumentIds);
            _variableIds[call.ResultId] = resultId;
        }
    }

    private void EmitCallBuiltin(CallBuiltinInstruction callBuiltin)
    {
        var resultType = MapType(callBuiltin.ResultType);
        var glslInstruction = MapBuiltinToGLSLstd450(callBuiltin.BuiltinName);

        if (glslInstruction != 0)
        {
            var resultId = _builder.AddExtInst(resultType, _glslStd450Id, glslInstruction, callBuiltin.ArgumentIds);
            _variableIds[callBuiltin.ResultId] = resultId;
        }
    }

    private void EmitConvert(ConvertInstruction convert)
    {
        var resultType = MapType(convert.ResultType);
        var op = convert.ResultType switch
        {
            ShaderIrType.IntType => SpirvConstants.Op.OpConvertFToS,
            ShaderIrType.FloatType => SpirvConstants.Op.OpConvertSToF,
            _ => SpirvConstants.Op.OpConvertFToS
        };
        var resultId = _builder.AddConvert(op, resultType, convert.ValueId);
        _variableIds[convert.ResultId] = resultId;
    }

    #endregion

    #region Private Methods - Type Mapping

    private uint MapType(ShaderIrType? type) => type switch
    {
        ShaderIrType.VoidType => _typeCache.GetVoidType(),
        ShaderIrType.BoolType => _typeCache.GetBoolType(),
        ShaderIrType.IntType i => _typeCache.GetIntType((uint)i.BitWidth, i.Signed),
        ShaderIrType.FloatType f => _typeCache.GetFloatType((uint)f.BitWidth),
        ShaderIrType.VectorType v => _typeCache.GetVectorType(MapType(v.ElementType), (uint)v.ComponentCount),
        ShaderIrType.MatrixType m => _typeCache.GetMatrixType(_typeCache.GetVectorType(MapType(m.ElementType), (uint)m.RowCount), (uint)m.ColumnCount),
        ShaderIrType.StructType s => _typeCache.GetStructType(s.Fields.Select(f => MapType(f.Type)).ToArray(), s.Name),
        ShaderIrType.ImageType img => _typeCache.GetImageType(MapType(img.SampledType), (uint)img.Dim),
        ShaderIrType.SamplerType => _typeCache.GetSamplerType(),
        ShaderIrType.SampledImageType si => _typeCache.GetSampledImageType(MapType(si.Image)),
        ShaderIrType.PointerType p => _typeCache.GetPointerType(MapType(p.PointeeType), MapStorageClass(p.Storage)),
        ShaderIrType.FunctionType fn => _typeCache.GetFunctionType(MapType(fn.ReturnType), fn.ParameterTypes.Select(MapType).ToArray()),
        ShaderIrType.AccelerationStructureType => _typeCache.GetAccelerationStructureType(),
        null => _typeCache.GetVoidType(),
        _ => _typeCache.GetVoidType()
    };

    private uint MapStorageClass(ShaderIR.StorageClass storage) => storage switch
    {
        ShaderIR.StorageClass.UniformConstant => SpirvConstants.StorageClass.UniformConstant,
        ShaderIR.StorageClass.Input => SpirvConstants.StorageClass.Input,
        ShaderIR.StorageClass.Uniform => SpirvConstants.StorageClass.Uniform,
        ShaderIR.StorageClass.Output => SpirvConstants.StorageClass.Output,
        ShaderIR.StorageClass.Function => SpirvConstants.StorageClass.Function,
        ShaderIR.StorageClass.PushConstant => SpirvConstants.StorageClass.PushConstant,
        ShaderIR.StorageClass.StorageBuffer => SpirvConstants.StorageClass.StorageBuffer,
        _ => SpirvConstants.StorageClass.Function
    };

    private uint MapExecutionModel(ShaderExecutionModel model) => model switch
    {
        ShaderExecutionModel.Vertex => SpirvConstants.ExecutionModel.Vertex,
        ShaderExecutionModel.Fragment => SpirvConstants.ExecutionModel.Fragment,
        ShaderExecutionModel.GLCompute => SpirvConstants.ExecutionModel.GLCompute,
        ShaderExecutionModel.RayGenerationKHR => SpirvConstants.ExecutionModel.RayGenerationKHR,
        ShaderExecutionModel.ClosestHitKHR => SpirvConstants.ExecutionModel.ClosestHitKHR,
        ShaderExecutionModel.MissKHR => SpirvConstants.ExecutionModel.MissKHR,
        ShaderExecutionModel.AnyHitKHR => SpirvConstants.ExecutionModel.AnyHitKHR,
        ShaderExecutionModel.IntersectionKHR => SpirvConstants.ExecutionModel.IntersectionKHR,
        _ => SpirvConstants.ExecutionModel.Vertex
    };

    private uint MapBuiltin(string builtin) => builtin.ToLowerInvariant() switch
    {
        "position" => SpirvConstants.BuiltIn.Position,
        "vertexindex" => SpirvConstants.BuiltIn.VertexIndex,
        "instanceindex" => SpirvConstants.BuiltIn.InstanceIndex,
        "fragcoord" => SpirvConstants.BuiltIn.FragCoord,
        "frontfacing" => SpirvConstants.BuiltIn.FrontFacing,
        "fragdepth" => SpirvConstants.BuiltIn.FragDepth,
        "localinvocationid" => SpirvConstants.BuiltIn.LocalInvocationId,
        "globalinvocationid" => SpirvConstants.BuiltIn.GlobalInvocationId,
        "workgroupid" => SpirvConstants.BuiltIn.WorkgroupId,
        "numworkgroups" => SpirvConstants.BuiltIn.NumWorkgroups,
        "launchidkhr" => SpirvConstants.BuiltIn.LaunchIdKHR,
        "launchsizekhr" => SpirvConstants.BuiltIn.LaunchSizeKHR,
        "worldrayoriginkhr" => SpirvConstants.BuiltIn.WorldRayOriginKHR,
        "worldraydirectionkhr" => SpirvConstants.BuiltIn.WorldRayDirectionKHR,
        "objectrayoriginkhr" => SpirvConstants.BuiltIn.ObjectRayOriginKHR,
        "objectraydirectionkhr" => SpirvConstants.BuiltIn.ObjectRayDirectionKHR,
        "hittkhr" => SpirvConstants.BuiltIn.HitTKHR,
        "hitkindkhr" => SpirvConstants.BuiltIn.HitKindKHR,
        "instancecustomindexkhr" => SpirvConstants.BuiltIn.InstanceCustomIndexKHR,
        _ => 0
    };

    private uint MapBuiltinToGLSLstd450(string name) => name switch
    {
        "abs" => SpirvConstants.GLSLstd450.FAbs,
        "sign" => SpirvConstants.GLSLstd450.FSign,
        "floor" => SpirvConstants.GLSLstd450.Floor,
        "ceil" => SpirvConstants.GLSLstd450.Ceil,
        "round" => SpirvConstants.GLSLstd450.Round,
        "min" => SpirvConstants.GLSLstd450.FMin,
        "max" => SpirvConstants.GLSLstd450.FMax,
        "clamp" => SpirvConstants.GLSLstd450.FClamp,
        "mix" or "lerp" => SpirvConstants.GLSLstd450.FMix,
        "step" => SpirvConstants.GLSLstd450.Step,
        "smoothstep" => SpirvConstants.GLSLstd450.SmoothStep,
        "sin" => SpirvConstants.GLSLstd450.Sin,
        "cos" => SpirvConstants.GLSLstd450.Cos,
        "tan" => SpirvConstants.GLSLstd450.Tan,
        "asin" => SpirvConstants.GLSLstd450.Asin,
        "acos" => SpirvConstants.GLSLstd450.Acos,
        "atan" => SpirvConstants.GLSLstd450.Atan,
        "atan2" => SpirvConstants.GLSLstd450.Atan2,
        "pow" => SpirvConstants.GLSLstd450.Pow,
        "exp" => SpirvConstants.GLSLstd450.Exp,
        "log" => SpirvConstants.GLSLstd450.Log,
        "exp2" => SpirvConstants.GLSLstd450.Exp2,
        "log2" => SpirvConstants.GLSLstd450.Log2,
        "sqrt" => SpirvConstants.GLSLstd450.Sqrt,
        "inversesqrt" => SpirvConstants.GLSLstd450.InverseSqrt,
        "dot" => SpirvConstants.GLSLstd450.Length + 1,
        "cross" => SpirvConstants.GLSLstd450.Cross,
        "normalize" => SpirvConstants.GLSLstd450.Normalize,
        "length" => SpirvConstants.GLSLstd450.Length,
        "reflect" => SpirvConstants.GLSLstd450.Reflect,
        "refract" => SpirvConstants.GLSLstd450.Refract,
        "determinant" => SpirvConstants.GLSLstd450.Determinant,
        "matrixinverse" => SpirvConstants.GLSLstd450.MatrixInverse,
        _ => 0
    };

    private static bool IsFloatType(ShaderIrType? type) => type is ShaderIrType.FloatType or ShaderIrType.VectorType { ElementType: ShaderIrType.FloatType };

    #endregion
}
