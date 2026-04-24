using Oak.Diagnostics;
using Oak.Valkyrie.AST;
using Gnosis.IR.Shader;

namespace Gnosis.Toolchain.ShaderCompiler.Backend;

public sealed class ShaderAstLowering
{
    #region Fields

    private readonly DiagnosticSink _diagnostics;
    private readonly List<ShaderStructIr> _structs = [];
    private readonly List<ShaderGlobalVariableIr> _globals = [];
    private readonly List<ShaderFunctionIr> _functions = [];
    private readonly List<ShaderEntryPointIr> _entryPoints = [];
    private readonly List<ShaderResourceIr> _resources = [];
    private readonly List<ExternalFunctionRef> _externalFunctions = [];
    private readonly Dictionary<string, ShaderIrType> _typeMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, uint> _variableIds = new(StringComparer.Ordinal);
    private readonly List<ShaderIrInstruction> _currentInstructions = [];
    private readonly List<LocalVariableInstruction> _currentLocals = [];
    private readonly List<string> _currentInterfaceVars = [];
    private uint _nextId = 1;

    #endregion

    #region Constructors

    public ShaderAstLowering(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
    }

    #endregion

    #region Public Methods

    public ShaderModuleIr Lower(CompilationUnit ast)
    {
        _structs.Clear();
        _globals.Clear();
        _functions.Clear();
        _entryPoints.Clear();
        _resources.Clear();
        _externalFunctions.Clear();
        _typeMap.Clear();
        _variableIds.Clear();
        _nextId = 1;

        RegisterBuiltinTypes();
        CollectTypeInfo(ast);

        foreach (var decl in ast.Declarations)
        {
            LowerDeclaration(decl);
        }

        return new ShaderModuleIr(
            ast.FilePath ?? "unknown",
            _functions.ToList(),
            _structs.ToList(),
            _globals.ToList(),
            _entryPoints.ToList(),
            _externalFunctions.ToList());
    }

    #endregion

    #region 类型注册

    private void RegisterBuiltinTypes()
    {
        _typeMap["void"] = ShaderIrType.Void;
        _typeMap["bool"] = ShaderIrType.Bool;
        _typeMap["f32"] = ShaderIrType.Float32;
        _typeMap["f64"] = ShaderIrType.Float64;
        _typeMap["i32"] = ShaderIrType.Int32;
        _typeMap["u32"] = ShaderIrType.UInt32;
        _typeMap["i64"] = ShaderIrType.Int64;
        _typeMap["u64"] = ShaderIrType.UInt64;
        _typeMap["vec2"] = ShaderIrType.Vec2();
        _typeMap["vec3"] = ShaderIrType.Vec3();
        _typeMap["vec4"] = ShaderIrType.Vec4();
        _typeMap["mat3"] = ShaderIrType.Mat3();
        _typeMap["mat4"] = ShaderIrType.Mat4();
        _typeMap["sampler"] = new ShaderIrType.SamplerType();
        _typeMap["acceleration_structure"] = new ShaderIrType.AccelerationStructureType();
    }

    private void CollectTypeInfo(CompilationUnit ast)
    {
        foreach (var decl in ast.Declarations)
        {
            switch (decl)
            {
                case StructDecl structDecl:
                    CollectStructType(structDecl);
                    break;
                case ComponentDecl compDecl:
                    CollectCbufferType(compDecl);
                    break;
            }
        }
    }

    private void CollectStructType(StructDecl decl)
    {
        var fields = new List<ShaderStructFieldIr>();
        uint offset = 0;

        foreach (var field in decl.Fields)
        {
            var fieldType = ResolveType(field.FieldType);
            var alignment = GetAlignment(fieldType);
            offset = (offset + alignment - 1) & ~(alignment - 1);
            fields.Add(new ShaderStructFieldIr
            {
                Name = field.Name,
                Type = fieldType,
                Offset = offset
            });
            offset += GetTypeSize(fieldType);
        }

        var structType = new ShaderIrType.StructType(decl.Name, fields);
        _typeMap[decl.Name] = structType;
    }

    private void CollectCbufferType(ComponentDecl decl)
    {
        var fields = new List<ShaderStructFieldIr>();
        uint offset = 0;

        foreach (var field in decl.Fields)
        {
            var fieldType = ResolveType(field.FieldType);
            var alignment = GetAlignment(fieldType);
            offset = (offset + alignment - 1) & ~(alignment - 1);
            fields.Add(new ShaderStructFieldIr
            {
                Name = field.Name,
                Type = fieldType,
                Offset = offset
            });
            offset += GetTypeSize(fieldType);
        }

        var structType = new ShaderIrType.StructType(decl.Name, fields);
        _typeMap[decl.Name] = structType;

        _structs.Add(new ShaderStructIr
        {
            Name = decl.Name,
            Fields = fields,
            Size = offset
        });
    }

