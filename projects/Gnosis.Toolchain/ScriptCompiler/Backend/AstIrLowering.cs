using Oak.Diagnostics;
using Oak.GGScript.AST;
using Gnosis.IR.Graph;
using Gnosis.IR.Lowering;

namespace Gnosis.Toolchain.ScriptCompiler.Backend;

public sealed class AstIrLowering
{
    #region 字段

    private readonly DiagnosticSink _diagnostics;
    private LoweringContext _context = null!;
    private IrLowering _lowering = null!;
    private IrModule _module = null!;
    private readonly Dictionary<string, IrType> _typeMap = [];
    private readonly Dictionary<string, ComponentInfo> _componentInfo = [];

    #endregion

    #region 构造函数

    public AstIrLowering(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
    }

    #endregion

    #region 公共方法

    public IrModule Lower(CompilationUnit ast)
    {
        _module = new IrModule(ast.FilePath ?? "main");
        _context = new LoweringContext(_module);
        _lowering = new IrLowering(_context);
        _typeMap.Clear();
        _componentInfo.Clear();

        RegisterBuiltinTypes();
        CollectComponentInfo(ast);

        foreach (var decl in ast.Declarations)
        {
            LowerDeclaration(decl);
        }

        return _module;
    }

    #endregion

    #region 类型注册

    private void RegisterBuiltinTypes()
    {
        _typeMap["i8"] = IrType.I8;
        _typeMap["i16"] = IrType.I16;
        _typeMap["i32"] = IrType.I32;
        _typeMap["i64"] = IrType.I64;
        _typeMap["f32"] = IrType.F32;
        _typeMap["f64"] = IrType.F64;
        _typeMap["bool"] = IrType.Bool;
        _typeMap["void"] = IrType.Void;
        _typeMap["string"] = IrType.String;
        _typeMap["Entity"] = IrType.Entity;
    }

    private void CollectComponentInfo(CompilationUnit ast)
    {
        foreach (var decl in ast.Declarations)
        {
            if (decl is ComponentDecl compDecl)
            {
                var fields = compDecl.Fields.Select(f => (f.Name, MapType(f.FieldType))).ToList();
                var compType = IrType.ComponentOf(compDecl.Name, fields);
                _typeMap[compDecl.Name] = compType;
                _componentInfo[compDecl.Name] = new ComponentInfo(
                    compDecl.Name,
                    compDecl.Attributes.Select(a => a.Name).ToList(),
                    fields);
            }
            else if (decl is StructDecl structDecl)
            {
                var fields = structDecl.Fields.Select(f => (f.Name, MapType(f.FieldType))).ToList();
                _typeMap[structDecl.Name] = IrType.StructOf(structDecl.Name, fields);
            }
        }
    }

    #endregion

    #region 声明降低

    private void LowerDeclaration(AstNode decl)
    {
        switch (decl.Type)
        {
            case NodeType.ComponentDecl:
                LowerComponentDecl((ComponentDecl)decl);
                break;
            case NodeType.SystemDecl:
                LowerSystemDecl((SystemDecl)decl);
                break;
            case NodeType.FunctionDecl:
                LowerFunctionDecl((FunctionDecl)decl);
                break;
            case NodeType.PluginDecl:
                LowerPluginDecl((PluginDecl)decl);
                break;
            case NodeType.ImportDecl:
                _module.AddImport(((ImportDecl)decl).ModulePath);
                break;
            case NodeType.StructDecl:
                LowerStructDecl((StructDecl)decl);
                break;
        }
    }

    private void LowerComponentDecl(ComponentDecl decl)
    {
        var fields = decl.Fields.Select(f => (f.Name, MapType(f.FieldType))).ToList();
        var compType = IrType.ComponentOf(decl.Name, fields);
        _module.AddStructType(compType);
        _module.AddExport(decl.Name);

        if (decl.Attributes.Any(a => a.Name == "Encrypted"))
        {
            GenerateEncryptedAccessors(decl, compType);
        }

        if (decl.Attributes.Any(a => a.Name == "Replicated"))
        {
            GenerateReplicatedMetadata(decl, compType);
        }
    }

    private void LowerSystemDecl(SystemDecl decl)
    {
        _module.AddExport(decl.Name);

        foreach (var method in decl.Methods)
        {
            LowerFunctionDecl(method);
        }
    }

