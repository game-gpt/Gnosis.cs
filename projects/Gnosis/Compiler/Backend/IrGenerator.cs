using Gnosis.Compiler.Backend.ShaderIR;
using Gnosis.Compiler.Diagnostics;
using Gnosis.Compiler.ValueObjects.AST;

namespace Gnosis.Compiler.Backend;

public sealed class IrGenerator : IAstVisitor<ShaderIrInstruction?>
{
    #region Fields

    private readonly DiagnosticSink _diagnostics;
    private readonly List<ShaderFunctionIr> _functions = new();
    private readonly List<ShaderStructIr> _structs = new();
    private readonly List<ShaderGlobalVariableIr> _globals = new();
    private readonly List<ShaderEntryPointIr> _entryPoints = new();
    private readonly List<ExternalFunctionRef> _externalFunctions = new();
    private readonly Dictionary<string, ShaderIrType> _typeMap = new();
    private readonly Dictionary<string, uint> _variableMap = new();
    private readonly List<ShaderIrInstruction> _currentInstructions = new();
    private readonly List<LocalVariableInstruction> _currentLocals = new();
    private readonly List<string> _currentInterfaceVars = new();
    private uint _nextId = 1;

    #endregion

    #region Constructors

    public IrGenerator(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
    }

    #endregion

    #region Public Methods

    public ShaderModuleIr Generate(CompilationUnit ast)
    {
        _functions.Clear();
        _structs.Clear();
        _globals.Clear();
        _entryPoints.Clear();
        _externalFunctions.Clear();
        _typeMap.Clear();
        _variableMap.Clear();
        _nextId = 1;

        foreach (var decl in ast.Declarations)
        {
            decl.Accept(this);
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

    #region IAstVisitor Implementation

    public ShaderIrInstruction? VisitCompilationUnit(CompilationUnit node)
    {
        foreach (var decl in node.Declarations)
        {
            decl.Accept(this);
        }
        return null;
    }

    public ShaderIrInstruction? VisitComponentDecl(ComponentDecl node)
    {
        var fields = new List<ShaderStructFieldIr>();
        uint offset = 0;

        foreach (var field in node.Fields)
        {
            var fieldType = ResolveType(field.FieldType);
            var alignment = GetAlignment(fieldType);
            offset = (offset + alignment - 1) & ~(alignment - 1);
            fields.Add(new ShaderStructFieldIr(field.Name, fieldType, offset));
            offset += GetTypeSize(fieldType);
        }

        _structs.Add(new ShaderStructIr(node.Name, fields, offset));
        return null;
    }

    public ShaderIrInstruction? VisitFunctionDecl(FunctionDecl node)
    {
        _currentInstructions.Clear();
        _currentLocals.Clear();
        _currentInterfaceVars.Clear();
        _variableMap.Clear();

        var executionModel = GetExecutionModel(node.Attributes);
        var isEntryPoint = executionModel != null;
        var parameters = new List<ShaderIrParameter>();

        foreach (var param in node.Parameters)
        {
            var paramType = ResolveType(param.ParamType);
            var storage = isEntryPoint ? GetParameterStorage(param) : StorageClass.Function;
            var location = GetLocation(param.Attributes);
            var builtin = GetBuiltin(param.Attributes);

            var irParam = new ShaderIrParameter(param.Name, paramType, storage, location, builtin)
            {
                ResultId = AllocateId()
            };
            parameters.Add(irParam);
            _variableMap[param.Name] = irParam.ResultId;
        }

        var returnType = node.ReturnType != null ? ResolveType(node.ReturnType) : new ShaderIrType.VoidType();

        if (node.Body != null)
        {
            node.Body.Accept(this);
        }

        var function = new ShaderFunctionIr(
            node.Name,
            executionModel,
            parameters,
            returnType,
            _currentInstructions.ToList(),
            _currentLocals.ToList(),
            node.Attributes,
            isEntryPoint)
        {
            ResultId = AllocateId()
        };

        _functions.Add(function);

        if (isEntryPoint && executionModel != null)
        {
            _entryPoints.Add(new ShaderEntryPointIr(
                node.Name,
                executionModel.Value,
                _currentInterfaceVars.ToList()));
        }

        return null;
    }

    public ShaderIrInstruction? VisitVariableDecl(VariableDecl node)
    {
        var varType = ResolveType(node.VarType);
        var varId = AllocateId();
        _variableMap[node.Name] = varId;

        ShaderIrInstruction? initValue = null;
        if (node.Initializer != null)
        {
            initValue = node.Initializer.Accept(this);
        }

        var local = new LocalVariableInstruction(varType, node.Name, initValue?.ResultId ?? 0)
        {
            ResultId = varId
        };
        _currentLocals.Add(local);

        if (initValue != null)
        {
            _currentInstructions.Add(new StoreInstruction(varId, initValue.ResultId));
        }

        return null;
    }

    public ShaderIrInstruction? VisitBlockStmt(BlockStmt node)
    {
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this);
        }
        return null;
    }