    #endregion

    #region 声明降低

    private void LowerDeclaration(AstNode decl)
    {
        switch (decl)
        {
            case ShaderDecl shaderDecl:
                LowerShaderDecl(shaderDecl);
                break;
            case FunctionDecl funcDecl:
                LowerFunctionDecl(funcDecl);
                break;
            case StructDecl structDecl:
                LowerStructDecl(structDecl);
                break;
            case ComponentDecl compDecl:
                LowerCbufferDecl(compDecl);
                break;
            case UniformDecl uniformDecl:
                LowerUniformDecl(uniformDecl);
                break;
            case VaryingDecl varyingDecl:
                LowerVaryingDecl(varyingDecl);
                break;
            case ConstantBufferDecl cbufferDecl:
                LowerConstantBufferDecl(cbufferDecl);
                break;
            case TextureDecl textureDecl:
                LowerTextureDecl(textureDecl);
                break;
            case SamplerDecl samplerDecl:
                LowerSamplerDecl(samplerDecl);
                break;
            case ShaderAttributeDecl attrDecl:
                LowerShaderAttributeDecl(attrDecl);
                break;
            case UniformBindingDecl bindingDecl:
                LowerUniformBindingDecl(bindingDecl);
                break;
            case ImportDecl importDecl:
                break;
        }
    }

    private void LowerShaderDecl(ShaderDecl decl)
    {
        foreach (var stage in decl.Stages)
        {
            LowerShaderStageDecl(decl.Name, stage);
        }
    }

    private void LowerShaderStageDecl(string shaderName, ShaderStageDecl stage)
    {
        var executionModel = stage switch
        {
            VertexShaderDecl => ShaderExecutionModel.Vertex,
            FragmentShaderDecl => ShaderExecutionModel.Fragment,
            ComputeShaderDecl => ShaderExecutionModel.GLCompute,
            _ => ShaderExecutionModel.Vertex
        };

        var funcName = $"{shaderName}_{stage.Name}";
        _entryPoints.Add(new ShaderEntryPointIr
        {
            Name = stage.Name,
            ExecutionModel = executionModel,
            FunctionName = funcName,
            InterfaceVariables = _currentInterfaceVars.ToList()
        });

        _currentInstructions.Clear();
        _currentLocals.Clear();
        _variableIds.Clear();
        _nextId = 1;

        foreach (var stmt in stage.Body)
        {
            LowerStatement(stmt);
        }

        _functions.Add(new ShaderFunctionIr
        {
            Name = funcName,
            ReturnType = ShaderIrType.Vec4(),
            IsEntryPoint = true,
            EntryPointModel = executionModel,
            Instructions = _currentInstructions.ToList(),
            LocalVariables = _currentLocals.ToList()
        });
    }

    private void LowerFunctionDecl(FunctionDecl decl)
    {
        var returnType = decl.ReturnType is not null
            ? ResolveType(decl.ReturnType)
            : ShaderIrType.Void;

        var isEntryPoint = false;
        ShaderExecutionModel? entryModel = null;

        foreach (var attr in decl.Attributes)
        {
            switch (attr.Name)
            {
                case "Vertex":
                    isEntryPoint = true;
                    entryModel = ShaderExecutionModel.Vertex;
                    break;
                case "Fragment":
                    isEntryPoint = true;
                    entryModel = ShaderExecutionModel.Fragment;
                    break;
                case "Compute":
                    isEntryPoint = true;
                    entryModel = ShaderExecutionModel.GLCompute;
                    break;
                case "RayGen":
                    isEntryPoint = true;
                    entryModel = ShaderExecutionModel.RayGenerationKHR;
                    break;
                case "ClosestHit":
                    isEntryPoint = true;
                    entryModel = ShaderExecutionModel.ClosestHitKHR;
                    break;
                case "Miss":
                    isEntryPoint = true;
                    entryModel = ShaderExecutionModel.MissKHR;
                    break;
                case "External":
                    LowerExternalFunction(decl);
                    return;
            }
        }

        _currentInstructions.Clear();
        _currentLocals.Clear();
        _variableIds.Clear();
        _nextId = 1;

        var parameters = new List<ShaderIrParameter>();
        foreach (var param in decl.Parameters)
        {
            var paramType = ResolveType(param.ParamType);
            var paramId = AllocateId();
            _variableIds[param.Name] = paramId;

            parameters.Add(new ShaderIrParameter
            {
                Name = param.Name,
                Type = paramType,
                ResultId = paramId
            });
        }

        if (decl.Body is not null)
        {
            LowerBlock(decl.Body);
        }

        var funcIr = new ShaderFunctionIr
        {
            Name = decl.Name,
            ReturnType = returnType,
            Parameters = parameters,
            Instructions = _currentInstructions.ToList(),
            LocalVariables = _currentLocals.ToList(),
            IsEntryPoint = isEntryPoint,
            EntryPointModel = entryModel
        };

        foreach (var attr in decl.Attributes)
        {
            funcIr.Attributes.Add(new ShaderAttributeIr
            {
                Name = attr.Name,
                Arguments = attr.Arguments.Select(a => a.Value).ToList()
            });
        }

        _functions.Add(funcIr);

        if (isEntryPoint && entryModel is not null)
        {
            _entryPoints.Add(new ShaderEntryPointIr
            {
                Name = decl.Name,
                ExecutionModel = entryModel.Value,
                FunctionName = decl.Name,
                InterfaceVariables = _currentInterfaceVars.ToList()
            });
        }
    }

