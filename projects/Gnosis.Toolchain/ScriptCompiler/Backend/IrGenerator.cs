using Gnosis.Toolchain.ScriptCompiler.AST;
using Gnosis.Toolchain.ScriptCompiler.Diagnostics;
using Gnosis.Core.Diagnostic;
using Gnosis.IR.Instruction;

namespace Gnosis.Toolchain.ScriptCompiler.Backend;

public sealed class IrGenerator : IAstVisitor<int>
{
    #region 字段

    private readonly DiagnosticSink _diagnostics;
    private readonly List<object> _constants = [];
    private readonly List<BytecodeFunction> _functions = [];
    private readonly List<string> _imports = [];
    private readonly List<string> _exports = [];
    private readonly List<(int Offset, SourceSpan? Span)> _sourceMap = [];
    private readonly List<BytecodeInstruction> _currentInstructions = [];
    private readonly Dictionary<string, int> _currentLocals = [];
    private readonly Dictionary<string, int> _globalVariables = [];
    private readonly Dictionary<string, StructInfo> _structTypes = [];
    private readonly List<(int JumpIndex, string TargetLabel)> _unresolvedJumps = [];
    private readonly Dictionary<string, int> _labels = [];
    private int _currentParameterCount;
    private int _currentLocalCount;
    private string _currentFilePath = "unknown";
    private int _labelCounter;

    #endregion

    #region 构造函数

    public IrGenerator(DiagnosticSink? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticSink();
    }

    #endregion

    #region 公共方法

    public BytecodeUnit Generate(CompilationUnit ast)
    {
        _constants.Clear();
        _functions.Clear();
        _imports.Clear();
        _exports.Clear();
        _sourceMap.Clear();
        _globalVariables.Clear();
        _structTypes.Clear();
        _currentFilePath = ast.FilePath ?? "unknown";

        foreach (var decl in ast.Declarations)
        {
            decl.Accept(this);
        }

        return new BytecodeUnit(
            ast.FilePath ?? "unknown",
            _constants.ToList(),
            _functions.ToList(),
            _imports.ToList(),
            _exports.ToList(),
            _sourceMap.ToList());
    }

    #endregion

    #region IAstVisitor 声明

    public int VisitCompilationUnit(CompilationUnit node)
    {
        foreach (var decl in node.Declarations)
        {
            decl.Accept(this);
        }

        return 0;
    }

    public int VisitFunctionDecl(FunctionDecl node)
    {
        _currentInstructions.Clear();
        _currentLocals.Clear();
        _unresolvedJumps.Clear();
        _labels.Clear();
        _currentParameterCount = node.Parameters.Count;
        _currentLocalCount = 0;
        _labelCounter = 0;

        foreach (var param in node.Parameters)
        {
            _currentLocals[param.Name] = _currentLocalCount++;
        }

        if (node.Body != null)
        {
            node.Body.Accept(this);
        }

        Emit(OpCode.Return);

        ResolveJumps();

        _functions.Add(new BytecodeFunction(
            node.Name,
            _currentParameterCount,
            _currentLocalCount,
            _currentInstructions.ToList()));

        return 0;
    }

    public int VisitVariableDecl(VariableDecl node)
    {
        _currentLocals[node.Name] = _currentLocalCount++;

        if (node.Initializer != null)
        {
            var stackEffect = node.Initializer.Accept(this);
            Emit(OpCode.StoreLocal, _currentLocals[node.Name]);
            return stackEffect - 1;
        }

        return 0;
    }

    public int VisitStructDecl(StructDecl node)
    {
        var fields = new List<StructFieldInfo>();
        var offset = 0;

        foreach (var field in node.Fields)
        {
            fields.Add(new StructFieldInfo(field.Name, offset));
            offset++;
        }

        _structTypes[node.Name] = new StructInfo(node.Name, fields);
        return 0;
    }

    public int VisitComponentDecl(ComponentDecl node)
    {
        var fields = new List<StructFieldInfo>();
        var offset = 0;

        foreach (var field in node.Fields)
        {
            fields.Add(new StructFieldInfo(field.Name, offset));
            offset++;
        }

        _structTypes[node.Name] = new StructInfo(node.Name, fields);
        return 0;
    }

    public int VisitImportDecl(ImportDecl node)
    {
        _imports.Add(node.ModulePath);
        return 0;
    }

    public int VisitUsingDecl(UsingDecl node)
    {
        _imports.Add(node.NamespacePath);
        return 0;
    }

    public int VisitFieldDecl(FieldDecl node)
    {
        return 0;
    }

    public int VisitParameterDecl(ParameterDecl node)
    {
        return 0;
    }