    public ShaderIrInstruction? VisitIfStmt(IfStmt node)
    {
        var condition = node.Condition.Accept(this);
        if (condition == null)
        {
            return null;
        }

        var thenLabelId = AllocateId();
        var elseLabelId = AllocateId();
        var mergeLabelId = AllocateId();

        _currentInstructions.Add(new SelectionMergeInstruction(mergeLabelId));
        _currentInstructions.Add(new BranchConditionalInstruction(condition.ResultId, thenLabelId, elseLabelId));

        _currentInstructions.Add(new LabelInstruction(thenLabelId));
        if (node.ThenBlock != null)
        {
            node.ThenBlock.Accept(this);
        }
        _currentInstructions.Add(new BranchInstruction(mergeLabelId));

        _currentInstructions.Add(new LabelInstruction(elseLabelId));
        if (node.ElseBlock != null)
        {
            node.ElseBlock.Accept(this);
        }
        _currentInstructions.Add(new BranchInstruction(mergeLabelId));

        _currentInstructions.Add(new LabelInstruction(mergeLabelId));
        return null;
    }

    public ShaderIrInstruction? VisitReturnStmt(ReturnStmt node)
    {
        if (node.Value != null)
        {
            var value = node.Value.Accept(this);
            if (value != null)
            {
                _currentInstructions.Add(new ReturnInstruction(value.ResultId));
                return value;
            }
        }

        _currentInstructions.Add(new ReturnInstruction());
        return null;
    }

    public ShaderIrInstruction? VisitExprStmt(ExprStmt node)
    {
        return node.Expression.Accept(this);
    }

    public ShaderIrInstruction? VisitBinaryExpr(BinaryExpr node)
    {
        var left = node.Left.Accept(this);
        var right = node.Right.Accept(this);
        if (left == null || right == null)
        {
            return null;
        }

        var resultType = left.ResultType ?? new ShaderIrType.FloatType();
        var opCode = MapBinaryOp(node.Operator);
        var instruction = new ArithmeticInstruction(opCode, resultType, left.ResultId, right.ResultId)
        {
            ResultId = AllocateId(),
            ResultType = resultType
        };
        _currentInstructions.Add(instruction);
        return instruction;
    }

    public ShaderIrInstruction? VisitUnaryExpr(UnaryExpr node)
    {
        var operand = node.Operand.Accept(this);
        if (operand == null)
        {
            return null;
        }

        var resultType = operand.ResultType ?? new ShaderIrType.FloatType();

        if (node.Operator == "-")
        {
            var instruction = new ArithmeticInstruction(ShaderIrOpCode.Negate, resultType, operand.ResultId, 0)
            {
                ResultId = AllocateId(),
                ResultType = resultType
            };
            _currentInstructions.Add(instruction);
            return instruction;
        }

        if (node.Operator == "!")
        {
            var instruction = new LogicalInstruction(ShaderIrOpCode.LogicalNot, resultType, operand.ResultId, 0)
            {
                ResultId = AllocateId(),
                ResultType = resultType
            };
            _currentInstructions.Add(instruction);
            return instruction;
        }

        return operand;
    }