    private void LowerExternalFunction(FunctionDecl decl)
    {
        var returnType = decl.ReturnType is not null
            ? ResolveType(decl.ReturnType)
            : ShaderIrType.Void;

        _externalFunctions.Add(new ExternalFunctionRef
        {
            Name = decl.Name,
            ReturnType = returnType,
            ParameterTypes = decl.Parameters.Select(p => ResolveType(p.ParamType)).ToList()
        });
    }

    private void LowerStructDecl(StructDecl decl)
    {
        if (_typeMap.ContainsKey(decl.Name))
        {
            return;
        }

        CollectStructType(decl);

        if (_typeMap[decl.Name] is ShaderIrType.StructType structType)
        {
            _structs.Add(new ShaderStructIr
            {
                Name = decl.Name,
                Fields = structType.Fields,
                Size = structType.Fields.Sum(f => GetTypeSize(f.Type))
            });
        }
    }

    private void LowerCbufferDecl(ComponentDecl decl)
    {
        var bindingAttr = decl.Attributes.FirstOrDefault(a => a.Name == "Binding");
        uint descriptorSet = 0;
        uint binding = 0;

        if (bindingAttr is not null)
        {
            foreach (var arg in bindingAttr.Arguments)
            {
                if (arg.Key == "set" && uint.TryParse(arg.Value, out var ds))
                {
                    descriptorSet = ds;
                }
                else if (arg.Key == "binding" && uint.TryParse(arg.Value, out var b))
                {
                    binding = b;
                }
            }
        }

        _resources.Add(new ShaderResourceIr
        {
            Name = decl.Name,
            Kind = ShaderResourceKind.UniformBuffer,
            Type = _typeMap.GetValueOrDefault(decl.Name, ShaderIrType.Void),
            DescriptorSet = descriptorSet,
            Binding = binding
        });

        var globalId = AllocateId();
        _globals.Add(new ShaderGlobalVariableIr
        {
            Name = decl.Name,
            Type = _typeMap.GetValueOrDefault(decl.Name, ShaderIrType.Void),
            Storage = StorageClass.Uniform,
            ResultId = globalId,
            Resource = new ShaderResourceIr
            {
                Name = decl.Name,
                Kind = ShaderResourceKind.UniformBuffer,
                DescriptorSet = descriptorSet,
                Binding = binding
            }
        });
    }

    private void LowerUniformDecl(UniformDecl decl)
    {
        var valueType = ResolveType(decl.ValueType);
        var globalId = AllocateId();
        _variableIds[decl.Name] = globalId;

        var bindingAttr = decl.Attributes.FirstOrDefault(a => a.Name == "Binding");
        uint descriptorSet = 0;
        uint binding = 0;

        if (bindingAttr is not null)
        {
            foreach (var arg in bindingAttr.Arguments)
            {
                if (arg.Key == "set" && uint.TryParse(arg.Value, out var ds))
                {
                    descriptorSet = ds;
                }
                else if (arg.Key == "binding" && uint.TryParse(arg.Value, out var b))
                {
                    binding = b;
                }
            }
        }

        _globals.Add(new ShaderGlobalVariableIr
        {
            Name = decl.Name,
            Type = valueType,
            Storage = StorageClass.UniformConstant,
            ResultId = globalId,
            Resource = new ShaderResourceIr
            {
                Name = decl.Name,
                Kind = ShaderResourceKind.UniformBuffer,
                DescriptorSet = descriptorSet,
                Binding = binding
            }
        });
    }