    private void LowerFunctionDecl(FunctionDecl decl)
    {
        var returnType = MapType(decl.ReturnType);
        var parameters = decl.Parameters.Select(p => (p.Name, MapType(p.ParamType))).ToList();
        var attributes = decl.Attributes.Select(a => a.Name).ToList();

        _context.Reset();

        _lowering.LowerFunction(decl.Name, returnType, parameters, () =>
        {
            if (decl.Body is not null)
            {
                LowerBlock(decl.Body);
            }
        }, attributes);

        _module.AddExport(decl.Name);
    }

    private void LowerPluginDecl(PluginDecl decl)
    {
        foreach (var func in decl.Functions)
        {
            LowerFunctionDecl(func);
        }
    }

    private void LowerStructDecl(StructDecl decl)
    {
        var fields = decl.Fields.Select(f => (f.Name, MapType(f.FieldType))).ToList();
        var structType = IrType.StructOf(decl.Name, fields);
        _module.AddStructType(structType);
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
        switch (stmt.Type)
        {
            case NodeType.VariableDecl:
                LowerVariableDecl((VariableDecl)stmt);
                break;
            case NodeType.ExprStmt:
                LowerExpression(((TermExpressionStatement)stmt).Expression);
                break;
            case NodeType.ReturnStmt:
                LowerReturnStmt((ReturnStatement)stmt);
                break;
            case NodeType.IfStmt:
                LowerIfStmt((IfStatement)stmt);
                break;
            case NodeType.WhileStmt:
                LowerWhileStmt((WhileStmt)stmt);
                break;
            case NodeType.ForStmt:
                LowerForStmt((ForStmt)stmt);
                break;
            case NodeType.BlockStmt:
                LowerBlock((BlockStmt)stmt);
                break;
        }
    }

    private void LowerVariableDecl(VariableDecl decl)
    {
        var varType = MapType(decl.FieldType);
        var alloca = _lowering.EmitAlloca(varType, decl.Name);
        _context.BindValue(decl.Name, alloca);

        if (decl.Initializer is not null)
        {
            var initValue = LowerExpression(decl.Initializer);
            _lowering.EmitStore(initValue, alloca);
        }
    }

    private void LowerReturnStmt(ReturnStatement stmt)
    {
        if (stmt.Value is not null)
        {
            var value = LowerExpression(stmt.Value);
            _lowering.EmitReturn(value);
        }
        else
        {
            _lowering.EmitReturn();
        }
    }

    private void LowerIfStmt(IfStatement stmt)
    {
        var condition = LowerExpression(stmt.Condition);

        _lowering.EmitIf(condition, () =>
        {
            LowerStatement(stmt.ThenBlock);
        }, stmt.ElseBlock is not null ? () =>
        {
            LowerStatement(stmt.ElseBlock);
        } : null);
    }

    private void LowerWhileStmt(WhileStmt stmt)
    {
        _lowering.EmitWhile(
            _context.CreateValue(IrType.Bool, "while_cond"),
            () => LowerBlock(stmt.Body),
            () => LowerExpression(stmt.Condition));
    }

    private void LowerForStmt(ForStmt stmt)
    {
        if (stmt.Initializer is not null)
        {
            LowerStatement(stmt.Initializer);
        }

        _lowering.EmitWhile(
            _context.CreateValue(IrType.Bool, "for_cond"),
            () =>
            {
                LowerBlock(stmt.Body);
                if (stmt.Update is not null)
                {
                    LowerExpression(stmt.Update);
                }
            },
            () => stmt.Condition is not null ? LowerExpression(stmt.Condition) : _context.CreateValue(IrType.Bool, "true"));
    }

    #endregion

    #region 表达式降低

    private IrValue LowerExpression(AstNode expr)
    {
        switch (expr.Type)
        {
            case NodeType.LiteralExpr:
                return LowerLiteralExpr((LiteralExpr)expr);
            case NodeType.IdentifierExpr:
                return LowerIdentifierExpr((IdentifierNode)expr);
            case NodeType.BinaryExpr:
                return LowerBinaryExpr((BinaryExpr)expr);
            case NodeType.UnaryExpr:
                return LowerUnaryExpr((TermUnaryExpression)expr);
            case NodeType.CallExpr:
                return LowerCallExpr((TermCallExpression)expr);
            case NodeType.MemberAccessExpr:
                return LowerMemberAccessExpr((MemberAccessExpr)expr);
            case NodeType.AssignmentExpr:
                return LowerAssignmentExpr((AssignmentExpr)expr);
            default:
                return _lowering.EmitConst(0);
        }
    }