    public ShaderIrInstruction? VisitCallExpr(CallExpr node)
    {
        if (node.Callee is IdentifierExpr ident)
        {
            var builtinName = ident.Name;
            if (IsBuiltinFunction(builtinName))
            {
                var args = new List<uint>();
                foreach (var arg in node.Arguments)
                {
                    var argResult = arg.Accept(this);
                    if (argResult != null)
                    {
                        args.Add(argResult.ResultId);
                    }
                }

                var resultType = InferBuiltinReturnType(builtinName, args.Count);
                var instruction = new CallBuiltinInstruction(resultType, builtinName, args.ToArray())
                {
                    ResultId = AllocateId(),
                    ResultType = resultType
                };
                _currentInstructions.Add(instruction);
                return instruction;
            }

            if (IsExternalFunction(builtinName))
            {
                var args = new List<uint>();
                foreach (var arg in node.Arguments)
                {
                    var argResult = arg.Accept(this);
                    if (argResult != null)
                    {
                        args.Add(argResult.ResultId);
                    }
                }

                var resultType = new ShaderIrType.VectorType(new ShaderIrType.FloatType(), 4);
                var instruction = new CallInstruction(resultType, builtinName, args.ToArray())
                {
                    ResultId = AllocateId(),
                    ResultType = resultType
                };
                _currentInstructions.Add(instruction);
                return instruction;
            }
        }

        var callArgs = new List<uint>();
        foreach (var arg in node.Arguments)
        {
            var argResult = arg.Accept(this);
            if (argResult != null)
            {
                callArgs.Add(argResult.ResultId);
            }
        }

        var calleeName = node.Callee is IdentifierExpr id ? id.Name : "unknown";
        var callResultType = new ShaderIrType.VoidType();
        var callInstruction = new CallInstruction(callResultType, calleeName, callArgs.ToArray())
        {
            ResultId = AllocateId(),
            ResultType = callResultType
        };
        _currentInstructions.Add(callInstruction);
        return callInstruction;
    }

    public ShaderIrInstruction? VisitMemberAccessExpr(MemberAccessExpr node)
    {
        var obj = node.Object.Accept(this);
        if (obj == null)
        {
            return null;
        }

        if (node.MemberName == "x" || node.MemberName == "y" || node.MemberName == "z" || node.MemberName == "w")
        {
            var componentIndex = node.MemberName switch
            {
                "x" => 0, "y" => 1, "z" => 2, "w" => 3, _ => 0
            };
            var resultType = new ShaderIrType.FloatType();
            var instruction = new CompositeExtractInstruction(resultType, obj.ResultId, new[] { componentIndex })
            {
                ResultId = AllocateId(),
                ResultType = resultType
            };
            _currentInstructions.Add(instruction);
            return instruction;
        }

        if (node.MemberName == "xy" || node.MemberName == "xyz" || node.MemberName == "yz" || node.MemberName == "zw")
        {
            var components = node.MemberName.Select(c => c - 'x').ToArray();
            var resultType = new ShaderIrType.VectorType(new ShaderIrType.FloatType(), components.Length);
            var instruction = new VectorSwizzleInstruction(resultType, obj.ResultId, components)
            {
                ResultId = AllocateId(),
                ResultType = resultType
            };
            _currentInstructions.Add(instruction);
            return instruction;
        }

        return obj;
    }

    public ShaderIrInstruction? VisitLiteralExpr(LiteralExpr node)
    {
        var (type, value) = node.Value switch
        {
            bool b => (new ShaderIrType.BoolType() as ShaderIrType, (uint)(b ? 1 : 0)),
            int i => (new ShaderIrType.IntType() as ShaderIrType, (uint)i),
            float f => (new ShaderIrType.FloatType() as ShaderIrType, BitConverter.SingleToUInt32Bits(f)),
            _ => (new ShaderIrType.IntType() as ShaderIrType, 0u)
        };

        var instruction = new LoadInstruction(type, 0)
        {
            ResultId = AllocateId(),
            ResultType = type
        };
        _currentInstructions.Add(instruction);
        return instruction;
    }

    public ShaderIrInstruction? VisitIdentifierExpr(IdentifierExpr node)
    {
        if (_variableMap.TryGetValue(node.Name, out var varId))
        {
            var type = new ShaderIrType.FloatType();
            var instruction = new LoadInstruction(type, varId)
            {
                ResultId = AllocateId(),
                ResultType = type
            };
            _currentInstructions.Add(instruction);
            return instruction;
        }

        return null;
    }

    public ShaderIrInstruction? VisitAssignmentExpr(AssignmentExpr node)
    {
        var value = node.Value.Accept(this);
        if (value == null)
        {
            return null;
        }

        if (node.Target is IdentifierExpr ident && _variableMap.TryGetValue(ident.Name, out var varId))
        {
            _currentInstructions.Add(new StoreInstruction(varId, value.ResultId));
        }

        return value;
    }