    private void LowerVaryingDecl(VaryingDecl decl)
    {
        var valueType = ResolveType(decl.ValueType);
        var globalId = AllocateId();
        _variableIds[decl.Name] = globalId;

        var locationAttr = decl.Attributes.FirstOrDefault(a => a.Name == "Location");
        uint? location = null;
        if (locationAttr is not null)
        {
            foreach (var arg in locationAttr.Arguments)
            {
                if (uint.TryParse(arg.Value, out var loc))
                {
                    location = loc;
                }
            }
        }

        var builtinAttr = decl.Attributes.FirstOrDefault(a => a.Name == "Builtin");
        string? builtin = null;
        if (builtinAttr is not null)
        {
            foreach (var arg in builtinAttr.Arguments)
            {
                builtin = arg.Value;
            }
        }

        var isInput = decl.Attributes.Any(a => a.Name == "Input" || a.Name == "In");
        var isOutput = decl.Attributes.Any(a => a.Name == "Output" || a.Name == "Out");
        var storage = isOutput ? StorageClass.Output : StorageClass.Input;

        _globals.Add(new ShaderGlobalVariableIr
        {
            Name = decl.Name,
            Type = valueType,
            Storage = storage,
            ResultId = globalId,
            Location = location,
            Builtin = builtin
        });
    }

    private void LowerConstantBufferDecl(ConstantBufferDecl decl)
    {
        var fields = new List<ShaderStructFieldIr>();
        uint offset = 0;

        foreach (var field in decl.Fields)
        {
            var fieldType = ResolveType(field.FieldType);
            var alignment = GetAlignment(fieldType);
            offset = (offset + alignment - 1) & ~(alignment - 1);
            fields.Add(new ShaderStructFieldIr
            {
                Name = field.Name,
                Type = fieldType,
                Offset = offset
            });
            offset += GetTypeSize(fieldType);
        }

        var structType = new ShaderIrType.StructType(decl.Name, fields);
        _typeMap[decl.Name] = structType;

        _structs.Add(new ShaderStructIr
        {
            Name = decl.Name,
            Fields = fields,
            Size = offset
        });

        var bindingAttr = decl.Attributes.FirstOrDefault(a => a.Name == "Binding");
        uint descriptorSet = 0;
        uint binding = 0;

        if (bindingAttr is not null)
        {
            foreach (var arg in bindingAttr.Arguments)
            {
                if (arg.Key == "set" && uint.TryParse(arg.Value, out var ds))
                {
                    descriptorSet = ds;
                }
                else if (arg.Key == "binding" && uint.TryParse(arg.Value, out var b))
                {
                    binding = b;
                }
            }
        }

        var globalId = AllocateId();
        _variableIds[decl.Name] = globalId;

        _globals.Add(new ShaderGlobalVariableIr
        {
            Name = decl.Name,
            Type = structType,
            Storage = StorageClass.Uniform,
            ResultId = globalId,
            Resource = new ShaderResourceIr
            {
                Name = decl.Name,
                Kind = ShaderResourceKind.UniformBuffer,
                DescriptorSet = descriptorSet,
                Binding = binding
            }
        });
    }

    private void LowerTextureDecl(TextureDecl decl)
    {
        var sampledType = ResolveType(decl.ValueType);
        var imageType = new ShaderIrType.ImageType(sampledType, 1);
        _typeMap[decl.Name] = imageType;

        var globalId = AllocateId();
        _variableIds[decl.Name] = globalId;

        var bindingAttr = decl.Attributes.FirstOrDefault(a => a.Name == "Binding");
        uint descriptorSet = 0;
        uint binding = 0;

        if (bindingAttr is not null)
        {
            foreach (var arg in bindingAttr.Arguments)
            {
                if (arg.Key == "set" && uint.TryParse(arg.Value, out var ds))
                {
                    descriptorSet = ds;
                }
                else if (arg.Key == "binding" && uint.TryParse(arg.Value, out var b))
                {
                    binding = b;
                }
            }
        }

        _globals.Add(new ShaderGlobalVariableIr
        {
            Name = decl.Name,
            Type = imageType,
            Storage = StorageClass.UniformConstant,
            ResultId = globalId,
            Resource = new ShaderResourceIr
            {
                Name = decl.Name,
                Kind = ShaderResourceKind.SampledImage,
                DescriptorSet = descriptorSet,
                Binding = binding
            }
        });
    }

    private void LowerSamplerDecl(SamplerDecl decl)
    {
        var samplerType = new ShaderIrType.SamplerType();
        _typeMap[decl.Name] = samplerType;

        var globalId = AllocateId();
        _variableIds[decl.Name] = globalId;

        var bindingAttr = decl.Attributes.FirstOrDefault(a => a.Name == "Binding");
        uint descriptorSet = 0;
        uint binding = 0;

        if (bindingAttr is not null)
        {
            foreach (var arg in bindingAttr.Arguments)
            {
                if (arg.Key == "set" && uint.TryParse(arg.Value, out var ds))
                {
                    descriptorSet = ds;
                }
                else if (arg.Key == "binding" && uint.TryParse(arg.Value, out var b))
                {
                    binding = b;
                }
            }
        }

        _globals.Add(new ShaderGlobalVariableIr
        {
            Name = decl.Name,
            Type = samplerType,
            Storage = StorageClass.UniformConstant,
            ResultId = globalId,
            Resource = new ShaderResourceIr
            {
                Name = decl.Name,
                Kind = ShaderResourceKind.Sampler,
                DescriptorSet = descriptorSet,
                Binding = binding
            }
        });
    }