    private IrValue LowerLiteralExpr(LiteralExpr expr)
    {
        switch (expr.LiteralKind)
        {
            case LiteralType.Number:
                if (expr.Value is long l)
                {
                    return _lowering.EmitConst(l);
                }
                if (expr.Value is double d)
                {
                    return _lowering.EmitConst(d);
                }
                if (expr.Value is int i)
                {
                    return _lowering.EmitConst((long)i);
                }
                if (expr.Value is float f)
                {
                    return _lowering.EmitConst((double)f);
                }
                return _lowering.EmitConst(0);
            case LiteralType.Boolean:
                return _lowering.EmitConst(expr.Value is true);
            case LiteralType.Null:
            case LiteralType.String:
            default:
                return _lowering.EmitConst(0);
        }
    }

    private IrValue LowerIdentifierExpr(IdentifierNode node)
    {
        if (node.Name == "create_entity")
        {
            var result = _context.CreateValue(IrType.Entity, "entity");
            _context.Emit(IrInstruction.SpawnEntity(result));
            return result;
        }

        var value = _context.LookupValue(node.Name);
        if (value is not null)
        {
            if (value.Type.Kind == IrTypeKind.Reference)
            {
                return _lowering.EmitLoad(value, value.Type.GetElementType());
            }
            return value;
        }

        _diagnostics.AddError("ir", node.Span, "IR010", $"未定义的标识符: {node.Name}");
        return _lowering.EmitConst(0);
    }

    private IrValue LowerBinaryExpr(BinaryExpr expr)
    {
        var left = LowerExpression(expr.Left);
        var right = LowerExpression(expr.Right);

        var opcode = expr.Operator switch
        {
            "+" => IrOpcode.Add,
            "-" => IrOpcode.Sub,
            "*" => IrOpcode.Mul,
            "/" => IrOpcode.Div,
            "%" => IrOpcode.Mod,
            "==" => IrOpcode.Equal,
            "!=" => IrOpcode.NotEqual,
            "<" => IrOpcode.Less,
            ">" => IrOpcode.Greater,
            "<=" => IrOpcode.LessEqual,
            ">=" => IrOpcode.GreaterEqual,
            "&&" => IrOpcode.LogicalAnd,
            "||" => IrOpcode.LogicalOr,
            "&" => IrOpcode.BitAnd,
            "|" => IrOpcode.BitOr,
            "^" => IrOpcode.BitXor,
            "<<" => IrOpcode.Shl,
            ">>" => IrOpcode.Shr,
            _ => IrOpcode.Nop
        };

        return _lowering.EmitBinary(opcode, left, right);
    }

    private IrValue LowerUnaryExpr(TermUnaryExpression expr)
    {
        var operand = LowerExpression(expr.Operand);

        if (expr.Operator == "-")
        {
            return _lowering.EmitUnary(IrOpcode.Neg, operand);
        }

        if (expr.Operator == "!")
        {
            return _lowering.EmitUnary(IrOpcode.LogicalNot, operand);
        }

        if (expr.Operator == "~")
        {
            return _lowering.EmitUnary(IrOpcode.BitNot, operand);
        }

        return operand;
    }

    private IrValue LowerCallExpr(TermCallExpression expr)
    {
        if (expr.Callee is IdentifierNode idExpr)
        {
            if (idExpr.Name == "create_entity")
            {
                var result = _context.CreateValue(IrType.Entity, "entity");
                _context.Emit(IrInstruction.SpawnEntity(result));
                return result;
            }

            if (idExpr.Name == "destroy_entity" && expr.Arguments.Count > 0)
            {
                var entity = LowerExpression(expr.Arguments[0]);
                _context.Emit(IrInstruction.DestroyEntity(entity));
                return _lowering.EmitConst(0);
            }

            var args = expr.Arguments.Select(LowerExpression).ToList();
            var returnType = InferCallReturnType(idExpr.Name);

            return _lowering.EmitCall(idExpr.Name, args, returnType);
        }

        if (expr.Callee is MemberAccessExpr memberExpr)
        {
            return LowerMemberCallExpr(memberExpr, expr.Arguments);
        }

        var defaultArgs = expr.Arguments.Select(LowerExpression).ToList();
        return _lowering.EmitCall("unknown", defaultArgs, IrType.Void);
    }

