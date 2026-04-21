using System.Text;
using System.Text.RegularExpressions;
using Gnosis.Compiler.AST;

namespace Gnosis.Compiler.Frontend;

public partial class MetaLanguageEvaluator : IMetaLanguageEvaluator
{
    #region Fields

    private readonly IMacroTable _macroTable;

    #endregion

    #region Constructors

    public MetaLanguageEvaluator(IMacroTable macroTable)
    {
        _macroTable = macroTable;
    }

    #endregion

    #region Public Methods

    public AstNode Evaluate(AstNode ast, ChannelMacros macros)
    {
        foreach (var (key, value) in macros.Macros)
        {
            if (!_macroTable.Contains(key))
            {
                _macroTable.Add(key, value);
            }
        }

        return EvaluateNode(ast);
    }

    #endregion

    #region Private Methods

    private AstNode EvaluateNode(AstNode node)
    {
        return node.Type switch
        {
            NodeType.CompilationUnit => EvaluateCompilationUnit((CompilationUnit)node),
            NodeType.ComponentDecl => node,
            NodeType.SystemDecl => EvaluateSystemDecl((SystemDecl)node),
            NodeType.WidgetDecl => EvaluateWidgetDecl((WidgetDecl)node),
            NodeType.SceneDecl => EvaluateSceneDecl((SceneDecl)node),
            NodeType.PluginDecl => node,
            NodeType.FunctionDecl => EvaluateFunctionDecl((FunctionDecl)node),
            NodeType.BlockStmt => EvaluateBlockStmt((BlockStmt)node),
            NodeType.IfStmt => EvaluateIfStmt((IfStmt)node),
            NodeType.LoopStmt => EvaluateLoopStmt((LoopStmt)node),
            NodeType.MetaBlock => EvaluateMetaBlock((MetaBlock)node),
            NodeType.StructDecl => node,
            NodeType.ForStmt => EvaluateForStmt((ForStmt)node),
            NodeType.DiscardStmt => node,
            NodeType.SwizzleExpr => EvaluateSwizzleExpr((SwizzleExpr)node),
            NodeType.UsingDecl => node,
            NodeType.UniformBindingDecl => node,
            _ => node
        };
    }

    private AstNode EvaluateCompilationUnit(CompilationUnit node)
    {
        var declarations = new List<AstNode>();

        foreach (var decl in node.Declarations)
        {
            var evaluated = EvaluateNode(decl);

            if (evaluated is CompilationUnit nestedUnit)
            {
                declarations.AddRange(nestedUnit.Declarations);
            }
            else
            {
                declarations.Add(evaluated);
            }
        }

        return node with { Declarations = declarations };
    }

    private AstNode EvaluateSystemDecl(SystemDecl node)
    {
        var methods = node.LifecycleMethods.Select(EvaluateFunctionDecl).ToList();
        return node with { LifecycleMethods = methods };
    }

    private AstNode EvaluateWidgetDecl(WidgetDecl node)
    {
        var renderMethod = node.RenderMethod is not null
            ? EvaluateFunctionDecl(node.RenderMethod)
            : null;
        return node with { RenderMethod = renderMethod };
    }

    private AstNode EvaluateSceneDecl(SceneDecl node)
    {
        var methods = node.LifecycleMethods.Select(EvaluateFunctionDecl).ToList();
        return node with { LifecycleMethods = methods };
    }

    private FunctionDecl EvaluateFunctionDecl(FunctionDecl node)
    {
        if (node.Body is null)
        {
            return node;
        }

        var body = EvaluateBlockStmt(node.Body);
        return node with { Body = body };
    }

    private BlockStmt EvaluateBlockStmt(BlockStmt node)
    {
        var statements = new List<AstNode>();

        foreach (var stmt in node.Statements)
        {
            var evaluated = EvaluateNode(stmt);

            if (evaluated is BlockStmt expandedBlock)
            {
                statements.AddRange(expandedBlock.Statements);
            }
            else
            {
                statements.Add(evaluated);
            }
        }

        return node with { Statements = statements };
    }

    private AstNode EvaluateIfStmt(IfStmt node)
    {
        var condition = EvaluateNode(node.Condition);
        var thenBlock = EvaluateNode(node.ThenBlock);
        var elseBlock = node.ElseBlock is not null ? EvaluateNode(node.ElseBlock) : null;
        return node with { Condition = condition, ThenBlock = thenBlock, ElseBlock = elseBlock };
    }

    private AstNode EvaluateLoopStmt(LoopStmt node)
    {
        var body = EvaluateBlockStmt(node.Body);
        return node with { Body = body };
    }