    public int VisitTypeAnnotation(TypeAnnotation node)
    {
        return 0;
    }

    public int VisitAttributeDecl(AttributeDecl node)
    {
        return 0;
    }

    public int VisitUniformBindingDecl(UniformBindingDecl node)
    {
        var globalIndex = _globalVariables.Count;
        _globalVariables[node.Name] = globalIndex;
        return 0;
    }

    public int VisitNeuralDecl(NeuralDecl node)
    {
        node.ForwardFunction.Accept(this);
        return 0;
    }

    public int VisitSystemDecl(SystemDecl node)
    {
        foreach (var query in node.Queries)
        {
            query.Accept(this);
        }

        foreach (var method in node.LifecycleMethods)
        {
            method.Accept(this);
        }

        return 0;
    }

    public int VisitWidgetDecl(WidgetDecl node)
    {
        if (node.RenderMethod != null)
        {
            node.RenderMethod.Accept(this);
        }

        return 0;
    }

    public int VisitPluginDecl(PluginDecl node)
    {
        foreach (var func in node.Functions)
        {
            func.Accept(this);
        }

        return 0;
    }

    #endregion

    #region IAstVisitor 语句

    public int VisitLoopStmt(LoopStmt node)
    {
        var loopStartLabel = NewLabel();
        var loopExitLabel = NewLabel();

        if (node.Iterable != null && node.IteratorName != null)
        {
            _currentLocals[node.IteratorName] = _currentLocalCount++;

            node.Iterable.Accept(this);
            Emit(OpCode.StoreLocal, _currentLocals[node.IteratorName]);

            PlaceLabel(loopStartLabel);

            node.Body.Accept(this);

            Emit(OpCode.LoadLocal, _currentLocals[node.IteratorName]);
            Emit(OpCode.Jump, ResolveLabel(loopStartLabel));
            PlaceLabel(loopExitLabel);
        }
        else
        {
            PlaceLabel(loopStartLabel);

            node.Body.Accept(this);

            Emit(OpCode.Jump, ResolveLabel(loopStartLabel));
            PlaceLabel(loopExitLabel);
        }

        return 0;
    }

    public int VisitWhileStmt(WhileStmt node)
    {
        var loopStartLabel = NewLabel();
        var loopExitLabel = NewLabel();

        PlaceLabel(loopStartLabel);

        node.Condition.Accept(this);
        EmitJump(OpCode.JumpIfFalse, loopExitLabel);

        node.Body.Accept(this);

        Emit(OpCode.Jump, ResolveLabel(loopStartLabel));
        PlaceLabel(loopExitLabel);

        return 0;
    }

    public int VisitForStmt(ForStmt node)
    {
        var loopStartLabel = NewLabel();
        var loopExitLabel = NewLabel();

        if (node.Initializer != null)
        {
            node.Initializer.Accept(this);
        }

        PlaceLabel(loopStartLabel);

        if (node.Condition != null)
        {
            node.Condition.Accept(this);
            EmitJump(OpCode.JumpIfFalse, loopExitLabel);
        }

        node.Body.Accept(this);

        if (node.Update != null)
        {
            node.Update.Accept(this);
        }

        Emit(OpCode.Jump, ResolveLabel(loopStartLabel));
        PlaceLabel(loopExitLabel);

        return 0;
    }

    public int VisitIfStmt(IfStatement node)
    {
        var elseLabel = NewLabel();
        var endLabel = NewLabel();

        node.Condition.Accept(this);
        EmitJump(OpCode.JumpIfFalse, elseLabel);

        node.ThenBlock.Accept(this);
        EmitJump(OpCode.Jump, endLabel);

        PlaceLabel(elseLabel);

        if (node.ElseBlock != null)
        {
            node.ElseBlock.Accept(this);
        }

        PlaceLabel(endLabel);

        return 0;
    }

    public int VisitReturnStmt(ReturnStatement node)
    {
        if (node.Value != null)
        {
            node.Value.Accept(this);
        }

        Emit(OpCode.Return);
        return 0;
    }

    public int VisitBlockStmt(BlockStmt node)
    {
        foreach (var stmt in node.Statements)
        {
            stmt.Accept(this);
        }

        return 0;
    }

    public int VisitDiscardStmt(DiscardStmt node)
    {
        _diagnostics.AddWarning(_currentFilePath, node.Span, "IR001", "脚本上下文中不支持 discard 语句");
        Emit(OpCode.Halt);
        return 0;
    }

    public int VisitExprStmt(TermExpressionStatement node)
    {
        var stackEffect = node.Expression.Accept(this);

        if (stackEffect > 0)
        {
            Emit(OpCode.Pop);
        }

        return 0;
    }

    #endregion

    #region IAstVisitor 表达式