    private IrValue LowerMemberAccessExpr(MemberAccessExpr expr)
    {
        var obj = LowerExpression(expr.Target);
        var result = _context.CreateValue(IrType.Void, expr.MemberName);
        _context.Emit(IrInstruction.GetField(result, obj, expr.MemberName));
        return result;
    }

    private IrValue LowerMemberCallExpr(MemberAccessExpr memberExpr, IReadOnlyList<AstNode> arguments)
    {
        var obj = LowerExpression(memberExpr.Target);
        var args = arguments.Select(LowerExpression).ToList();

        switch (memberExpr.MemberName)
        {
            case "add":
                {
                    var compArg = args.Count > 0 ? args[0] : _lowering.EmitConst(0);
                    _context.Emit(IrInstruction.AddComponent(obj, compArg));
                    return _lowering.EmitConst(0);
                }
            case "get":
                {
                    var compTypeName = arguments.Count > 0 && arguments[0] is IdentifierNode id ? id.Name : "unknown";
                    var result = _context.CreateValue(_typeMap.GetValueOrDefault(compTypeName, IrType.Void), "comp");
                    _context.Emit(IrInstruction.GetComponent(result, obj, compTypeName));
                    return result;
                }
            case "remove":
                {
                    var compTypeName = arguments.Count > 0 && arguments[0] is IdentifierNode id2 ? id2.Name : "unknown";
                    _context.Emit(IrInstruction.RemoveComponent(obj, compTypeName));
                    return _lowering.EmitConst(0);
                }
            default:
                return _lowering.EmitCall($"{memberExpr.MemberName}", [obj, .. args], IrType.Void);
        }
    }

    private IrValue LowerAssignmentExpr(AssignmentExpr expr)
    {
        var value = LowerExpression(expr.Value);

        if (expr.Target is IdentifierNode idExpr)
        {
            var target = _context.LookupValue(idExpr.Name);
            if (target is not null)
            {
                if (expr.Operator != "=")
                {
                    var current = target.Type.Kind == IrTypeKind.Reference
                        ? _lowering.EmitLoad(target, target.Type.GetElementType())
                        : target;
                    var op = expr.Operator switch
                    {
                        "+=" => IrOpcode.Add,
                        "-=" => IrOpcode.Sub,
                        "*=" => IrOpcode.Mul,
                        "/=" => IrOpcode.Div,
                        _ => IrOpcode.Nop
                    };
                    value = _lowering.EmitBinary(op, current, value);
                }

                _lowering.EmitStore(value, target);
            }
            else
            {
                _context.BindValue(idExpr.Name, value);
            }
        }
        else if (expr.Target is MemberAccessExpr memberExpr)
        {
            var obj = LowerExpression(memberExpr.Target);
            _context.Emit(IrInstruction.SetField(obj, memberExpr.MemberName, value));
        }

        return value;
    }

    #endregion

    #region 特性标注代码生成

    private void GenerateEncryptedAccessors(ComponentDecl decl, IrType compType)
    {
        foreach (var field in decl.Fields)
        {
            if (field.Attributes.Any(a => a.Name == "Honeypot"))
            {
                continue;
            }

            var fieldType = MapType(field.FieldType);
            if (!fieldType.IsNumeric())
            {
                continue;
            }

            var getterName = $"__encrypted_get_{decl.Name}_{field.Name}";
            _context.Reset();
            _lowering.LowerFunction(getterName, fieldType,
                [("self", IrType.ReferenceTo(compType))], () =>
            {
                var selfParam = _context.LookupValue("self")!;
                var rawValue = _context.CreateValue(fieldType, "raw");
                _context.Emit(IrInstruction.GetField(rawValue, selfParam, field.Name));

                var xorKey = _lowering.EmitConst(GetEncryptionKey(decl.Name, field.Name));
                var decrypted = _lowering.EmitBinary(IrOpcode.BitXor, rawValue, xorKey);
                _lowering.EmitReturn(decrypted);
            });

            var setterName = $"__encrypted_set_{decl.Name}_{field.Name}";
            _context.Reset();
            _lowering.LowerFunction(setterName, IrType.Void,
                [("self", IrType.ReferenceTo(compType)), ("value", fieldType)], () =>
            {
                var selfParam = _context.LookupValue("self")!;
                var valueParam = _context.LookupValue("value")!;

                var xorKey = _lowering.EmitConst(GetEncryptionKey(decl.Name, field.Name));
                var encrypted = _lowering.EmitBinary(IrOpcode.BitXor, valueParam, xorKey);
                _context.Emit(IrInstruction.SetField(selfParam, field.Name, encrypted));
                _lowering.EmitReturn();
            });
        }
    }