    public ShaderIrInstruction? VisitImportDecl(ImportDecl node) => null;
    public ShaderIrInstruction? VisitFieldDecl(FieldDecl node) => null;
    public ShaderIrInstruction? VisitParameterDecl(ParameterDecl node) => null;
    public ShaderIrInstruction? VisitTypeAnnotation(TypeAnnotation node) => null;
    public ShaderIrInstruction? VisitAttributeDecl(AttributeDecl node) => null;
    public ShaderIrInstruction? VisitQueryExpr(QueryExpr node) => null;
    public ShaderIrInstruction? VisitMetaBlock(MetaBlock node) => null;
    public ShaderIrInstruction? VisitSystemDecl(SystemDecl node) => null;
    public ShaderIrInstruction? VisitWidgetDecl(WidgetDecl node) => null;
    public ShaderIrInstruction? VisitSceneDecl(SceneDecl node) => null;
    public ShaderIrInstruction? VisitPluginDecl(PluginDecl node) => null;
    public ShaderIrInstruction? VisitLoopStmt(LoopStmt node) => null;
    public ShaderIrInstruction? VisitWhileStmt(WhileStmt node) => null;
    public ShaderIrInstruction? VisitIndexExpr(IndexExpr node) => null;
    public ShaderIrInstruction? VisitLambdaExpr(LambdaExpr node) => null;
    public ShaderIrInstruction? VisitStructDecl(StructDecl node) => null;
    public ShaderIrInstruction? VisitForStmt(ForStmt node) => null;
    public ShaderIrInstruction? VisitDiscardStmt(DiscardStmt node) => null;
    public ShaderIrInstruction? VisitSwizzleExpr(SwizzleExpr node) => null;
    public ShaderIrInstruction? VisitUsingDecl(UsingDecl node) => null;
    public ShaderIrInstruction? VisitUniformBindingDecl(UniformBindingDecl node) => null;

    #endregion

    #region Private Methods

    private uint AllocateId() => _nextId++;

    private ShaderIrType ResolveType(TypeAnnotation? typeAnnotation)
    {
        if (typeAnnotation == null)
        {
            return new ShaderIrType.VoidType();
        }

        var name = typeAnnotation.Name;
        var generics = typeAnnotation.GenericArguments;

        return name switch
        {
            "void" => new ShaderIrType.VoidType(),
            "bool" => new ShaderIrType.BoolType(),
            "i32" => new ShaderIrType.IntType(32, true),
            "u32" => new ShaderIrType.IntType(32, false),
            "f32" => new ShaderIrType.FloatType(32),
            "f64" => new ShaderIrType.FloatType(64),
            "vec2" => new ShaderIrType.VectorType(ResolveType(generics.FirstOrDefault()), 2),
            "vec3" => new ShaderIrType.VectorType(ResolveType(generics.FirstOrDefault()), 3),
            "vec4" => new ShaderIrType.VectorType(ResolveType(generics.FirstOrDefault()), 4),
            "mat2" => new ShaderIrType.MatrixType(new ShaderIrType.FloatType(), 2, 2),
            "mat3" => new ShaderIrType.MatrixType(new ShaderIrType.FloatType(), 3, 3),
            "mat4" => new ShaderIrType.MatrixType(new ShaderIrType.FloatType(), 4, 4),
            "texture_2d" => new ShaderIrType.ImageType(new ShaderIrType.FloatType(), 1, 0, false, false, 0),
            "sampler" => new ShaderIrType.SamplerType(),
            "image_2d" => new ShaderIrType.ImageType(new ShaderIrType.FloatType(), 1, 0, false, false, 0),
            _ when _structs.Any(s => s.Name == name) => new ShaderIrType.StructType(name, _structs.First(s => s.Name == name).Fields),
            _ => new ShaderIrType.VoidType()
        };
    }

    private ShaderExecutionModel? GetExecutionModel(IReadOnlyList<AttributeDecl> attributes)
    {
        foreach (var attr in attributes)
        {
            switch (attr.Name)
            {
                case "Vertex": return ShaderExecutionModel.Vertex;
                case "Fragment": return ShaderExecutionModel.Fragment;
                case "Compute": return ShaderExecutionModel.GLCompute;
                case "RayGen": return ShaderExecutionModel.RayGenerationKHR;
                case "ClosestHit": return ShaderExecutionModel.ClosestHitKHR;
                case "Miss": return ShaderExecutionModel.MissKHR;
                case "AnyHit": return ShaderExecutionModel.AnyHitKHR;
                case "Intersection": return ShaderExecutionModel.IntersectionKHR;
            }
        }
        return null;
    }