    private AstNode EvaluateForStmt(ForStmt stmt)
    {
        var initializer = stmt.Initializer is not null ? EvaluateNode(stmt.Initializer) : null;
        var condition = stmt.Condition is not null ? EvaluateNode(stmt.Condition) : null;
        var update = stmt.Update is not null ? EvaluateNode(stmt.Update) : null;
        var body = (BlockStmt)EvaluateNode(stmt.Body);

        return new ForStmt(stmt.Span, initializer, condition, update, body);
    }

    private AstNode EvaluateSwizzleExpr(SwizzleExpr expr)
    {
        var obj = EvaluateNode(expr.Object);
        return new SwizzleExpr(expr.Span, obj, expr.Components);
    }

    private AstNode EvaluateMetaBlock(MetaBlock node)
    {
        if (node.IsExpression)
        {
            return EvaluateMetaExpression(node.Content);
        }

        return EvaluateMetaCodeBlock(node.Content);
    }

    private AstNode EvaluateMetaExpression(string content)
    {
        var result = EvaluateMetaCode(content.Trim());

        if (result is string strResult)
        {
            return new LiteralExpr(null, LiteralType.String, strResult);
        }

        if (result is int intResult)
        {
            return new LiteralExpr(null, LiteralType.Number, intResult.ToString());
        }

        if (result is float floatResult)
        {
            return new LiteralExpr(null, LiteralType.Number, floatResult.ToString());
        }

        if (result is bool boolResult)
        {
            return new LiteralExpr(null, LiteralType.Boolean, boolResult ? "true" : "false");
        }

        return new LiteralExpr(null, LiteralType.String, result?.ToString() ?? "");
    }

    private AstNode EvaluateMetaCodeBlock(string content)
    {
        var expanded = ExpandMacros(content);
        expanded = ExpandLoopConstructs(expanded);
        expanded = ExpandConditionalConstructs(expanded);
        expanded = ExpandMatchConstructs(expanded);

        if (string.IsNullOrWhiteSpace(expanded))
        {
            return new BlockStmt(null, Array.Empty<AstNode>());
        }

        var lexer = new GgScriptLexer();
        var tokens = lexer.Tokenize(expanded);

        var parser = new GgScriptParser();
        var ast = parser.Parse(tokens);

        if (ast is CompilationUnit unit)
        {
            return new BlockStmt(null, unit.Declarations);
        }

        return ast;
    }

    private string ExpandMacros(string content)
    {
        var result = content;

        foreach (var (key, value) in _macroTable.GetAll())
        {
            result = result.Replace($"MACRO.{key}", value);
            result = result.Replace($"${key}", value);
        }

        return result;
    }

    private string ExpandLoopConstructs(string content)
    {
        var result = new StringBuilder();
        var pos = 0;

        while (pos < content.Length)
        {
            var loopStart = FindConstruct(content, "loop", pos);

            if (loopStart is null)
            {
                result.Append(content[pos..]);
                break;
            }

            result.Append(content[pos..loopStart.Start]);

            var expanded = ExpandLoop(content, loopStart);
            result.Append(expanded.Text);

            pos = expanded.End;
        }

        return result.ToString();
    }

    private string ExpandConditionalConstructs(string content)
    {
        var result = new StringBuilder();
        var pos = 0;

        while (pos < content.Length)
        {
            var ifStart = FindConstruct(content, "if", pos);

            if (ifStart is null)
            {
                result.Append(content[pos..]);
                break;
            }

            result.Append(content[pos..ifStart.Start]);

            var expanded = ExpandConditional(content, ifStart);
            result.Append(expanded.Text);

            pos = expanded.End;
        }

        return result.ToString();
    }

    private string ExpandMatchConstructs(string content)
    {
        var result = new StringBuilder();
        var pos = 0;

        while (pos < content.Length)
        {
            var matchStart = FindConstruct(content, "match", pos);

            if (matchStart is null)
            {
                result.Append(content[pos..]);
                break;
            }

            result.Append(content[pos..matchStart.Start]);

            var expanded = ExpandMatch(content, matchStart);
            result.Append(expanded.Text);

            pos = expanded.End;
        }

        return result.ToString();
    }

    private object? EvaluateMetaCode(string code)
    {
        var expanded = ExpandMacros(code);

        if (expanded.StartsWith("MACRO."))
        {
            var macroName = expanded[6..];
            var value = _macroTable.Get(macroName);

            if (value is not null)
            {
                if (int.TryParse(value, out var intVal))
                {
                    return intVal;
                }

                if (float.TryParse(value, out var floatVal))
                {
                    return floatVal;
                }

                return value;
            }
        }

        if (int.TryParse(expanded, out var intResult))
        {
            return intResult;
        }

        if (float.TryParse(expanded, out var floatResult))
        {
            return floatResult;
        }

        if (expanded == "true")
        {
            return true;
        }

        if (expanded == "false")
        {
            return false;
        }

        return expanded;
    }