    public int VisitBinaryExpr(BinaryExpr node)
    {
        node.Left.Accept(this);
        node.Right.Accept(this);

        var opCode = MapBinaryOp(node.Operator);
        Emit(opCode);

        return 1;
    }

    public int VisitUnaryExpr(TermUnaryExpression node)
    {
        node.Operand.Accept(this);

        if (node.Operator == "-")
        {
            Emit(OpCode.NegInt);
        }
        else if (node.Operator == "!")
        {
            Emit(OpCode.Not);
        }

        return 1;
    }

    public int VisitLiteralExpr(LiteralExpr node)
    {
        switch (node.LiteralKind)
        {
            case LiteralType.Number:
                if (node.Value is int intValue)
                {
                    Emit(OpCode.PushInt32, intValue);
                }
                else if (node.Value is float floatValue)
                {
                    Emit(OpCode.PushFloat32, (long)BitConverter.SingleToUInt32Bits(floatValue));
                }
                else if (node.Value is double doubleValue)
                {
                    Emit(OpCode.PushFloat64, (long)BitConverter.DoubleToUInt64Bits(doubleValue));
                }
                else if (node.Value is long longValue)
                {
                    Emit(OpCode.PushInt64, longValue);
                }
                else
                {
                    Emit(OpCode.PushInt32, 0);
                }

                break;

            case LiteralType.String:
                var strValue = node.Value?.ToString() ?? "";
                var constIndex = AddConstant(strValue);
                Emit(OpCode.PushInt32, constIndex);
                break;

            case LiteralType.Boolean:
                Emit(OpCode.PushInt32, node.Value is true ? 1 : 0);
                break;

            case LiteralType.Null:
                Emit(OpCode.PushInt32, 0);
                break;
        }

        return 1;
    }

    public int VisitIdentifierExpr(IdentifierNode node)
    {
        if (_currentLocals.TryGetValue(node.Name, out var localIndex))
        {
            Emit(OpCode.LoadLocal, localIndex);
        }
        else if (_globalVariables.TryGetValue(node.Name, out var globalIndex))
        {
            Emit(OpCode.LoadGlobal, globalIndex);
        }
        else
        {
            _diagnostics.AddError(_currentFilePath, node.Span, "IR002", $"未定义的标识符 '{node.Name}'");
            Emit(OpCode.PushInt32, 0);
        }

        return 1;
    }

    public int VisitCallExpr(TermCallExpression node)
    {
        foreach (var arg in node.Arguments)
        {
            arg.Accept(this);
        }

        if (node.Callee is IdentifierNode ident)
        {
            var funcNameIndex = AddConstant(ident.Name);
            Emit(OpCode.Call, funcNameIndex);
        }
        else
        {
            var calleeName = "unknown";
            var nameIndex = AddConstant(calleeName);
            Emit(OpCode.CallNative, nameIndex);
        }

        return 1;
    }

    public int VisitAssignmentExpr(AssignmentExpr node)
    {
        node.Value.Accept(this);

        if (node.Target is IdentifierNode ident)
        {
            if (_currentLocals.TryGetValue(ident.Name, out var localIndex))
            {
                Emit(OpCode.StoreLocal, localIndex);
            }
            else if (_globalVariables.TryGetValue(ident.Name, out var globalIndex))
            {
                Emit(OpCode.StoreGlobal, globalIndex);
            }
            else
            {
                _diagnostics.AddError(_currentFilePath, ident.Span, "IR003", $"未定义的变量 '{ident.Name}'");
            }
        }
        else if (node.Target is MemberAccessExpr memberAccess)
        {
            memberAccess.Object.Accept(this);
            var fieldIndex = ResolveFieldIndex(memberAccess);
            Emit(OpCode.SetField, fieldIndex);
        }

        return 1;
    }

    public int VisitMemberAccessExpr(MemberAccessExpr node)
    {
        node.Object.Accept(this);

        var fieldIndex = ResolveFieldIndex(node);
        Emit(OpCode.GetField, fieldIndex);

        return 1;
    }

    public int VisitIndexExpr(TermIndexExpression node)
    {
        node.Object.Accept(this);
        node.Index.Accept(this);

        Emit(OpCode.LoadField, 0);

        return 1;
    }

    public int VisitSwizzleExpr(SwizzleExpr node)
    {
        node.Object.Accept(this);

        var components = ParseSwizzleComponents(node.Components);
        if (components != null)
        {
            foreach (var comp in components)
            {
                Emit(OpCode.GetField, comp);
            }
        }

        return 1;
    }

    public int VisitLambdaExpr(LambdaExpr node)
    {
        _diagnostics.AddWarning(_currentFilePath, node.Span, "IR004", "字节码 IR 中不支持 Lambda 表达式");
        return 0;
    }

