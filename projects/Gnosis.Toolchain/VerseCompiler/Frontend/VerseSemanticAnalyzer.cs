using Oak.Diagnostics;
using Oak.Verse.AST;

namespace Gnosis.Toolchain.VerseCompiler.Frontend;

/// <summary>
/// Verse 语义分析器，负责变量作用域检查、类型推断、场景/标签引用验证
/// </summary>
public sealed class VerseSemanticAnalyzer
{
    #region 字段

    private readonly DiagnosticSink _diagnostics;
    private readonly Dictionary<string, SceneDecl> _scenes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LabelDecl> _labels = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VariableInfo> _globalVariables = new(StringComparer.Ordinal);
    private readonly Stack<Dictionary<string, VariableInfo>> _scopes = new();
    private string _currentFilePath = string.Empty;

    #endregion

    #region 构造函数

    public VerseSemanticAnalyzer(DiagnosticSink diagnostics)
    {
        _diagnostics = diagnostics;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 对 Verse 编译单元执行语义分析
    /// </summary>
    public CompilationUnit Analyze(CompilationUnit unit)
    {
        _scenes.Clear();
        _labels.Clear();
        _globalVariables.Clear();
        _scopes.Clear();
        _currentFilePath = unit.FilePath ?? string.Empty;

        BuildSymbolTable(unit);
        ValidateReferences(unit);

        return unit;
    }

    #endregion

    #region 符号表构建

    private void BuildSymbolTable(CompilationUnit unit)
    {
        foreach (var decl in unit.Declarations)
        {
            BuildSymbolTableForNode(decl);
        }
    }

    private void BuildSymbolTableForNode(AstNode node)
    {
        switch (node.Kind)
        {
            case NodeType.SceneDecl:
                BuildSceneSymbol((SceneDecl)node);
                break;

            case NodeType.LabelDecl:
                BuildLabelSymbol((LabelDecl)node);
                break;

            case NodeType.SetStmt:
                BuildVariableSymbol((SetStmt)node);
                break;
        }
    }

    private void BuildSceneSymbol(SceneDecl scene)
    {
        if (_scenes.ContainsKey(scene.Name))
        {
            _diagnostics.AddError(
                _currentFilePath,
                scene.Span,
                "VS0401",
                $"重复的场景定义: {scene.Name}",
                [$"移除或重命名重复的场景 '{scene.Name}'"]);
        }
        else
        {
            _scenes[scene.Name] = scene;
        }

        foreach (var stmt in scene.Body)
        {
            BuildSymbolTableForNode(stmt);
        }
    }

    private void BuildLabelSymbol(LabelDecl label)
    {
        if (_labels.ContainsKey(label.Name))
        {
            _diagnostics.AddWarning(
                _currentFilePath,
                label.Span,
                "VS0402",
                $"重复的标签定义: {label.Name}");
        }
        else
        {
            _labels[label.Name] = label;
        }
    }

    private void BuildVariableSymbol(SetStmt setStmt)
    {
        var varInfo = new VariableInfo(setStmt.VariableName, InferTypeFromValue(setStmt.Value));

        if (_scopes.Count > 0)
        {
            _scopes.Peek()[setStmt.VariableName] = varInfo;
        }
        else
        {
            if (_globalVariables.ContainsKey(setStmt.VariableName))
            {
                _diagnostics.AddWarning(
                    _currentFilePath,
                    setStmt.Span,
                    "VS0403",
                    $"重复的全局变量定义: {setStmt.VariableName}");
            }
            else
            {
                _globalVariables[setStmt.VariableName] = varInfo;
            }
        }
    }

    #endregion

    #region 引用验证

    private void ValidateReferences(CompilationUnit unit)
    {
        foreach (var decl in unit.Declarations)
        {
            ValidateNode(decl);
        }
    }

    private void ValidateNode(AstNode node)
    {
        switch (node.Kind)
        {
            case NodeType.SceneDecl:
                var scene = (SceneDecl)node;
                foreach (var stmt in scene.Body)
                {
                    ValidateNode(stmt);
                }
                break;

            case NodeType.JumpStmt:
                ValidateJumpStmt((JumpStmt)node);
                break;

            case NodeType.CallStmt:
                ValidateCallStmt((CallStmt)node);
                break;

            case NodeType.SetStmt:
                ValidateSetStmt((SetStmt)node);
                break;

            case NodeType.IfStmt:
                ValidateIfStmt((IfStmt)node);
                break;

            case NodeType.MenuDecl:
                ValidateMenuDecl((MenuDecl)node);
                break;

            case NodeType.CommandCall:
                ValidateCommandCall((CommandCall)node);
                break;
        }
    }

    private void ValidateJumpStmt(JumpStmt jump)
    {
        if (!_scenes.ContainsKey(jump.Target) && !_labels.ContainsKey(jump.Target))
        {
            _diagnostics.AddError(
                _currentFilePath,
                jump.Span,
                "VS0501",
                $"未定义的跳转目标: {jump.Target}",
                [$"确保场景或标签 '{jump.Target}' 已定义"]);
        }

        if (jump.Condition is not null)
        {
            ValidateExpression(jump.Condition);
        }
    }

    private void ValidateCallStmt(CallStmt call)
    {
        if (!_scenes.ContainsKey(call.Target) && !_labels.ContainsKey(call.Target))
        {
            _diagnostics.AddWarning(
                _currentFilePath,
                call.Span,
                "VS0502",
                $"未定义的调用目标: {call.Target}");
        }

        foreach (var arg in call.Arguments)
        {
            ValidateExpression(arg);
        }
    }

    private void ValidateSetStmt(SetStmt set)
    {
        ValidateExpression(set.Value);
    }

    private void ValidateIfStmt(IfStmt ifStmt)
    {
        ValidateExpression(ifStmt.Condition);

        PushScope();
        foreach (var stmt in ifStmt.ThenBody)
        {
            ValidateNode(stmt);
        }
        PopScope();

        foreach (var elif in ifStmt.ElifBranches)
        {
            ValidateExpression(elif.Condition);

            PushScope();
            foreach (var stmt in elif.Body)
            {
                ValidateNode(stmt);
            }
            PopScope();
        }

        if (ifStmt.ElseBranch is not null)
        {
            PushScope();
            foreach (var stmt in ifStmt.ElseBranch.Body)
            {
                ValidateNode(stmt);
            }
            PopScope();
        }
    }

    private void ValidateMenuDecl(MenuDecl menu)
    {
        foreach (var item in menu.Items)
        {
            if (item.Condition is not null)
            {
                ValidateExpression(item.Condition);
            }

            PushScope();
            foreach (var stmt in item.Body)
            {
                ValidateNode(stmt);
            }
            PopScope();
        }
    }

    private void ValidateCommandCall(CommandCall command)
    {
        foreach (var arg in command.Arguments)
        {
            ValidateExpression(arg.Value);
        }
    }

    #endregion

    #region 表达式验证

    private void ValidateExpression(AstNode expr)
    {
        switch (expr.Kind)
        {
            case NodeType.IdentifierExpr:
                var idExpr = (IdentifierExpr)expr;
                if (!IsVariableInScope(idExpr.Name) && !_scenes.ContainsKey(idExpr.Name))
                {
                    _diagnostics.AddWarning(
                        _currentFilePath,
                        idExpr.Span,
                        "VS0530",
                        $"未定义的标识符: {idExpr.Name}");
                }
                break;

            case NodeType.BinaryExpr:
                var binExpr = (BinaryExpr)expr;
                ValidateExpression(binExpr.Left);
                ValidateExpression(binExpr.Right);
                break;

            case NodeType.UnaryExpr:
                var unaryExpr = (UnaryExpr)expr;
                ValidateExpression(unaryExpr.Operand);
                break;

            case NodeType.MemberAccessExpr:
                var memberExpr = (MemberAccessExpr)expr;
                ValidateExpression(memberExpr.Object);
                break;

            case NodeType.AssignmentExpr:
                var assignExpr = (AssignmentExpr)expr;
                ValidateExpression(assignExpr.Target);
                ValidateExpression(assignExpr.Value);
                break;

            case NodeType.LiteralExpr:
            case NodeType.CommandArg:
                break;
        }
    }

    #endregion

    #region 作用域管理

    private void PushScope()
    {
        _scopes.Push(new Dictionary<string, VariableInfo>(StringComparer.Ordinal));
    }

    private void PopScope()
    {
        if (_scopes.Count > 0)
        {
            _scopes.Pop();
        }
    }

    private bool IsVariableInScope(string name)
    {
        if (_globalVariables.ContainsKey(name))
        {
            return true;
        }

        foreach (var scope in _scopes)
        {
            if (scope.ContainsKey(name))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    #region 类型推断

    private static VerseTypeKind InferTypeFromValue(AstNode value)
    {
        return value.Kind switch
        {
            NodeType.LiteralExpr => InferTypeFromLiteral((LiteralExpr)value),
            NodeType.IdentifierExpr => VerseTypeKind.Unknown,
            NodeType.BinaryExpr => InferTypeFromBinary((BinaryExpr)value),
            NodeType.UnaryExpr => InferTypeFromUnary((UnaryExpr)value),
            _ => VerseTypeKind.Unknown
        };
    }

    private static VerseTypeKind InferTypeFromLiteral(LiteralExpr literal)
    {
        return literal.LiteralKind switch
        {
            LiteralType.Number => double.TryParse(literal.Value, out _) ? VerseTypeKind.F64 : VerseTypeKind.I32,
            LiteralType.String => VerseTypeKind.String,
            LiteralType.Boolean => VerseTypeKind.Bool,
            LiteralType.Null => VerseTypeKind.Null,
            _ => VerseTypeKind.Unknown
        };
    }

    private static VerseTypeKind InferTypeFromBinary(BinaryExpr binary)
    {
        var op = binary.Operator;

        if (op is "==" or "!=" or "<" or ">" or "<=" or ">=")
        {
            return VerseTypeKind.Bool;
        }

        if (op is "&&" or "||")
        {
            return VerseTypeKind.Bool;
        }

        var leftType = InferTypeFromValue(binary.Left);
        var rightType = InferTypeFromValue(binary.Right);

        if (leftType == VerseTypeKind.F64 || rightType == VerseTypeKind.F64)
        {
            return VerseTypeKind.F64;
        }

        return VerseTypeKind.I32;
    }

    private static VerseTypeKind InferTypeFromUnary(UnaryExpr unary)
    {
        if (unary.Operator == "!")
        {
            return VerseTypeKind.Bool;
        }

        return InferTypeFromValue(unary.Operand);
    }

    #endregion
}

/// <summary>
/// Verse 类型种类
/// </summary>
public enum VerseTypeKind
{
    Unknown,
    Null,
    Bool,
    I32,
    I64,
    F32,
    F64,
    String
}

/// <summary>
/// 变量信息
/// </summary>
internal sealed class VariableInfo
{
    public string Name { get; }
    public VerseTypeKind Type { get; }

    public VariableInfo(string name, VerseTypeKind type)
    {
        Name = name;
        Type = type;
    }
}