    private void LowerShaderAttributeDecl(ShaderAttributeDecl decl)
    {
        var valueType = ResolveType(decl.ValueType);
        var globalId = AllocateId();
        _variableIds[decl.Name] = globalId;

        var locationAttr = decl.Attributes.FirstOrDefault(a => a.Name == "Location");
        uint? location = null;
        if (locationAttr is not null)
        {
            foreach (var arg in locationAttr.Arguments)
            {
                if (uint.TryParse(arg.Value, out var loc))
                {
                    location = loc;
                }
            }
        }

        _globals.Add(new ShaderGlobalVariableIr
        {
            Name = decl.Name,
            Type = valueType,
            Storage = StorageClass.Input,
            ResultId = globalId,
            Location = location
        });
    }

    private void LowerUniformBindingDecl(UniformBindingDecl decl)
    {
        if (decl.Binding is not null)
        {
            _resources.Add(new ShaderResourceIr
            {
                Name = decl.Name,
                Kind = ShaderResourceKind.UniformBuffer,
                DescriptorSet = decl.Group ?? 0,
                Binding = decl.Binding.Value
            });
        }
    }

    #endregion

    #region 语句降低

    private void LowerBlock(BlockStmt block)
    {
        foreach (var stmt in block.Statements)
        {
            LowerStatement(stmt);
        }
    }

    private void LowerStatement(AstNode stmt)
    {
        switch (stmt)
        {
            case VariableDecl varDecl:
                LowerVariableDecl(varDecl);
                break;
            case ReturnStatement returnStmt:
                LowerReturnStmt(returnStmt);
                break;
            case IfStatement ifStmt:
                LowerIfStmt(ifStmt);
                break;
            case WhileStmt whileStmt:
                LowerWhileStmt(whileStmt);
                break;
            case ForStmt forStmt:
                LowerForStmt(forStmt);
                break;
            case DiscardStmt:
                _currentInstructions.Add(new ShaderIrInstruction
                {
                    OpCode = ShaderIrOpCode.Kill
                });
                break;
            case BlockStmt blockStmt:
                LowerBlock(blockStmt);
                break;
            case TermExpressionStatement exprStmt:
                LowerExpression(exprStmt.Expression);
                break;
        }
    }

    private void LowerVariableDecl(VariableDecl decl)
    {
        var varType = ResolveType(decl.VarType);
        var varId = AllocateId();
        _variableIds[decl.Name] = varId;

        _currentLocals.Add(new LocalVariableInstruction
        {
            Name = decl.Name,
            Type = varType,
            ResultType = varType,
            ResultId = varId
        });

        if (decl.Initializer is not null)
        {
            var initId = LowerExpression(decl.Initializer);
            _currentInstructions.Add(new ShaderIrInstruction
            {
                OpCode = ShaderIrOpCode.Store,
                ResultType = varType,
                Operands = [varId, initId]
            });
        }
    }

    private void LowerReturnStmt(ReturnStatement stmt)
    {
        if (stmt.Value is not null)
        {
            var valueId = LowerExpression(stmt.Value);
            _currentInstructions.Add(new ShaderIrInstruction
            {
                OpCode = ShaderIrOpCode.Return,
                ResultType = ShaderIrType.Void,
                Operands = [valueId]
            });
        }
        else
        {
            _currentInstructions.Add(new ShaderIrInstruction
            {
                OpCode = ShaderIrOpCode.Return,
                ResultType = ShaderIrType.Void,
                Operands = []
            });
        }
    }

    private void LowerIfStmt(IfStatement stmt)
    {
        var condId = LowerExpression(stmt.Condition);
        _currentInstructions.Add(new ShaderIrInstruction
        {
            OpCode = ShaderIrOpCode.BranchConditional,
            ResultType = ShaderIrType.Bool,
            Operands = [condId]
        });

        LowerStatement(stmt.ThenBlock);

        if (stmt.ElseBlock is not null)
        {
            LowerStatement(stmt.ElseBlock);
        }
    }

    private void LowerWhileStmt(WhileStmt stmt)
    {
        _currentInstructions.Add(new ShaderIrInstruction
        {
            OpCode = ShaderIrOpCode.LoopMerge,
            ResultType = ShaderIrType.Void,
            Operands = []
        });

        var condId = LowerExpression(stmt.Condition);
        _currentInstructions.Add(new ShaderIrInstruction
        {
            OpCode = ShaderIrOpCode.BranchConditional,
            ResultType = ShaderIrType.Bool,
            Operands = [condId]
        });

        LowerBlock(stmt.Body);
    }