    private static int FindMatchingBrace(string text, int openPos)
    {
        var depth = 0;

        for (var i = openPos; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;

                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private record ConstructLocation(int Start, int NameEnd);

    private static ConstructLocation? FindConstruct(string content, string name, int startPos)
    {
        var idx = content.IndexOf(name, startPos, StringComparison.Ordinal);

        if (idx < 0)
        {
            return null;
        }

        return new ConstructLocation(idx, idx + name.Length);
    }

    private record ExpandedResult(string Text, int End);

    private ExpandedResult ExpandLoop(string content, ConstructLocation location)
    {
        var afterLoop = location.NameEnd;

        while (afterLoop < content.Length && char.IsWhiteSpace(content[afterLoop]))
        {
            afterLoop++;
        }

        if (afterLoop >= content.Length || content[afterLoop] != '(')
        {
            return new ExpandedResult(content[location.Start..location.NameEnd], location.NameEnd);
        }

        var parenEnd = FindMatchingParen(content, afterLoop);

        if (parenEnd < 0)
        {
            return new ExpandedResult(content[location.Start..location.NameEnd], location.NameEnd);
        }

        var header = content[(afterLoop + 1)..parenEnd].Trim();
        var bodyStart = parenEnd + 1;

        while (bodyStart < content.Length && char.IsWhiteSpace(content[bodyStart]))
        {
            bodyStart++;
        }

        if (bodyStart >= content.Length || content[bodyStart] != '{')
        {
            return new ExpandedResult(content[location.Start..location.NameEnd], location.NameEnd);
        }

        var bodyEnd = FindMatchingBrace(content, bodyStart);

        if (bodyEnd < 0)
        {
            return new ExpandedResult(content[location.Start..location.NameEnd], location.NameEnd);
        }

        var body = content[(bodyStart + 1)..bodyEnd].Trim();

        var endKeyword = "end loop";
        var endIdx = content.IndexOf(endKeyword, bodyEnd + 1, StringComparison.Ordinal);
        var constructEnd = endIdx >= 0 ? endIdx + endKeyword.Length : bodyEnd + 1;

        var expandedBody = ExpandRangeLoop(header, body);

        return new ExpandedResult(expandedBody, constructEnd);
    }

    private string ExpandRangeLoop(string header, string body)
    {
        var rangeMatch = RangeLoopRegex().Match(header);

        if (rangeMatch.Success)
        {
            var varName = rangeMatch.Groups[1].Value;
            var startStr = rangeMatch.Groups[2].Value;
            var endStr = rangeMatch.Groups[3].Value;

            if (!int.TryParse(startStr, out var start) || !int.TryParse(endStr, out var end))
            {
                return body;
            }

            var sb = new StringBuilder();

            for (var i = start; i < end; i++)
            {
                var iteration = body
                    .Replace($"<%= {varName} %>", i.ToString())
                    .Replace($"<%={varName}%>", i.ToString());
                sb.AppendLine(iteration);
            }

            return sb.ToString();
        }

        var foreachMatch = ForeachLoopRegex().Match(header);

        if (foreachMatch.Success)
        {
            return body;
        }

        return body;
    }

    private ExpandedResult ExpandConditional(string content, ConstructLocation location)
    {
        var afterIf = location.NameEnd;

        while (afterIf < content.Length && char.IsWhiteSpace(content[afterIf]))
        {
            afterIf++;
        }

        if (afterIf >= content.Length || content[afterIf] != '(')
        {
            return new ExpandedResult(content[location.Start..location.NameEnd], location.NameEnd);
        }

        var parenEnd = FindMatchingParen(content, afterIf);

        if (parenEnd < 0)
        {
            return new ExpandedResult(content[location.Start..location.NameEnd], location.NameEnd);
        }

        var condition = content[(afterIf + 1)..parenEnd].Trim();
        var bodyStart = parenEnd + 1;

        while (bodyStart < content.Length && char.IsWhiteSpace(content[bodyStart]))
        {
            bodyStart++;
        }

        if (bodyStart >= content.Length || content[bodyStart] != '{')
        {
            return new ExpandedResult(content[location.Start..location.NameEnd], location.NameEnd);
        }

        var bodyEnd = FindMatchingBrace(content, bodyStart);

        if (bodyEnd < 0)
        {
            return new ExpandedResult(content[location.Start..location.NameEnd], location.NameEnd);
        }

        var body = content[(bodyStart + 1)..bodyEnd].Trim();

        var conditionResult = EvaluateCondition(condition);

        string elseBody = "";
        var elseIdx = content.IndexOf("else", bodyEnd + 1, StringComparison.Ordinal);
        var constructEnd = bodyEnd + 1;

        if (elseIdx >= 0)
        {
            var elseBraceStart = elseIdx + 4;

            while (elseBraceStart < content.Length && char.IsWhiteSpace(content[elseBraceStart]))
            {
                elseBraceStart++;
            }

            if (elseBraceStart < content.Length && content[elseBraceStart] == '{')
            {
                var elseBraceEnd = FindMatchingBrace(content, elseBraceStart);

                if (elseBraceEnd >= 0)
                {
                    elseBody = content[(elseBraceStart + 1)..elseBraceEnd].Trim();
                    constructEnd = elseBraceEnd + 1;
                }
            }
        }

        var result = conditionResult ? body : elseBody;

        return new ExpandedResult(result, constructEnd);
    }

    private ExpandedResult ExpandMatch(string content, ConstructLocation location)
    {
        var afterMatch = location.NameEnd;

        while (afterMatch < content.Length && char.IsWhiteSpace(content[afterMatch]))
        {
            afterMatch++;
        }

        var valueEnd = afterMatch;

        while (valueEnd < content.Length && content[valueEnd] != '{' && content[valueEnd] != '\n')
        {
            valueEnd++;
        }

        var matchValue = content[afterMatch..valueEnd].Trim();
        var resolvedValue = EvaluateMetaCode(matchValue)?.ToString() ?? matchValue;

        var sb = new StringBuilder();
        var pos = valueEnd;
        var defaultBody = "";

        while (pos < content.Length)
        {
            while (pos < content.Length && char.IsWhiteSpace(content[pos]))
            {
                pos++;
            }

            if (pos >= content.Length)
            {
                break;
            }

            var caseIdx = content.IndexOf("case", pos, StringComparison.Ordinal);

            if (caseIdx < 0)
            {
                break;
            }

            var afterCase = caseIdx + 4;

            while (afterCase < content.Length && char.IsWhiteSpace(content[afterCase]))
            {
                afterCase++;
            }

            var caseValueEnd = afterCase;

            while (caseValueEnd < content.Length && content[caseValueEnd] != '{' && content[caseValueEnd] != '\n')
            {
                caseValueEnd++;
            }

            var caseValue = content[afterCase..caseValueEnd].Trim();

            while (caseValueEnd < content.Length && char.IsWhiteSpace(content[caseValueEnd]))
            {
                caseValueEnd++;
            }

            if (caseValueEnd >= content.Length || content[caseValueEnd] != '{')
            {
                break;
            }

            var caseBodyEnd = FindMatchingBrace(content, caseValueEnd);

            if (caseBodyEnd < 0)
            {
                break;
            }

            var caseBody = content[(caseValueEnd + 1)..caseBodyEnd].Trim();

            if (caseValue == "_" || caseValue == "default")
            {
                defaultBody = caseBody;
            }
            else if (caseValue == resolvedValue)
            {
                sb.Append(caseBody);
            }

            pos = caseBodyEnd + 1;

            var endMatch = content.IndexOf("end match", pos, StringComparison.Ordinal);

            if (endMatch >= 0 && endMatch < (content.IndexOf("case", pos, StringComparison.Ordinal) >= 0 ? content.IndexOf("case", pos, StringComparison.Ordinal) : int.MaxValue))
            {
                pos = endMatch + "end match".Length;
                break;
            }
        }

        if (sb.Length == 0 && defaultBody.Length > 0)
        {
            sb.Append(defaultBody);
        }

        return new ExpandedResult(sb.ToString(), pos);
    }

    private bool EvaluateCondition(string condition)
    {
        var expanded = ExpandMacros(condition).Trim();

        if (expanded == "true")
        {
            return true;
        }

        if (expanded == "false")
        {
            return false;
        }

        var eqIdx = expanded.IndexOf("==", StringComparison.Ordinal);

        if (eqIdx >= 0)
        {
            var left = expanded[..eqIdx].Trim();
            var right = expanded[(eqIdx + 2)..].Trim();
            return left == right;
        }

        var neqIdx = expanded.IndexOf("!=", StringComparison.Ordinal);

        if (neqIdx >= 0)
        {
            var left = expanded[..neqIdx].Trim();
            var right = expanded[(neqIdx + 2)..].Trim();
            return left != right;
        }

        return false;
    }

    private static int FindMatchingParen(string text, int openPos)
    {
        var depth = 0;

        for (var i = openPos; i < text.Length; i++)
        {
            if (text[i] == '(')
            {
                depth++;
            }
            else if (text[i] == ')')
            {
                depth--;

                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    #endregion

    #region Generated Regex

    [GeneratedRegex(@"(\w+)\s+in\s+range\(\s*(\d+)\s*,\s*(\d+)\s*\)")]
    private static partial Regex RangeLoopRegex();

    [GeneratedRegex(@"\((\w+(?:\s*,\s*\w+)*)\)\s+in\s+(\w+)")]
    private static partial Regex ForeachLoopRegex();

    #endregion
}