    private StorageClass GetParameterStorage(ParameterDecl param)
    {
        var builtin = GetBuiltin(param.Attributes);
        if (builtin != null)
        {
            return StorageClass.Input;
        }

        var location = GetLocation(param.Attributes);
        if (location != null)
        {
            return StorageClass.Input;
        }

        return StorageClass.Function;
    }

    private uint? GetLocation(IReadOnlyList<AttributeDecl> attributes)
    {
        foreach (var attr in attributes)
        {
            if (attr.Name == "Location" && attr.Arguments.Count > 0)
            {
                if (uint.TryParse(attr.Arguments[0].Value, out var loc))
                {
                    return loc;
                }
            }
        }
        return null;
    }

    private string? GetBuiltin(IReadOnlyList<AttributeDecl> attributes)
    {
        foreach (var attr in attributes)
        {
            if (attr.Name == "Builtin" && attr.Arguments.Count > 0)
            {
                return attr.Arguments[0].Value;
            }
        }
        return null;
    }

    private ShaderIrOpCode MapBinaryOp(string op) => op switch
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
        _ => ShaderIrOpCode.Add
    };

    private static bool IsBuiltinFunction(string name) =>
        name is "abs" or "sign" or "floor" or "ceil" or "round"
            or "min" or "max" or "clamp" or "mix" or "lerp"
            or "step" or "smoothstep" or "sin" or "cos" or "tan"
            or "pow" or "exp" or "log" or "sqrt" or "inversesqrt"
            or "dot" or "cross" or "normalize" or "length"
            or "reflect" or "refract" or "textureSample" or "textureLoad" or "textureStore";

    private static bool IsExternalFunction(string name) =>
        name.StartsWith("evaluate_") || name.StartsWith("denoise_");

    private static ShaderIrType InferBuiltinReturnType(string name, int argCount) => name switch
    {
        "dot" or "length" => new ShaderIrType.FloatType(),
        "cross" => new ShaderIrType.VectorType(new ShaderIrType.FloatType(), 3),
        "normalize" => new ShaderIrType.VectorType(new ShaderIrType.FloatType(), argCount > 0 ? 3 : 4),
        "textureSample" or "textureLoad" => new ShaderIrType.VectorType(new ShaderIrType.FloatType(), 4),
        "abs" or "sign" or "floor" or "ceil" or "round"
            or "min" or "max" or "clamp" or "mix" or "lerp"
            or "step" or "smoothstep" or "sin" or "cos" or "tan"
            or "pow" or "exp" or "log" or "sqrt" or "inversesqrt"
            or "reflect" or "refract" => new ShaderIrType.FloatType(),
        _ => new ShaderIrType.VoidType()
    };

    private static uint GetAlignment(ShaderIrType type) => type switch
    {
        ShaderIrType.BoolType => 4,
        ShaderIrType.IntType => 4,
        ShaderIrType.FloatType => 4,
        ShaderIrType.VectorType v => v.ComponentCount == 3 ? 16u : (uint)(v.ComponentCount * 4),
        ShaderIrType.MatrixType m => (uint)(m.ColumnCount * 4 * m.RowCount),
        ShaderIrType.StructType s => s.Fields.Count > 0 ? GetAlignment(s.Fields[0].Type) : 16u,
        _ => 4u
    };

    private static uint GetTypeSize(ShaderIrType type) => type switch
    {
        ShaderIrType.BoolType => 4,
        ShaderIrType.IntType => 4,
        ShaderIrType.FloatType f => (uint)(f.BitWidth / 8),
        ShaderIrType.VectorType v => (uint)(v.ComponentCount * 4),
        ShaderIrType.MatrixType m => (uint)(m.ColumnCount * m.RowCount * 4),
        ShaderIrType.StructType s => s.Fields.Count > 0 ? s.Fields.Max(f => f.Offset + GetTypeSize(f.Type)) : 0,
        _ => 4u
    };

    #endregion
}