    private void LowerForStmt(ForStmt stmt)
    {
        if (stmt.Initializer is not null)
        {
            LowerStatement(stmt.Initializer);
        }

        _currentInstructions.Add(new ShaderIrInstruction
        {
            OpCode = ShaderIrOpCode.LoopMerge,
            ResultType = ShaderIrType.Void,
            Operands = []
        });

        if (stmt.Condition is not null)
        {
            var condId = LowerExpression(stmt.Condition);
            _currentInstructions.Add(new ShaderIrInstruction
            {
                OpCode = ShaderIrOpCode.BranchConditional,
                ResultType = ShaderIrType.Bool,
                Operands = [condId]
            });
        }

        LowerBlock(stmt.Body);

        if (stmt.Update is not null)
        {
            LowerExpression(stmt.Update);
        }
    }

    #endregion

    #region 表达式降低

    private uint LowerExpression(AstNode expr)
    {
        switch (expr)
        {
            case LiteralExpr litExpr:
                return LowerLiteralExpr(litExpr);
            case IdentifierNode idExpr:
                return LowerIdentifierExpr(idExpr);
            case BinaryExpr binExpr:
                return LowerBinaryExpr(binExpr);
            case TermUnaryExpression unaryExpr:
                return LowerUnaryExpr(unaryExpr);
            case TermCallExpression callExpr:
                return LowerCallExpr(callExpr);
            case MemberAccessExpr memberExpr:
                return LowerMemberAccessExpr(memberExpr);
            case SwizzleExpr swizzleExpr:
                return LowerSwizzleExpr(swizzleExpr);
            case AssignmentExpr assignExpr:
                return LowerAssignmentExpr(assignExpr);
            default:
                return AllocateId();
        }
    }

    private uint LowerLiteralExpr(LiteralExpr expr)
    {
        var resultId = AllocateId();
        ShaderIrType resultType;
        object value;

        switch (expr.LiteralKind)
        {
            case LiteralType.Number:
                if (expr.Value is float f)
                {
                    resultType = ShaderIrType.Float32;
                    value = BitConverter.SingleToUInt32Bits(f);
                }
                else if (expr.Value is double d)
                {
                    resultType = ShaderIrType.Float64;
                    value = BitConverter.DoubleToUInt64Bits(d);
                }
                else if (expr.Value is long l)
                {
                    resultType = ShaderIrType.Int64;
                    value = l;
                }
                else
                {
                    resultType = ShaderIrType.Int32;
                    value = expr.Value ?? 0;
                }
                break;
            case LiteralType.Boolean:
                resultType = ShaderIrType.Bool;
                value = expr.Value is true ? 1u : 0u;
                break;
            default:
                resultType = ShaderIrType.Void;
                value = 0u;
                break;
        }

        _currentInstructions.Add(new ShaderIrInstruction
        {
            OpCode = ShaderIrOpCode.Constant,
            ResultType = resultType,
            Operands = [resultId, value]
        });

        return resultId;
    }

    private uint LowerIdentifierExpr(IdentifierNode node)
    {
        if (_variableIds.TryGetValue(node.Name, out var id))
        {
            return id;
        }

        _diagnostics.AddError("shader", node.Span, "SHD001", $"未定义的标识符: {node.Name}");
        return AllocateId();
    }

    private uint LowerBinaryExpr(BinaryExpr expr)
    {
        var leftId = LowerExpression(expr.Left);
        var rightId = LowerExpression(expr.Right);
        var resultId = AllocateId();

        var opCode = expr.Operator switch
        {
            "+" => ShaderIrOpCode.Add,
            "-" => ShaderIrOpCode.Sub,
            "*" => ShaderIrOpCode.Mul,
            "/" => ShaderIrOpCode.Div,
            "%" => ShaderIrOpCode.Mod,
            "==" => ShaderIrOpCode.Equal,
            "!=" => ShaderIrOpCode.NotEqual,
            "<" => ShaderIrOpCode.LessThan,
            ">" => ShaderIrOpCode.GreaterThan,
            "<=" => ShaderIrOpCode.LessEqual,
            ">=" => ShaderIrOpCode.GreaterEqual,
            "&&" => ShaderIrOpCode.LogicalAnd,
            "||" => ShaderIrOpCode.LogicalOr,
            _ => ShaderIrOpCode.Nop
        };

        var isCompare = expr.Operator is "==" or "!=" or "<" or ">" or "<=" or ">=";
        var isLogical = expr.Operator is "&&" or "||";
        var resultType = isCompare ? ShaderIrType.Bool
            : isLogical ? ShaderIrType.Bool
            : ShaderIrType.Float32;

        _currentInstructions.Add(new ShaderIrInstruction
        {
            OpCode = opCode,
            ResultType = resultType,
            Operands = [resultId, leftId, rightId]
        });

        return resultId;
    }