    private void GenerateReplicatedMetadata(ComponentDecl decl, IrType compType)
    {
        var metaName = $"__replicated_meta_{decl.Name}";
        _context.Reset();
        _lowering.LowerFunction(metaName, IrType.Void, [], () =>
        {
            foreach (var field in decl.Fields)
            {
                var fieldType = MapType(field.FieldType);
                var fieldSizeName = $"__replicated_size_{decl.Name}_{field.Name}";
                var sizeValue = _lowering.EmitConst(fieldType.BitWidth / 8);
                _lowering.EmitReturn(sizeValue);
            }
            _lowering.EmitReturn();
        });

        var serializeName = $"__replicated_serialize_{decl.Name}";
        _context.Reset();
        _lowering.LowerFunction(serializeName, IrType.Void,
            [("self", IrType.ReferenceTo(compType)), ("buffer", IrType.ReferenceTo(IrType.ArrayOf(IrType.I8)))], () =>
        {
            var selfParam = _context.LookupValue("self")!;
            var bufferParam = _context.LookupValue("buffer")!;

            foreach (var field in decl.Fields)
            {
                var fieldValue = _context.CreateValue(MapType(field.FieldType), field.Name);
                _context.Emit(IrInstruction.GetField(fieldValue, selfParam, field.Name));

                var nativeName = $"replicate_write_{MapType(field.FieldType)}";
                _lowering.EmitNativeCall(nativeName, [fieldValue, bufferParam], IrType.Void);
            }
            _lowering.EmitReturn();
        });

        var deserializeName = $"__replicated_deserialize_{decl.Name}";
        _context.Reset();
        _lowering.LowerFunction(deserializeName, IrType.Void,
            [("self", IrType.ReferenceTo(compType)), ("buffer", IrType.ReferenceTo(IrType.ArrayOf(IrType.I8)))], () =>
        {
            var selfParam = _context.LookupValue("self")!;
            var bufferParam = _context.LookupValue("buffer")!;

            foreach (var field in decl.Fields)
            {
                var nativeName = $"replicate_read_{MapType(field.FieldType)}";
                var value = _lowering.EmitNativeCall(nativeName, [bufferParam], MapType(field.FieldType));
                _context.Emit(IrInstruction.SetField(selfParam, field.Name, value));
            }
            _lowering.EmitReturn();
        });
    }

    private static long GetEncryptionKey(string componentName, string fieldName)
    {
        var hash = 0x811c9dc5u;
        var str = $"{componentName}.{fieldName}";
        foreach (var c in str)
        {
            hash ^= (uint)c;
            hash *= 0x01000193u;
        }
        return hash;
    }

    #endregion

    #region 类型映射

    private IrType MapType(TypeAnnotation typeAnnotation)
    {
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
                    ? MapType(typeAnnotation.GenericArgs[0])
                    : IrType.F32;
                return IrType.VectorOf(elementType, dim);
            }
        }

        if (name.StartsWith("mat"))
        {
            return IrType.F32;
        }

        return IrType.Void;
    }

    private IrType InferCallReturnType(string functionName)
    {
        if (functionName.StartsWith("new_"))
        {
            var typeName = functionName[4..];
            return _typeMap.GetValueOrDefault(typeName, IrType.Void);
        }

        return IrType.Void;
    }

    #endregion
}

internal sealed record ComponentInfo(
    string Name,
    IReadOnlyList<string> Attributes,
    IReadOnlyList<(string Name, IrType Type)> Fields);
