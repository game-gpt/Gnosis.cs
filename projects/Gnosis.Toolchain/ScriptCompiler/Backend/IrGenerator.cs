using Oak.Diagnostics;
using Oak.GGScript.AST;
using Gnosis.IR.Instruction;

namespace Gnosis.Toolchain.ScriptCompiler.Backend;

public sealed class IrGenerator
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
            Visit(decl);
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

    #region 调度方法

    private int Visit(AstNode node)
    {
        return node.Type switch
        {
            NodeType.CompilationUnit => VisitCompilationUnit((CompilationUnit)node),
            NodeType.FunctionDecl => VisitFunctionDecl((FunctionDecl)node),
            NodeType.VariableDecl => VisitVariableDecl((VariableDecl)node),
            NodeType.StructDecl => VisitStructDecl((StructDecl)node),
            NodeType.ComponentDecl => VisitComponentDecl((ComponentDecl)node),
            NodeType.ImportDecl => VisitImportDecl((ImportDecl)node),
            NodeType.UsingDecl => VisitUsingDecl((UsingDecl)node),
            NodeType.FieldDecl => VisitFieldDecl((FieldDecl)node),
            NodeType.ParameterDecl => VisitParameterDecl((ParameterDecl)node),
            NodeType.TypeAnnotation => VisitTypeAnnotation((TypeAnnotation)node),
            NodeType.AttributeDecl => VisitAttributeDecl((AttributeDecl)node),
            NodeType.UniformBindingDecl => VisitUniformBindingDecl((UniformBindingDecl)node),
            NodeType.NeuralDecl => VisitNeuralDecl((NeuralDecl)node),
            NodeType.SystemDecl => VisitSystemDecl((SystemDecl)node),
            NodeType.WidgetDecl => VisitWidgetDecl((WidgetDecl)node),
            NodeType.PluginDecl => VisitPluginDecl((PluginDecl)node),
            NodeType.LoopStmt => VisitLoopStmt((LoopStmt)node),
            NodeType.WhileStmt => VisitWhileStmt((WhileStmt)node),
            NodeType.ForStmt => VisitForStmt((ForStmt)node),
            NodeType.IfStmt => VisitIfStmt((IfStatement)node),
            NodeType.ReturnStmt => VisitReturnStmt((ReturnStatement)node),
            NodeType.BlockStmt => VisitBlockStmt((BlockStmt)node),
            NodeType.DiscardStmt => VisitDiscardStmt((DiscardStmt)node),
            NodeType.ExprStmt => VisitExprStmt((TermExpressionStatement)node),
            NodeType.BinaryExpr => VisitBinaryExpr((BinaryExpr)node),
            NodeType.UnaryExpr => VisitUnaryExpr((TermUnaryExpression)node),
            NodeType.LiteralExpr => VisitLiteralExpr((LiteralExpr)node),
            NodeType.IdentifierExpr => VisitIdentifierExpr((IdentifierNode)node),
            NodeType.CallExpr => VisitCallExpr((TermCallExpression)node),
            NodeType.AssignmentExpr => VisitAssignmentExpr((AssignmentExpr)node),
            NodeType.MemberAccessExpr => VisitMemberAccessExpr((MemberAccessExpr)node),
            NodeType.IndexExpr => VisitIndexExpr((TermIndexExpression)node),
            NodeType.SwizzleExpr => VisitSwizzleExpr((SwizzleExpr)node),
            NodeType.LambdaExpr => VisitLambdaExpr((LambdaExpr)node),
            NodeType.QueryExpr => VisitQueryExpr((QueryExpr)node),
            NodeType.MetaBlock => VisitMetaBlock((MetaBlock)node),
            NodeType.TensorTypeExpr => VisitTensorTypeExpr((TensorTypeExpr)node),
            NodeType.TensorDimension => VisitTensorDimension((TensorDimension)node),
            _ => 0
        };
    }

    #endregion

    #region 声明

    private int VisitCompilationUnit(CompilationUnit node)
    {
        foreach (var decl in node.Declarations)
        {
            Visit(decl);
        }

        return 0;
    }

    private int VisitFunctionDecl(FunctionDecl node)
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
            Visit(node.Body);
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

    private int VisitVariableDecl(VariableDecl node)
    {
        _currentLocals[node.Name] = _currentLocalCount++;

        if (node.Initializer != null)
        {
            var stackEffect = Visit(node.Initializer);
            Emit(OpCode.StoreLocal, _currentLocals[node.Name]);
            return stackEffect - 1;
        }

        return 0;
    }

    private int VisitStructDecl(StructDecl node)
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

    private int VisitComponentDecl(ComponentDecl node)
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

    private int VisitImportDecl(ImportDecl node)
    {
        _imports.Add(node.ModulePath);
        return 0;
    }

    private int VisitUsingDecl(UsingDecl node)
    {
        _imports.Add(node.NamespacePath);
        return 0;
    }

    private int VisitFieldDecl(FieldDecl node) => 0;

    private int VisitParameterDecl(ParameterDecl node) => 0;

    private int VisitTypeAnnotation(TypeAnnotation node) => 0;

    private int VisitAttributeDecl(AttributeDecl node) => 0;

    private int VisitUniformBindingDecl(UniformBindingDecl node)
    {
        var globalIndex = _globalVariables.Count;
        _globalVariables[node.Name] = globalIndex;
        return 0;
    }

    private int VisitNeuralDecl(NeuralDecl node)
    {
        Visit(node.ForwardFunction);
        return 0;
    }

    private int VisitSystemDecl(SystemDecl node)
    {
        foreach (var query in node.Queries)
        {
            Visit(query);
        }

        foreach (var method in node.Methods)
        {
            Visit(method);
        }

        return 0;
    }

    private int VisitWidgetDecl(WidgetDecl node)
    {
        if (node.RenderMethod != null)
        {
            Visit(node.RenderMethod);
        }

        return 0;
    }

    private int VisitPluginDecl(PluginDecl node)
    {
        foreach (var func in node.Functions)
        {
            Visit(func);
        }

        return 0;
    }

    #endregion

    #region 语句

    private int VisitLoopStmt(LoopStmt node)
    {
        var loopStartLabel = NewLabel();
        var loopExitLabel = NewLabel();

        if (node.Iterable != null && node.IteratorName != null)
        {
            _currentLocals[node.IteratorName] = _currentLocalCount++;

            Visit(node.Iterable);
            Emit(OpCode.StoreLocal, _currentLocals[node.IteratorName]);

            PlaceLabel(loopStartLabel);

            Visit(node.Body);

            Emit(OpCode.LoadLocal, _currentLocals[node.IteratorName]);
            Emit(OpCode.Jump, ResolveLabel(loopStartLabel));
            PlaceLabel(loopExitLabel);
        }
        else
        {
            PlaceLabel(loopStartLabel);

            Visit(node.Body);

            Emit(OpCode.Jump, ResolveLabel(loopStartLabel));
            PlaceLabel(loopExitLabel);
        }

        return 0;
    }

    private int VisitWhileStmt(WhileStmt node)
    {
        var loopStartLabel = NewLabel();
        var loopExitLabel = NewLabel();

        PlaceLabel(loopStartLabel);

        Visit(node.Condition);
        EmitJump(OpCode.JumpIfFalse, loopExitLabel);

        Visit(node.Body);

        Emit(OpCode.Jump, ResolveLabel(loopStartLabel));
        PlaceLabel(loopExitLabel);

        return 0;
    }

    private int VisitForStmt(ForStmt node)
    {
        var loopStartLabel = NewLabel();
        var loopExitLabel = NewLabel();

        if (node.Initializer != null)
        {
            Visit(node.Initializer);
        }

        PlaceLabel(loopStartLabel);

        if (node.Condition != null)
        {
            Visit(node.Condition);
            EmitJump(OpCode.JumpIfFalse, loopExitLabel);
        }

        Visit(node.Body);

        if (node.Update != null)
        {
            Visit(node.Update);
        }

        Emit(OpCode.Jump, ResolveLabel(loopStartLabel));
        PlaceLabel(loopExitLabel);

        return 0;
    }

    private int VisitIfStmt(IfStatement node)
    {
        var elseLabel = NewLabel();
        var endLabel = NewLabel();

        Visit(node.Condition);
        EmitJump(OpCode.JumpIfFalse, elseLabel);

        Visit(node.ThenBlock);
        EmitJump(OpCode.Jump, endLabel);

        PlaceLabel(elseLabel);

        if (node.ElseBlock != null)
        {
            Visit(node.ElseBlock);
        }

        PlaceLabel(endLabel);

        return 0;
    }

    private int VisitReturnStmt(ReturnStatement node)
    {
        if (node.Value != null)
        {
            Visit(node.Value);
        }

        Emit(OpCode.Return);
        return 0;
    }

    private int VisitBlockStmt(BlockStmt node)
    {
        foreach (var stmt in node.Statements)
        {
            Visit(stmt);
        }

        return 0;
    }

    private int VisitDiscardStmt(DiscardStmt node)
    {
        _diagnostics.AddWarning(_currentFilePath, node.Span, "IR001", "脚本上下文中不支持 discard 语句");
        Emit(OpCode.Halt);
        return 0;
    }

    private int VisitExprStmt(TermExpressionStatement node)
    {
        var stackEffect = Visit(node.Expression);

        if (stackEffect > 0)
        {
            Emit(OpCode.Pop);
        }

        return 0;
    }

    #endregion

    #region 表达式

    private int VisitBinaryExpr(BinaryExpr node)
    {
        Visit(node.Left);
        Visit(node.Right);

        var opCode = MapBinaryOp(node.Operator);
        Emit(opCode);

        return 1;
    }

    private int VisitUnaryExpr(TermUnaryExpression node)
    {
        Visit(node.Operand);

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

    private int VisitLiteralExpr(LiteralExpr node)
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

    private int VisitIdentifierExpr(IdentifierNode node)
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

    private int VisitCallExpr(TermCallExpression node)
    {
        foreach (var arg in node.Arguments)
        {
            Visit(arg);
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

    private int VisitAssignmentExpr(AssignmentExpr node)
    {
        Visit(node.Value);

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
            Visit(memberAccess.Target);
            var fieldIndex = ResolveFieldIndex(memberAccess);
            Emit(OpCode.SetField, fieldIndex);
        }

        return 1;
    }

    private int VisitMemberAccessExpr(MemberAccessExpr node)
    {
        Visit(node.Target);

        var fieldIndex = ResolveFieldIndex(node);
        Emit(OpCode.GetField, fieldIndex);

        return 1;
    }

    private int VisitIndexExpr(TermIndexExpression node)
    {
        Visit(node.Target);
        Visit(node.Index);

        Emit(OpCode.LoadField, 0);

        return 1;
    }

    private int VisitSwizzleExpr(SwizzleExpr node)
    {
        Visit(node.Target);

        var components = ParseSwizzleComponents(node.Pattern);
        if (components != null)
        {
            foreach (var comp in components)
            {
                Emit(OpCode.GetField, comp);
            }
        }

        return 1;
    }

    private int VisitLambdaExpr(LambdaExpr node)
    {
        _diagnostics.AddWarning(_currentFilePath, node.Span, "IR004", "字节码 IR 中不支持 Lambda 表达式");
        return 0;
    }

    private int VisitQueryExpr(QueryExpr node)
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

    #region 其他

    private int VisitMetaBlock(MetaBlock node) => 0;

    private int VisitTensorTypeExpr(TensorTypeExpr node) => 0;

    private int VisitTensorDimension(TensorDimension node) => 0;

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
        if (node.Target is IdentifierNode ident && _structTypes.TryGetValue(ident.Name, out var structInfo))
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