    private uint LowerUnaryExpr(TermUnaryExpression expr)
    {
        var operandId = LowerExpression(expr.Operand);
        var resultId = AllocateId();

        var opCode = expr.Operator switch
        {
            "-" => ShaderIrOpCode.Negate,
            "!" => ShaderIrOpCode.LogicalNot,
            "~" => ShaderIrOpCode.BitNot,
            _ => ShaderIrOpCode.Nop
        };

        _currentInstructions.Add(new ShaderIrInstruction
        {
            OpCode = opCode,
            ResultType = ShaderIrType.Float32,
            Operands = [resultId, operandId]
        });

        return resultId;
    }

    private uint LowerCallExpr(TermCallExpression expr)
    {
        var argIds = expr.Arguments.Select(LowerExpression).ToArray();
        var resultId = AllocateId();

        if (expr.Callee is IdentifierNode idNode)
        {
            var builtinName = idNode.Name;
            var isBuiltin = IsBuiltinFunction(builtinName);

            if (isBuiltin)
            {
                _currentInstructions.Add(new ShaderIrInstruction
                {
                    OpCode = ShaderIrOpCode.CallBuiltin,
                    ResultType = InferBuiltinReturnType(builtinName),
                    Operands = [resultId, builtinName, ..argIds.Cast<object>()]
                });
            }
            else
            {
                _currentInstructions.Add(new ShaderIrInstruction
                {
                    OpCode = ShaderIrOpCode.Call,
                    ResultType = ShaderIrType.Void,
                    Operands = [resultId, builtinName, ..argIds.Cast<object>()]
                });
            }
        }

        return resultId;
    }

    private uint LowerMemberAccessExpr(MemberAccessExpr expr)
    {
        var objId = LowerExpression(expr.Target);
        var resultId = AllocateId();

        _currentInstructions.Add(new ShaderIrInstruction
        {
            OpCode = ShaderIrOpCode.AccessChain,
            ResultType = ShaderIrType.Void,
            Operands = [resultId, objId, expr.MemberName]
        });

        return resultId;
    }

    private uint LowerSwizzleExpr(SwizzleExpr expr)
    {
        var objId = LowerExpression(expr.Target);
        var resultId = AllocateId();
        var components = ParseSwizzleComponents(expr.Pattern);

        if (components is not null)
        {
            var resultType = components.Length == 1
                ? ShaderIrType.Float32
                : ShaderIrType.Vec4();

            _currentInstructions.Add(new ShaderIrInstruction
            {
                OpCode = ShaderIrOpCode.VectorShuffle,
                ResultType = resultType,
                Operands = [resultId, objId, ..components.Select(c => (uint)c)]
            });
        }

        return resultId;
    }

    private uint LowerAssignmentExpr(AssignmentExpr expr)
    {
        var valueId = LowerExpression(expr.Value);

        if (expr.Target is IdentifierNode idNode && _variableIds.TryGetValue(idNode.Name, out var targetId))
        {
            _currentInstructions.Add(new ShaderIrInstruction
            {
                OpCode = ShaderIrOpCode.Store,
                ResultType = ShaderIrType.Void,
                Operands = [targetId, valueId]
            });
        }
        else if (expr.Target is MemberAccessExpr memberExpr)
        {
            var objId = LowerExpression(memberExpr.Target);
            _currentInstructions.Add(new ShaderIrInstruction
            {
                OpCode = ShaderIrOpCode.Store,
                ResultType = ShaderIrType.Void,
                Operands = [objId, memberExpr.MemberName, valueId]
            });
        }

        return valueId;
    }

    #endregion

    #region 类型解析

