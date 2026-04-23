using Oak.Diagnostics;
using Oak.Verse.AST;
using Gnosis.IR.Graph;
using Gnosis.IR.Instruction;

namespace Gnosis.Toolchain.VerseCompiler.Backend;

/// <summary>
/// Verse IR 生成器，将 Verse AST 转换为 Gnosis IR 模块
/// </summary>
public sealed class VerseIrGenerator
{
    #region 字段

    private readonly DiagnosticSink _diagnostics;
    private IrModule _module = null!;
    private IrFunction _currentFunction = null!;
    private BasicBlock _currentBlock = null!;
    private readonly Dictionary<string, IrValue> _variables = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BasicBlock> _labelBlocks = new(StringComparer.Ordinal);
    private readonly List<(IrValue Condition, BasicBlock TrueTarget, BasicBlock FalseTarget)> _pendingBranches = [];
    private int _tempCounter;
    private int _blockCounter;
    private string _currentFilePath = string.Empty;

    #endregion

    #region 构造函数

    public VerseIrGenerator(DiagnosticSink diagnostics)
    {
        _diagnostics = diagnostics;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 将 Verse 编译单元转换为 Gnosis IR 模块
    /// </summary>
    public IrModule Generate(CompilationUnit unit)
    {
        _module = new IrModule(unit.FilePath ?? "verse_module");
        _variables.Clear();
        _labelBlocks.Clear();
        _tempCounter = 0;
        _blockCounter = 0;
        _currentFilePath = unit.FilePath ?? string.Empty;

        CollectLabels(unit);

        foreach (var decl in unit.Declarations)
        {
            GenerateTopLevelDecl(decl);
        }

        return _module;
    }

    #endregion

    #region 标签收集

    private void CollectLabels(CompilationUnit unit)
    {
        foreach (var decl in unit.Declarations)
        {
            CollectLabelsFromNode(decl);
        }
    }

    private void CollectLabelsFromNode(AstNode node)
    {
        switch (node.Kind)
        {
            case NodeType.SceneDecl:
                var scene = (SceneDecl)node;
                _labelBlocks[$"scene_{scene.Name}"] = CreateBlock($"scene_{scene.Name}");
                foreach (var stmt in scene.Body)
                {
                    CollectLabelsFromNode(stmt);
                }
                break;

            case NodeType.LabelDecl:
                var label = (LabelDecl)node;
                _labelBlocks[$"label_{label.Name}"] = CreateBlock($"label_{label.Name}");
                break;
        }
    }

    #endregion

    #region 顶层声明

    private void GenerateTopLevelDecl(AstNode node)
    {
        switch (node.Kind)
        {
            case NodeType.SceneDecl:
                GenerateSceneDecl((SceneDecl)node);
                break;

            case NodeType.LabelDecl:
                GenerateLabelDecl((LabelDecl)node);
                break;

            case NodeType.SetStmt:
            case NodeType.IfStmt:
            case NodeType.JumpStmt:
            case NodeType.CallStmt:
            case NodeType.ReturnStmt:
            case NodeType.PauseStmt:
            case NodeType.WaitStmt:
            case NodeType.MenuDecl:
            case NodeType.CommandCall:
            case NodeType.DialogueLine:
            case NodeType.NarrationLine:
                EnsureMainFunction();
                GenerateStatement(node);
                break;
        }
    }

    private void GenerateSceneDecl(SceneDecl scene)
    {
        var functionName = $"verse_scene_{scene.Name}";
        _currentFunction = _module.CreateFunction(functionName, IrType.Void);
        _currentBlock = _currentFunction.EntryBlock;
        _currentBlock.Label = $"scene_{scene.Name}";
        _variables.Clear();

        foreach (var stmt in scene.Body)
        {
            GenerateStatement(stmt);
        }

        if (!_currentBlock.IsTerminated)
        {
            _currentBlock.Append(IrInstruction.Return());
        }
    }

    private void GenerateLabelDecl(LabelDecl label)
    {
        if (_currentFunction is null)
        {
            EnsureMainFunction();
        }

        if (_labelBlocks.TryGetValue($"label_{label.Name}", out var block))
        {
            if (!_currentBlock.IsTerminated)
            {
                _currentBlock.Append(IrInstruction.Branch(block));
            }

            _currentBlock = block;
            _currentFunction.InsertBlock(_currentFunction.Blocks.Count, block);
        }
    }

    #endregion

    #region 语句生成

    private void GenerateStatement(AstNode node)
    {
        switch (node.Kind)
        {
            case NodeType.DialogueLine:
                GenerateDialogueLine((DialogueLine)node);
                break;

            case NodeType.NarrationLine:
                GenerateNarrationLine((NarrationLine)node);
                break;

            case NodeType.SetStmt:
                GenerateSetStmt((SetStmt)node);
                break;

            case NodeType.IfStmt:
                GenerateIfStmt((IfStmt)node);
                break;

            case NodeType.JumpStmt:
                GenerateJumpStmt((JumpStmt)node);
                break;

            case NodeType.CallStmt:
                GenerateCallStmt((CallStmt)node);
                break;

            case NodeType.ReturnStmt:
                _currentBlock.Append(IrInstruction.Return());
                break;

            case NodeType.PauseStmt:
                GeneratePauseStmt((PauseStmt)node);
                break;

            case NodeType.WaitStmt:
                GenerateWaitStmt((WaitStmt)node);
                break;

            case NodeType.MenuDecl:
                GenerateMenuDecl((MenuDecl)node);
                break;

            case NodeType.CommandCall:
                GenerateCommandCall((CommandCall)node);
                break;

            case NodeType.LabelDecl:
                GenerateLabelDecl((LabelDecl)node);
                break;
        }
    }

    private void GenerateDialogueLine(DialogueLine dialogue)
    {
        var speakerConst = NewTemp(IrType.String);
        _currentBlock.Append(IrInstruction.CallNative(
            speakerConst,
            "story_dialogue_show",
            [GenerateStringConstant(dialogue.Speaker ?? ""),
             GenerateStringConstant(dialogue.Text),
             GenerateStringConstant(dialogue.Emotion ?? "")]));
    }

    private void GenerateNarrationLine(NarrationLine narration)
    {
        var result = NewTemp(IrType.Void);
        _currentBlock.Append(IrInstruction.CallNative(
            result,
            "story_dialogue_show",
            [GenerateStringConstant(""),
             GenerateStringConstant(narration.Text),
             GenerateStringConstant("")]));
    }

    private void GenerateSetStmt(SetStmt set)
    {
        var value = GenerateExpression(set.Value);

        if (!_variables.TryGetValue(set.VariableName, out var variable))
        {
            variable = NewTemp(InferIrType(set.Value), set.VariableName);
            _variables[set.VariableName] = variable;
        }

        switch (set.Operator)
        {
            case "=":
                _currentBlock.Append(IrInstruction.Store(value, variable));
                break;

            case "+=":
                var addResult = NewTemp(value.Type);
                _currentBlock.Append(IrInstruction.Add(addResult, variable, value));
                _currentBlock.Append(IrInstruction.Store(addResult, variable));
                break;

            case "-=":
                var subResult = NewTemp(value.Type);
                _currentBlock.Append(IrInstruction.Sub(subResult, variable, value));
                _currentBlock.Append(IrInstruction.Store(subResult, variable));
                break;

            case "*=":
                var mulResult = NewTemp(value.Type);
                _currentBlock.Append(IrInstruction.Mul(mulResult, variable, value));
                _currentBlock.Append(IrInstruction.Store(mulResult, variable));
                break;

            case "/=":
                var divResult = NewTemp(value.Type);
                _currentBlock.Append(IrInstruction.Div(divResult, variable, value));
                _currentBlock.Append(IrInstruction.Store(divResult, variable));
                break;

            default:
                _diagnostics.AddWarning(
                    _currentFilePath,
                    set.Span,
                    "VS0601",
                    $"不支持的复合赋值运算符: {set.Operator}");
                break;
        }
    }

    private void GenerateIfStmt(IfStmt ifStmt)
    {
        var condition = GenerateExpression(ifStmt.Condition);

        var thenBlock = CreateBlock("if_then");
        var elseBlock = CreateBlock("if_else");
        var mergeBlock = CreateBlock("if_merge");

        _currentBlock.Append(IrInstruction.ConditionalBranch(condition, thenBlock, elseBlock));

        _currentBlock = thenBlock;
        _currentFunction.InsertBlock(_currentFunction.Blocks.Count, thenBlock);
        foreach (var stmt in ifStmt.ThenBody)
        {
            GenerateStatement(stmt);
        }

        if (!_currentBlock.IsTerminated)
        {
            _currentBlock.Append(IrInstruction.Branch(mergeBlock));
        }

        _currentBlock = elseBlock;
        _currentFunction.InsertBlock(_currentFunction.Blocks.Count, elseBlock);

        if (ifStmt.ElseBranch is not null)
        {
            foreach (var stmt in ifStmt.ElseBranch.Body)
            {
                GenerateStatement(stmt);
            }
        }

        foreach (var elif in ifStmt.ElifBranches)
        {
            if (!_currentBlock.IsTerminated)
            {
                var elifThenBlock = CreateBlock("elif_then");
                var elifElseBlock = CreateBlock("elif_else");

                var elifCondition = GenerateExpression(elif.Condition);
                _currentBlock.Append(IrInstruction.ConditionalBranch(elifCondition, elifThenBlock, elifElseBlock));

                _currentBlock = elifThenBlock;
                _currentFunction.InsertBlock(_currentFunction.Blocks.Count, elifThenBlock);
                foreach (var stmt in elif.Body)
                {
                    GenerateStatement(stmt);
                }

                if (!_currentBlock.IsTerminated)
                {
                    _currentBlock.Append(IrInstruction.Branch(mergeBlock));
                }

                _currentBlock = elifElseBlock;
                _currentFunction.InsertBlock(_currentFunction.Blocks.Count, elifElseBlock);
            }
        }

        if (!_currentBlock.IsTerminated)
        {
            _currentBlock.Append(IrInstruction.Branch(mergeBlock));
        }

        _currentBlock = mergeBlock;
        _currentFunction.InsertBlock(_currentFunction.Blocks.Count, mergeBlock);
    }

    private void GenerateJumpStmt(JumpStmt jump)
    {
        if (jump.Condition is not null)
        {
            var condition = GenerateExpression(jump.Condition);
            var targetBlock = ResolveJumpTarget(jump.Target);
            var continueBlock = CreateBlock("jump_skip");

            _currentBlock.Append(IrInstruction.ConditionalBranch(condition, targetBlock, continueBlock));

            _currentBlock = continueBlock;
            _currentFunction.InsertBlock(_currentFunction.Blocks.Count, continueBlock);
        }
        else
        {
            var targetBlock = ResolveJumpTarget(jump.Target);
            _currentBlock.Append(IrInstruction.Branch(targetBlock));
        }
    }

    private void GenerateCallStmt(CallStmt call)
    {
        var args = new List<IrValue>();
        foreach (var arg in call.Arguments)
        {
            args.Add(GenerateExpression(arg));
        }

        var result = NewTemp(IrType.Void);
        _currentBlock.Append(IrInstruction.CallNative(result, $"verse_call_{call.Target}", args));
    }

    private void GeneratePauseStmt(PauseStmt pause)
    {
        var result = NewTemp(IrType.Void);
        var duration = pause.Duration ?? 0.0;
        _currentBlock.Append(IrInstruction.CallNative(
            result,
            "verse_pause",
            [GenerateF64Constant(duration)]));
    }

    private void GenerateWaitStmt(WaitStmt wait)
    {
        var result = NewTemp(IrType.Void);
        _currentBlock.Append(IrInstruction.CallNative(
            result,
            "verse_wait",
            [GenerateF64Constant(wait.Duration)]));
    }

    private void GenerateMenuDecl(MenuDecl menu)
    {
        foreach (var item in menu.Items)
        {
            if (item.Condition is not null)
            {
                var condition = GenerateExpression(item.Condition);
                var itemBlock = CreateBlock("menu_item");
                var skipBlock = CreateBlock("menu_skip");

                _currentBlock.Append(IrInstruction.ConditionalBranch(condition, itemBlock, skipBlock));

                _currentBlock = itemBlock;
                _currentFunction.InsertBlock(_currentFunction.Blocks.Count, itemBlock);
            }

            var choiceResult = NewTemp(IrType.Void);
            _currentBlock.Append(IrInstruction.CallNative(
                choiceResult,
                "verse_menu_choice",
                [GenerateStringConstant(item.Text)]));

            foreach (var stmt in item.Body)
            {
                GenerateStatement(stmt);
            }

            if (item.Condition is not null)
            {
                if (!_currentBlock.IsTerminated)
                {
                    var mergeBlock = CreateBlock("menu_merge");
                    _currentBlock.Append(IrInstruction.Branch(mergeBlock));

                    _currentBlock = (BasicBlock)_currentFunction.Blocks
                        .First(b => b.Label == "menu_skip" || b.Label.StartsWith("menu_skip_"));
                    _currentFunction.InsertBlock(_currentFunction.Blocks.Count, _currentBlock);

                    _currentBlock = mergeBlock;
                    _currentFunction.InsertBlock(_currentFunction.Blocks.Count, mergeBlock);
                }
            }
        }
    }

    private void GenerateCommandCall(CommandCall command)
    {
        var args = new List<IrValue>();

        foreach (var arg in command.Arguments)
        {
            args.Add(GenerateExpression(arg.Value));
        }

        var result = NewTemp(IrType.Void);
        _currentBlock.Append(IrInstruction.CallNative(result, $"story_{command.CommandName}", args));
    }

    #endregion

    #region 表达式生成

    private IrValue GenerateExpression(AstNode expr)
    {
        return expr.Kind switch
        {
            NodeType.LiteralExpr => GenerateLiteralExpr((LiteralExpr)expr),
            NodeType.IdentifierExpr => GenerateIdentifierExpr((IdentifierExpr)expr),
            NodeType.BinaryExpr => GenerateBinaryExpr((BinaryExpr)expr),
            NodeType.UnaryExpr => GenerateUnaryExpr((UnaryExpr)expr),
            NodeType.MemberAccessExpr => GenerateMemberAccessExpr((MemberAccessExpr)expr),
            NodeType.AssignmentExpr => GenerateAssignmentExpr((AssignmentExpr)expr),
            _ => GenerateUnknownExpr(expr)
        };
    }

    private IrValue GenerateLiteralExpr(LiteralExpr literal)
    {
        return literal.LiteralKind switch
        {
            LiteralType.Number => GenerateNumberLiteral(literal.Value),
            LiteralType.String => GenerateStringConstant(literal.Value),
            LiteralType.Boolean => GenerateBoolLiteral(literal.Value == "true"),
            LiteralType.Null => GenerateNullLiteral(),
            _ => GenerateNullLiteral()
        };
    }

    private IrValue GenerateNumberLiteral(string value)
    {
        if (value.EndsWith('f') || value.EndsWith('F'))
        {
            var fVal = float.TryParse(value.AsSpan(0, value.Length - 1), out var fv) ? fv : 0.0f;
            var result = NewTemp(IrType.F32);
            _currentBlock.Append(IrInstruction.CallNative(result, "push_f32", [GenerateF64Constant(fVal)]));
            return result;
        }

        if (value.Contains('.') || value.Contains('e') || value.Contains('E'))
        {
            var dVal = double.TryParse(value, out var dv) ? dv : 0.0;
            var result = NewTemp(IrType.F64);
            _currentBlock.Append(IrInstruction.CallNative(result, "push_f64", [GenerateF64Constant(dVal)]));
            return result;
        }

        var iVal = int.TryParse(value, out var iv) ? iv : 0;
        var intResult = NewTemp(IrType.I32);
        _currentBlock.Append(IrInstruction.CallNative(intResult, "push_i32", [GenerateI32Constant(iVal)]));
        return intResult;
    }

    private IrValue GenerateBoolLiteral(bool value)
    {
        var result = NewTemp(IrType.Bool);
        _currentBlock.Append(IrInstruction.CallNative(result, "push_bool", [GenerateI32Constant(value ? 1 : 0)]));
        return result;
    }

    private IrValue GenerateNullLiteral()
    {
        var result = NewTemp(IrType.Void);
        _currentBlock.Append(IrInstruction.CallNative(result, "push_null", []));
        return result;
    }

    private IrValue GenerateIdentifierExpr(IdentifierExpr identifier)
    {
        if (_variables.TryGetValue(identifier.Name, out var variable))
        {
            var result = NewTemp(variable.Type);
            _currentBlock.Append(IrInstruction.Load(result, variable));
            return result;
        }

        _diagnostics.AddWarning(
            _currentFilePath,
            identifier.Span,
            "VS0620",
            $"未定义的标识符: {identifier.Name}");

        return GenerateNullLiteral();
    }

    private IrValue GenerateBinaryExpr(BinaryExpr binary)
    {
        var left = GenerateExpression(binary.Left);
        var right = GenerateExpression(binary.Right);

        var resultType = InferBinaryResultType(binary.Operator, left.Type, right.Type);
        var result = NewTemp(resultType);

        var instruction = binary.Operator switch
        {
            "+" => IrInstruction.Add(result, left, right),
            "-" => IrInstruction.Sub(result, left, right),
            "*" => IrInstruction.Mul(result, left, right),
            "/" => IrInstruction.Div(result, left, right),
            "%" => IrInstruction.Mod(result, left, right),
            "==" => IrInstruction.Equal(result, left, right),
            "!=" => IrInstruction.NotEqual(result, left, right),
            "<" => IrInstruction.Less(result, left, right),
            ">" => IrInstruction.Greater(result, left, right),
            "<=" => IrInstruction.LessEqual(result, left, right),
            ">=" => IrInstruction.GreaterEqual(result, left, right),
            "&&" => IrInstruction.LogicalAnd(result, left, right),
            "||" => IrInstruction.LogicalOr(result, left, right),
            _ => null
        };

        if (instruction is not null)
        {
            _currentBlock.Append(instruction);
        }
        else
        {
            _diagnostics.AddWarning(
                _currentFilePath,
                binary.Span,
                "VS0621",
                $"不支持的二元运算符: {binary.Operator}");
        }

        return result;
    }

    private IrValue GenerateUnaryExpr(UnaryExpr unary)
    {
        var operand = GenerateExpression(unary.Operand);
        var result = NewTemp(operand.Type);

        switch (unary.Operator)
        {
            case "-":
                _currentBlock.Append(IrInstruction.Neg(result, operand));
                break;

            case "!":
                _currentBlock.Append(IrInstruction.LogicalNot(result, operand));
                break;

            default:
                _diagnostics.AddWarning(
                    _currentFilePath,
                    unary.Span,
                    "VS0622",
                    $"不支持的一元运算符: {unary.Operator}");
                break;
        }

        return result;
    }

    private IrValue GenerateMemberAccessExpr(MemberAccessExpr memberAccess)
    {
        var obj = GenerateExpression(memberAccess.Object);
        var result = NewTemp(IrType.Void);
        _currentBlock.Append(IrInstruction.GetField(result, obj, memberAccess.Member));
        return result;
    }

    private IrValue GenerateAssignmentExpr(AssignmentExpr assignment)
    {
        var value = GenerateExpression(assignment.Value);

        if (assignment.Target is IdentifierExpr identifier)
        {
            if (!_variables.TryGetValue(identifier.Name, out var variable))
            {
                variable = NewTemp(value.Type, identifier.Name);
                _variables[identifier.Name] = variable;
            }

            _currentBlock.Append(IrInstruction.Store(value, variable));
            return variable;
        }

        _diagnostics.AddWarning(
            _currentFilePath,
            assignment.Span,
            "VS0623",
            "不支持的非标识符赋值目标");
        return value;
    }

    private IrValue GenerateUnknownExpr(AstNode expr)
    {
        _diagnostics.AddWarning(
            _currentFilePath,
            expr.Span,
            "VS0624",
            $"不支持的表达式类型: {expr.Kind}");
        return GenerateNullLiteral();
    }

    #endregion

    #region 辅助方法

    private void EnsureMainFunction()
    {
        if (_currentFunction is null)
        {
            _currentFunction = _module.CreateFunction("verse_main", IrType.Void);
            _currentBlock = _currentFunction.EntryBlock;
            _currentBlock.Label = "entry";
        }
    }

    private BasicBlock CreateBlock(string label)
    {
        var block = _currentFunction.CreateBlock($"{label}_{_blockCounter}");
        _blockCounter++;
        return block;
    }

    private IrValue NewTemp(IrType type, string? name = null)
    {
        var value = _currentFunction.CreateValue(type, name ?? $"t{_tempCounter}");
        _tempCounter++;
        return value;
    }

    private IrValue GenerateStringConstant(string value)
    {
        var result = NewTemp(IrType.String);
        _currentBlock.Append(IrInstruction.CallNative(result, "push_string", [GenerateI32Constant(value.GetHashCode())]));
        return result;
    }

    private IrValue GenerateI32Constant(int value)
    {
        var result = NewTemp(IrType.I32);
        _currentBlock.Append(IrInstruction.CallNative(result, "push_i32", []));
        return result;
    }

    private IrValue GenerateF64Constant(double value)
    {
        var result = NewTemp(IrType.F64);
        _currentBlock.Append(IrInstruction.CallNative(result, "push_f64", []));
        return result;
    }

    private BasicBlock ResolveJumpTarget(string target)
    {
        if (_labelBlocks.TryGetValue($"label_{target}", out var labelBlock))
        {
            return labelBlock;
        }

        if (_labelBlocks.TryGetValue($"scene_{target}", out var sceneBlock))
        {
            return sceneBlock;
        }

        var newBlock = CreateBlock($"jump_{target}");
        _labelBlocks[$"jump_{target}"] = newBlock;
        return newBlock;
    }

    private static IrType InferIrType(AstNode expr)
    {
        return expr.Kind switch
        {
            NodeType.LiteralExpr => ((LiteralExpr)expr).LiteralKind switch
            {
                LiteralType.Number => IrType.F64,
                LiteralType.String => IrType.String,
                LiteralType.Boolean => IrType.Bool,
                LiteralType.Null => IrType.Void,
                _ => IrType.Void
            },
            NodeType.IdentifierExpr => IrType.Void,
            NodeType.BinaryExpr => IrType.I32,
            NodeType.UnaryExpr => IrType.I32,
            _ => IrType.Void
        };
    }

    private static IrType InferBinaryResultType(string op, IrType leftType, IrType rightType)
    {
        if (op is "==" or "!=" or "<" or ">" or "<=" or ">=" or "&&" or "||")
        {
            return IrType.Bool;
        }

        if (leftType == IrType.F64 || rightType == IrType.F64)
        {
            return IrType.F64;
        }

        if (leftType == IrType.F32 || rightType == IrType.F32)
        {
            return IrType.F32;
        }

        return IrType.I32;
    }

    #endregion
}