    public int VisitQueryExpr(QueryExpr node)
    {
        var componentCount = node.ComponentTypes.Count;

        foreach (var compType in node.ComponentTypes)
        {
            var nameIndex = AddConstant(compType.Name);
            Emit(OpCode.PushInt32, nameIndex);
        }

        var opCode = node.Kind switch
        {
            QueryKind.All => OpCode.QueryAll,
            QueryKind.Any => OpCode.QueryAny,
            _ => OpCode.QueryAll
        };

        Emit(opCode, componentCount);
        return 1;
    }

    #endregion

    #region IAstVisitor 其他

    public int VisitMetaBlock(MetaBlock node)
    {
        return 0;
    }

    public int VisitTensorTypeExpr(TensorTypeExpr node)
    {
        return 0;
    }

    public int VisitTensorDimension(TensorDimension node)
    {
        return 0;
    }

    #endregion

    #region 辅助方法

    private void Emit(OpCode opCode, long operand = 0)
    {
        _currentInstructions.Add(new BytecodeInstruction(opCode, operand));
    }

    private void EmitJump(OpCode jumpOpCode, string targetLabel)
    {
        Emit(jumpOpCode, 0);
        _unresolvedJumps.Add((_currentInstructions.Count - 1, targetLabel));
    }

    private string NewLabel()
    {
        return $"L{_labelCounter++}";
    }

    private void PlaceLabel(string label)
    {
        _labels[label] = _currentInstructions.Count;
    }

    private int ResolveLabel(string label)
    {
        return _labels.TryGetValue(label, out var index) ? index : -1;
    }

    private void ResolveJumps()
    {
        foreach (var (jumpIndex, targetLabel) in _unresolvedJumps)
        {
            if (_labels.TryGetValue(targetLabel, out var targetIndex))
            {
                _currentInstructions[jumpIndex] = new BytecodeInstruction(
                    _currentInstructions[jumpIndex].OpCode,
                    targetIndex);
            }
            else
            {
                _diagnostics.AddError(_currentFilePath, null, "IR005", $"无法解析跳转目标 '{targetLabel}'");
            }
        }
    }

    private int AddConstant(object value)
    {
        var index = _constants.IndexOf(value);
        if (index >= 0)
        {
            return index;
        }

        _constants.Add(value);
        return _constants.Count - 1;
    }

    private OpCode MapBinaryOp(string op)
    {
        return op switch
        {
            "+" => OpCode.AddInt,
            "-" => OpCode.SubInt,
            "*" => OpCode.MulInt,
            "/" => OpCode.DivInt,
            "==" => OpCode.EqualInt,
            "!=" => OpCode.NotEqualInt,
            "<" => OpCode.LessInt,
            ">" => OpCode.GreaterInt,
            "<=" => OpCode.LessEqualInt,
            ">=" => OpCode.GreaterEqualInt,
            "&&" => OpCode.And,
            "||" => OpCode.Or,
            _ => OpCode.Nop
        };
    }

    private long ResolveFieldIndex(MemberAccessExpr node)
    {
        if (node.Object is IdentifierNode ident && _structTypes.TryGetValue(ident.Name, out var structInfo))
        {
            var field = structInfo.Fields.Find(f => f.Name == node.MemberName);
            if (field != null)
            {
                return field.Offset;
            }
        }

        return AddConstant(node.MemberName);
    }

    private static int[]? ParseSwizzleComponents(string components)
    {
        if (string.IsNullOrEmpty(components) || components.Length > 4)
        {
            return null;
        }

        var isXyzw = components.All(c => c is 'x' or 'y' or 'z' or 'w');
        var isRgba = components.All(c => c is 'r' or 'g' or 'b' or 'a');

        if (!isXyzw && !isRgba)
        {
            return null;
        }

        return components.Select(c => c switch
        {
            'x' or 'r' => 0,
            'y' or 'g' => 1,
            'z' or 'b' => 2,
            'w' or 'a' => 3,
            _ => 0
        }).ToArray();
    }

    #endregion
}

public sealed class StructInfo
{
    #region 属性

    public string Name { get; }

    public List<StructFieldInfo> Fields { get; }

    #endregion

    #region 构造函数

    public StructInfo(string name, List<StructFieldInfo> fields)
    {
        Name = name;
        Fields = fields;
    }

    #endregion
}

public sealed class StructFieldInfo
{
    #region 属性

    public string Name { get; }

    public int Offset { get; }

    #endregion

    #region 构造函数

    public StructFieldInfo(string name, int offset)
    {
        Name = name;
        Offset = offset;
    }

    #endregion
}