    private ShaderIrType ResolveType(TypeAnnotation typeAnnotation)
    {
        if (typeAnnotation is null)
        {
            return ShaderIrType.Void;
        }

        var name = typeAnnotation.Name;

        if (_typeMap.TryGetValue(name, out var mapped))
        {
            return mapped;
        }

        if (name.StartsWith("vec"))
        {
            var dimStr = name[3..];
            if (int.TryParse(dimStr, out var dim) && dim is >= 2 and <= 4)
            {
                var elementType = typeAnnotation.GenericArgs.Count > 0
                    ? ResolveType(typeAnnotation.GenericArgs[0])
                    : ShaderIrType.Float32;
                return new ShaderIrType.VectorType(elementType, dim);
            }
        }

        if (name.StartsWith("mat"))
        {
            var dimStr = name[3..];
            if (int.TryParse(dimStr, out var dim) && dim is >= 2 and <= 4)
            {
                var elementType = typeAnnotation.GenericArgs.Count > 0
                    ? ResolveType(typeAnnotation.GenericArgs[0])
                    : ShaderIrType.Float32;
                return new ShaderIrType.MatrixType(elementType, dim, dim);
            }
        }

        if (name is "texture_2d" or "texture2d")
        {
            return new ShaderIrType.ImageType(ShaderIrType.Float32, 1);
        }

        if (name is "texture_3d" or "texture3d")
        {
            return new ShaderIrType.ImageType(ShaderIrType.Float32, 2);
        }

        if (name is "texture_cube" or "texturecube")
        {
            return new ShaderIrType.ImageType(ShaderIrType.Float32, 3);
        }

        if (name is "sampler" or "sampler_comparison")
        {
            return new ShaderIrType.SamplerType();
        }

        if (name is "acceleration_structure")
        {
            return new ShaderIrType.AccelerationStructureType();
        }

        return ShaderIrType.Void;
    }

    #endregion

    #region 辅助方法

    private uint AllocateId()
    {
        return _nextId++;
    }

    private static uint GetAlignment(ShaderIrType type)
    {
        return type switch
        {
            ShaderIrType.FloatType => 4,
            ShaderIrType.IntType => 4,
            ShaderIrType.BoolType => 4,
            ShaderIrType.VectorType v => (uint)(v.ComponentCount * 4),
            ShaderIrType.MatrixType m => (uint)(m.RowCount * 4),
            ShaderIrType.StructType s => s.Fields.Count > 0 ? GetAlignment(s.Fields[0].Type) : 16,
            _ => 4
        };
    }

    private static uint GetTypeSize(ShaderIrType type)
    {
        return type switch
        {
            ShaderIrType.FloatType f => f.BitWidth / 8,
            ShaderIrType.IntType i => i.BitWidth / 8,
            ShaderIrType.BoolType => 4,
            ShaderIrType.VectorType v => (uint)(v.ComponentCount * GetTypeSize(v.ElementType)),
            ShaderIrType.MatrixType m => (uint)(m.RowCount * m.ColumnCount * GetTypeSize(m.ElementType)),
            ShaderIrType.StructType s => (uint)s.Fields.Sum(f => GetTypeSize(f.Type)),
            _ => 4
        };
    }

    private static bool IsBuiltinFunction(string name)
    {
        return name is
            "abs" or "sign" or "floor" or "ceil" or "round" or
            "min" or "max" or "clamp" or "mix" or "lerp" or
            "step" or "smoothstep" or
            "sin" or "cos" or "tan" or "asin" or "acos" or "atan" or "atan2" or
            "pow" or "exp" or "log" or "exp2" or "log2" or
            "sqrt" or "inversesqrt" or
            "dot" or "cross" or "normalize" or "length" or "distance" or
            "reflect" or "refract" or
            "textureSample" or "textureLoad" or "textureStore" or
            "determinant" or "matrixinverse" or
            "trace_ray" or "acceleration_structure";
    }

    private static ShaderIrType InferBuiltinReturnType(string name)
    {
        return name switch
        {
            "dot" or "length" or "distance" => ShaderIrType.Float32,
            "normalize" or "reflect" or "refract" or
            "abs" or "sign" or "floor" or "ceil" or "round" or
            "min" or "max" or "clamp" or "mix" or "lerp" or
            "step" or "smoothstep" or
            "sin" or "cos" or "tan" or "asin" or "acos" or "atan" or
            "pow" or "exp" or "log" or "exp2" or "log2" or
            "sqrt" or "inversesqrt" => ShaderIrType.Float32,
            "cross" => ShaderIrType.Vec3(),
            "textureSample" or "textureLoad" => ShaderIrType.Vec4(),
            "determinant" => ShaderIrType.Float32,
            "atan2" => ShaderIrType.Float32,
            _ => ShaderIrType.Void
        };
    }

    private static int[]? ParseSwizzleComponents(string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || pattern.Length > 4)
        {
            return null;
        }

        var isXyzw = pattern.All(c => c is 'x' or 'y' or 'z' or 'w');
        var isRgba = pattern.All(c => c is 'r' or 'g' or 'b' or 'a');
        var isStpq = pattern.All(c => c is 's' or 't' or 'p' or 'q');

        if (!isXyzw && !isRgba && !isStpq)
        {
            return null;
        }

        return pattern.Select(c => c switch
        {
            'x' or 'r' or 's' => 0,
            'y' or 'g' or 't' => 1,
            'z' or 'b' or 'p' => 2,
            'w' or 'a' or 'q' => 3,
            _ => 0
        }).ToArray();
    }

    #endregion
}
