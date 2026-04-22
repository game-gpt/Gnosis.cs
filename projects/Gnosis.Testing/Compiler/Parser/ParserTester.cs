using System.Text;
using Gnosis.Toolchain.ScriptCompiler;
using Gnosis.Toolchain.ScriptCompiler.AST;
using Gnosis.Toolchain.ScriptCompiler.Diagnostics;
using Gnosis.Toolchain.ScriptCompiler.Lexer;
using Gnosis.Toolchain.ScriptCompiler.Parser;

namespace Gnosis.Testing.Compiler.Parser;

/// <summary>
/// Parser 测试工具类，支持基于文件的测试模式
/// </summary>
public class ParserTester
{
    #region 字段

    private readonly IParser _parser;
    private readonly TimeSpan _timeout;
    private readonly Action<string>? _logger;

    #endregion

    #region 构造函数

    public ParserTester(IParser parser, TimeSpan? timeout = null, Action<string>? logger = null)
    {
        _parser = parser;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
        _logger = logger;
    }

    #endregion

    #region 公共方法 - 文件驱动测试

    /// <summary>
    /// 运行基于文件的测试
    /// </summary>
    /// <param name="scriptPath">.script 文件路径</param>
    /// <param name="autoGenerateExpected">如果 expected 文件不存在，是否自动生成</param>
    /// <returns>测试结果</returns>
    public ParserFileTestResult RunFileTest(string scriptPath, bool autoGenerateExpected = true)
    {
        var expectedPath = GetExpectedPath(scriptPath);

        Log($"加载测试文件: {scriptPath}");

        var source = File.ReadAllText(scriptPath);

        Log($"源代码长度: {source.Length} 字符");

        ParserTestResult result;

        try
        {
            result = Parse(source);
        }
        catch (TimeoutException ex)
        {
            Log($"超时: {ex.Message}");
            return new ParserFileTestResult(
                null,
                null,
                false,
                $"语法分析超时（超过 {_timeout.TotalSeconds} 秒）",
                true
            );
        }
        catch (Exception ex)
        {
            Log($"异常: {ex.Message}");
            return new ParserFileTestResult(
                null,
                null,
                false,
                $"语法分析异常: {ex.Message}",
                false
            );
        }

        if (!File.Exists(expectedPath))
        {
            if (autoGenerateExpected)
            {
                Log($"生成 expected 文件: {expectedPath}");
                SaveExpectedFile(expectedPath, result.Ast, result.Errors, result.Warnings);

                return new ParserFileTestResult(
                    result.Ast,
                    result.Ast,
                    true,
                    $"已自动生成 expected 文件: {expectedPath}",
                    false
                );
            }

            return new ParserFileTestResult(
                result.Ast,
                null,
                false,
                $"expected 文件不存在: {expectedPath}",
                false
            );
        }

        Log($"加载 expected 文件: {expectedPath}");
        var expected = LoadExpectedFile(expectedPath);

        var diff = CompareResults(result.Ast, result.Errors, result.Warnings, expected);

        if (diff.IsMatch)
        {
            Log("测试通过");
        }
        else
        {
            Log($"测试失败: {diff.Message}");
        }

        return new ParserFileTestResult(
            result.Ast,
            expected.Ast,
            diff.IsMatch,
            diff.Message,
            false
        );
    }

    /// <summary>
    /// 批量运行目录下所有 .script 文件的测试
    /// </summary>
    public IReadOnlyList<ParserFileTestResult> RunDirectoryTests(
        string directory,
        bool autoGenerateExpected = true,
        string searchPattern = "*.script")
    {
        var results = new List<ParserFileTestResult>();

        if (!Directory.Exists(directory))
        {
            Log($"目录不存在: {directory}");
            return results;
        }

        var scriptFiles = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);

        Log($"找到 {scriptFiles.Length} 个测试文件");

        foreach (var scriptPath in scriptFiles)
        {
            Log($"\n--- 测试: {Path.GetFileName(scriptPath)} ---");
            var result = RunFileTest(scriptPath, autoGenerateExpected);
            results.Add(result);
        }

        var passed = results.Count(r => r.IsMatch);
        var failed = results.Count - passed;
        Log($"\n=== 测试汇总: {passed} 通过, {failed} 失败 ===");

        return results;
    }

    #endregion

    #region 公共方法 - 直接语法分析

    /// <summary>
    /// 解析源代码为 AST
    /// </summary>
    public ParserTestResult Parse(string source)
    {
        Log($"开始词法分析");

        var diagnostics = new DiagnosticSink();
        var lexer = new GameScriptLexer(diagnostics);
        var tokens = lexer.Tokenize(source);

        Log($"词法分析完成，Token 数量: {tokens.Count}");
        Log($"开始语法分析");

        var parserWithDiagnostics = new GameScriptParser(diagnostics);
        var ast = ExecuteWithTimeout(() => parserWithDiagnostics.Parse(tokens));

        Log($"语法分析完成，AST 类型: {ast.GetType().Name}");

        return new ParserTestResult(ast, diagnostics.GetErrors().ToList(), diagnostics.GetWarnings().ToList());
    }

    #endregion

    #region 文件格式

    /// <summary>
    /// 获取 expected 文件路径
    /// </summary>
    public static string GetExpectedPath(string scriptPath)
    {
        return Path.ChangeExtension(scriptPath, ".expected");
    }

    /// <summary>
    /// 保存 expected 文件
    /// </summary>
    public static void SaveExpectedFile(
        string filePath,
        AstNode ast,
        IReadOnlyList<Diagnostic> errors,
        IReadOnlyList<Diagnostic> warnings)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Gnosis Parser Expected Output");
        sb.AppendLine($"# 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("# 格式: 文本格式的 AST 结构");
        sb.AppendLine();

        if (errors.Count > 0)
        {
            sb.AppendLine("# === 错误 ===");
            foreach (var error in errors)
            {
                sb.AppendLine($"# ERROR: {error.Message}");
            }
            sb.AppendLine();
        }

        if (warnings.Count > 0)
        {
            sb.AppendLine("# === 警告 ===");
            foreach (var warning in warnings)
            {
                sb.AppendLine($"# WARNING: {warning.Message}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("# === AST ===");
        var astText = SerializeAstToText(ast);
        sb.AppendLine(astText);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, sb.ToString());
    }

    /// <summary>
    /// 加载 expected 文件
    /// </summary>
    public static ParserExpectedResult LoadExpectedFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var errors = new List<string>();
        var warnings = new List<string>();
        var astLines = new List<string>();
        var inAst = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("# ERROR: "))
            {
                errors.Add(trimmed[9..]);
                continue;
            }

            if (trimmed.StartsWith("# WARNING: "))
            {
                warnings.Add(trimmed[11..]);
                continue;
            }

            if (trimmed.StartsWith("# === AST ==="))
            {
                inAst = true;
                continue;
            }

            if (inAst && !trimmed.StartsWith("#"))
            {
                astLines.Add(line);
            }
        }

        var astText = string.Join("\n", astLines);
        var ast = DeserializeAstFromText(astText);

        return new ParserExpectedResult(ast, errors, warnings);
    }

    private static string SerializeAstToText(AstNode ast)
    {
        var sb = new StringBuilder();
        SerializeNode(ast, sb, 0);
        return sb.ToString();
    }

    private static void SerializeNode(AstNode node, StringBuilder sb, int indent)
    {
        var prefix = new string(' ', indent * 2);

        switch (node)
        {
            case CompilationUnit unit:
                sb.AppendLine($"{prefix}CompilationUnit:");
                foreach (var decl in unit.Declarations)
                {
                    SerializeNode(decl, sb, indent + 1);
                }
                break;

            case ImportDecl import:
                sb.AppendLine($"{prefix}ImportDecl:");
                sb.AppendLine($"{prefix}  ModulePath: {import.ModulePath}");
                if (import.Alias != null)
                {
                    sb.AppendLine($"{prefix}  Alias: {import.Alias}");
                }
                break;

            case VariableDecl varDecl:
                sb.AppendLine($"{prefix}VariableDecl:");
                sb.AppendLine($"{prefix}  Name: {varDecl.Name}");
                sb.AppendLine($"{prefix}  IsMutable: {varDecl.IsMutable}");
                if (varDecl.VarType != null)
                {
                    sb.AppendLine($"{prefix}  Type: {SerializeType(varDecl.VarType)}");
                }
                if (varDecl.Initializer != null)
                {
                    sb.AppendLine($"{prefix}  Initializer:");
                    SerializeNode(varDecl.Initializer, sb, indent + 2);
                }
                break;

            case ComponentDecl comp:
                sb.AppendLine($"{prefix}ComponentDecl:");
                sb.AppendLine($"{prefix}  Name: {comp.Name}");
                if (comp.Attributes.Count > 0)
                {
                    sb.AppendLine($"{prefix}  Attributes: {string.Join(", ", comp.Attributes.Select(a => a.Name))}");
                }
                sb.AppendLine($"{prefix}  Fields:");
                foreach (var field in comp.Fields)
                {
                    SerializeNode(field, sb, indent + 2);
                }
                break;

            case FieldDecl field:
                sb.AppendLine($"{prefix}FieldDecl:");
                sb.AppendLine($"{prefix}  Name: {field.Name}");
                sb.AppendLine($"{prefix}  Type: {SerializeType(field.FieldType)}");
                if (field.DefaultValue != null)
                {
                    sb.AppendLine($"{prefix}  DefaultValue:");
                    SerializeNode(field.DefaultValue, sb, indent + 2);
                }
                break;

            case SystemDecl sys:
                sb.AppendLine($"{prefix}SystemDecl:");
                sb.AppendLine($"{prefix}  Name: {sys.Name}");
                if (sys.Attributes.Count > 0)
                {
                    sb.AppendLine($"{prefix}  Attributes: {string.Join(", ", sys.Attributes.Select(a => a.Name))}");
                }
                if (sys.Queries.Count > 0)
                {
                    sb.AppendLine($"{prefix}  Queries: {sys.Queries.Count}");
                }
                if (sys.LifecycleMethods.Count > 0)
                {
                    sb.AppendLine($"{prefix}  LifecycleMethods:");
                    foreach (var method in sys.LifecycleMethods)
                    {
                        SerializeNode(method, sb, indent + 2);
                    }
                }
                break;

            case FunctionDecl func:
                sb.AppendLine($"{prefix}FunctionDecl:");
                sb.AppendLine($"{prefix}  Name: {func.Name}");
                if (func.Parameters.Count > 0)
                {
                    sb.AppendLine($"{prefix}  Parameters: {string.Join(", ", func.Parameters.Select(p => $"{p.Name}: {SerializeType(p.ParamType)}"))}");
                }
                if (func.ReturnType != null)
                {
                    sb.AppendLine($"{prefix}  ReturnType: {SerializeType(func.ReturnType)}");
                }
                if (func.Body != null)
                {
                    sb.AppendLine($"{prefix}  Body:");
                    SerializeNode(func.Body, sb, indent + 2);
                }
                break;

            case BlockStmt block:
                sb.AppendLine($"{prefix}BlockStmt:");
                foreach (var stmt in block.Statements)
                {
                    SerializeNode(stmt, sb, indent + 1);
                }
                break;

            case IfStatement ifStmt:
                sb.AppendLine($"{prefix}IfStatement:");
                sb.AppendLine($"{prefix}  Condition:");
                SerializeNode(ifStmt.Condition, sb, indent + 2);
                sb.AppendLine($"{prefix}  Then:");
                SerializeNode(ifStmt.ThenBlock, sb, indent + 2);
                if (ifStmt.ElseBlock != null)
                {
                    sb.AppendLine($"{prefix}  Else:");
                    SerializeNode(ifStmt.ElseBlock, sb, indent + 2);
                }
                break;

            case WhileStmt whileStmt:
                sb.AppendLine($"{prefix}WhileStmt:");
                sb.AppendLine($"{prefix}  Condition:");
                SerializeNode(whileStmt.Condition, sb, indent + 2);
                sb.AppendLine($"{prefix}  Body:");
                SerializeNode(whileStmt.Body, sb, indent + 2);
                break;

            case LoopStmt loopStmt:
                sb.AppendLine($"{prefix}LoopStmt:");
                if (loopStmt.IteratorName != null)
                {
                    sb.AppendLine($"{prefix}  Iterator: {loopStmt.IteratorName}");
                }
                if (loopStmt.Iterable != null)
                {
                    sb.AppendLine($"{prefix}  Iterable:");
                    SerializeNode(loopStmt.Iterable, sb, indent + 2);
                }
                sb.AppendLine($"{prefix}  Body:");
                SerializeNode(loopStmt.Body, sb, indent + 2);
                break;

            case ReturnStatement ret:
                sb.AppendLine($"{prefix}ReturnStatement:");
                if (ret.Value != null)
                {
                    sb.AppendLine($"{prefix}  Value:");
                    SerializeNode(ret.Value, sb, indent + 2);
                }
                break;

            case BinaryExpr binary:
                sb.AppendLine($"{prefix}BinaryExpr:");
                sb.AppendLine($"{prefix}  Operator: {binary.Operator}");
                sb.AppendLine($"{prefix}  Left:");
                SerializeNode(binary.Left, sb, indent + 2);
                sb.AppendLine($"{prefix}  Right:");
                SerializeNode(binary.Right, sb, indent + 2);
                break;

            case AssignmentExpr assign:
                sb.AppendLine($"{prefix}AssignmentExpr:");
                sb.AppendLine($"{prefix}  Operator: {assign.Operator}");
                sb.AppendLine($"{prefix}  Target:");
                SerializeNode(assign.Target, sb, indent + 2);
                sb.AppendLine($"{prefix}  Value:");
                SerializeNode(assign.Value, sb, indent + 2);
                break;

            case MemberAccessExpr member:
                sb.AppendLine($"{prefix}MemberAccessExpr:");
                sb.AppendLine($"{prefix}  Member: {member.MemberName}");
                sb.AppendLine($"{prefix}  Object:");
                SerializeNode(member.Object, sb, indent + 2);
                break;

            case TermCallExpression call:
                sb.AppendLine($"{prefix}CallExpr:");
                sb.AppendLine($"{prefix}  Callee:");
                SerializeNode(call.Callee, sb, indent + 2);
                if (call.Arguments.Count > 0)
                {
                    sb.AppendLine($"{prefix}  Arguments:");
                    foreach (var arg in call.Arguments)
                    {
                        SerializeNode(arg, sb, indent + 2);
                    }
                }
                break;

            case IdentifierNode ident:
                sb.AppendLine($"{prefix}Identifier: {ident.Name}");
                break;

            case LiteralExpr lit:
                sb.AppendLine($"{prefix}Literal: {lit.LiteralKind} = {lit.Value}");
                break;

            case WidgetDecl widget:
                sb.AppendLine($"{prefix}WidgetDecl:");
                sb.AppendLine($"{prefix}  Name: {widget.Name}");
                if (widget.Properties.Count > 0)
                {
                    sb.AppendLine($"{prefix}  Properties:");
                    foreach (var prop in widget.Properties)
                    {
                        SerializeNode(prop, sb, indent + 2);
                    }
                }
                break;

            case PluginDecl plugin:
                sb.AppendLine($"{prefix}PluginDecl:");
                sb.AppendLine($"{prefix}  Name: {plugin.Name}");
                break;

            case QueryExpr query:
                sb.AppendLine($"{prefix}QueryExpr:");
                sb.AppendLine($"{prefix}  Kind: {query.Kind}");
                sb.AppendLine($"{prefix}  Components: {string.Join(", ", query.ComponentTypes.Select(SerializeType))}");
                break;

            case LambdaExpr lambda:
                sb.AppendLine($"{prefix}LambdaExpr:");
                if (lambda.Parameters.Count > 0)
                {
                    sb.AppendLine($"{prefix}  Parameters: {string.Join(", ", lambda.Parameters.Select(p => p.Name))}");
                }
                sb.AppendLine($"{prefix}  Body:");
                SerializeNode(lambda.Body, sb, indent + 2);
                break;

            case TermIndexExpression index:
                sb.AppendLine($"{prefix}IndexExpr:");
                sb.AppendLine($"{prefix}  Object:");
                SerializeNode(index.Object, sb, indent + 2);
                sb.AppendLine($"{prefix}  Index:");
                SerializeNode(index.Index, sb, indent + 2);
                break;

            case TermUnaryExpression unary:
                sb.AppendLine($"{prefix}UnaryExpr:");
                sb.AppendLine($"{prefix}  Operator: {unary.Operator}");
                sb.AppendLine($"{prefix}  Operand:");
                SerializeNode(unary.Operand, sb, indent + 2);
                break;

            case TermExpressionStatement exprStmt:
                sb.AppendLine($"{prefix}ExpressionStatement:");
                SerializeNode(exprStmt.Expression, sb, indent + 1);
                break;

            default:
                sb.AppendLine($"{prefix}{node.GetType().Name}");
                break;
        }
    }

    private static string SerializeType(TypeAnnotation type)
    {
        if (type.GenericArguments.Count == 0)
        {
            return type.Name;
        }

        return $"{type.Name}<{string.Join(", ", type.GenericArguments.Select(SerializeType))}>";
    }

    private static AstNode DeserializeAstFromText(string text)
    {
        var lines = text.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        var index = 0;
        return ParseNode(lines, ref index, 0) ?? new CompilationUnit([], "");
    }

    private static AstNode? ParseNode(List<string> lines, ref int index, int expectedIndent)
    {
        if (index >= lines.Count)
        {
            return null;
        }

        var line = lines[index];
        var currentIndent = CountLeadingSpaces(line) / 2;

        if (currentIndent < expectedIndent)
        {
            return null;
        }

        var content = line.Trim();
        index++;

        if (content.StartsWith("CompilationUnit:"))
        {
            var declarations = new List<AstNode>();
            while (index < lines.Count && CountLeadingSpaces(lines[index]) / 2 > currentIndent)
            {
                var decl = ParseNode(lines, ref index, currentIndent + 1);
                if (decl != null)
                {
                    declarations.Add(decl);
                }
            }
            return new CompilationUnit(declarations, "");
        }

        if (content.StartsWith("ImportDecl:"))
        {
            var modulePath = ParseFieldValue(lines, ref index, currentIndent, "ModulePath");
            var alias = ParseFieldValue(lines, ref index, currentIndent, "Alias");
            return new ImportDecl(modulePath ?? "", alias);
        }

        if (content.StartsWith("VariableDecl:"))
        {
            var name = ParseFieldValue(lines, ref index, currentIndent, "Name") ?? "";
            var isMutable = ParseFieldValue(lines, ref index, currentIndent, "IsMutable") == "True";
            var typeStr = ParseFieldValue(lines, ref index, currentIndent, "Type");
            var type = typeStr != null ? ParseTypeAnnotation(typeStr) : null;
            return new VariableDecl(name, type, null, isMutable);
        }

        if (content.StartsWith("ComponentDecl:"))
        {
            var name = ParseFieldValue(lines, ref index, currentIndent, "Name") ?? "";
            return new ComponentDecl(name, [], []);
        }

        if (content.StartsWith("SystemDecl:"))
        {
            var name = ParseFieldValue(lines, ref index, currentIndent, "Name") ?? "";
            return new SystemDecl(name, [], [], []);
        }

        if (content.StartsWith("FunctionDecl:"))
        {
            var name = ParseFieldValue(lines, ref index, currentIndent, "Name") ?? "";
            return new FunctionDecl(name, [], null, new BlockStmt([]), []);
        }

        if (content.StartsWith("Identifier:"))
        {
            var value = content["Identifier:".Length..].Trim();
            return new IdentifierNode(value);
        }

        if (content.StartsWith("Literal:"))
        {
            var parts = content["Literal:".Length..].Trim().Split(" = ", 2);
            var litType = Enum.Parse<LiteralType>(parts[0]);
            return new LiteralExpr(litType, parts.Length > 1 ? parts[1] : "");
        }

        return null;
    }

    private static string? ParseFieldValue(List<string> lines, ref int index, int parentIndent, string fieldName)
    {
        while (index < lines.Count)
        {
            var line = lines[index];
            var currentIndent = CountLeadingSpaces(line) / 2;

            if (currentIndent <= parentIndent)
            {
                return null;
            }

            var content = line.Trim();

            if (content.StartsWith($"{fieldName}:"))
            {
                index++;
                return content[$"{fieldName}:".Length..].Trim();
            }

            if (!content.StartsWith(fieldName))
            {
                return null;
            }

            index++;
        }

        return null;
    }

    private static TypeAnnotation ParseTypeAnnotation(string typeStr)
    {
        var ltIndex = typeStr.IndexOf('<');
        if (ltIndex < 0)
        {
            return new TypeAnnotation(typeStr, []);
        }

        var name = typeStr[..ltIndex];
        var inner = typeStr[(ltIndex + 1)..^1];
        var args = inner.Split(", ").Select(ParseTypeAnnotation).ToList();
        return new TypeAnnotation(name, args);
    }

    private static int CountLeadingSpaces(string line)
    {
        var count = 0;
        foreach (var c in line)
        {
            if (c == ' ')
            {
                count++;
            }
            else
            {
                break;
            }
        }
        return count;
    }

    #endregion

    #region 结果比较

    private ParserDiffResult CompareResults(
        AstNode actualAst,
        IReadOnlyList<Diagnostic> actualErrors,
        IReadOnlyList<Diagnostic> actualWarnings,
        ParserExpectedResult expected)
    {
        if (actualErrors.Count != expected.Errors.Count)
        {
            return new ParserDiffResult(
                false,
                $"错误数量不匹配: 期望 {expected.Errors.Count}，实际 {actualErrors.Count}"
            );
        }

        if (actualWarnings.Count != expected.Warnings.Count)
        {
            return new ParserDiffResult(
                false,
                $"警告数量不匹配: 期望 {expected.Warnings.Count}，实际 {actualWarnings.Count}"
            );
        }

        var astMatch = CompareAstNodes(actualAst, expected.Ast);

        if (!astMatch.IsMatch)
        {
            return new ParserDiffResult(false, astMatch.Message);
        }

        return new ParserDiffResult(true, "匹配成功");
    }

    private ParserDiffResult CompareAstNodes(AstNode actual, AstNode? expected)
    {
        if (expected == null)
        {
            return new ParserDiffResult(true, "无预期 AST");
        }

        var actualText = SerializeAstToText(actual);
        var expectedText = SerializeAstToText(expected);

        var actualLines = actualText.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        var expectedLines = expectedText.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        if (actualLines.Count != expectedLines.Count)
        {
            return new ParserDiffResult(
                false,
                $"AST 行数不匹配: 期望 {expectedLines.Count}，实际 {actualLines.Count}"
            );
        }

        for (var i = 0; i < actualLines.Count; i++)
        {
            if (actualLines[i].Trim() != expectedLines[i].Trim())
            {
                return new ParserDiffResult(
                    false,
                    $"AST 第 {i + 1} 行不匹配:\n  期望: {expectedLines[i]}\n  实际: {actualLines[i]}"
                );
            }
        }

        return new ParserDiffResult(true, "AST 匹配");
    }

    #endregion

    #region 私有方法

    private T ExecuteWithTimeout<T>(Func<T> action)
    {
        var task = Task.Run(action);

        if (!task.Wait(_timeout))
        {
            throw new TimeoutException($"语法分析超时，超过 {_timeout.TotalSeconds} 秒");
        }

        return task.Result;
    }

    private void Log(string message)
    {
        _logger?.Invoke($"[ParserTester] {message}");
    }

    #endregion
}

/// <summary>
/// Parser 测试结果
/// </summary>
public record ParserTestResult(
    AstNode Ast,
    IReadOnlyList<Diagnostic> Errors,
    IReadOnlyList<Diagnostic> Warnings
);

/// <summary>
/// Parser 文件测试结果
/// </summary>
public record ParserFileTestResult(
    AstNode? ActualAst,
    AstNode? ExpectedAst,
    bool IsMatch,
    string Message,
    bool IsTimeout
);

/// <summary>
/// Parser expected 文件内容
/// </summary>
public record ParserExpectedResult(
    AstNode? Ast,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings
);

/// <summary>
/// Parser 差异结果
/// </summary>
internal record ParserDiffResult(bool IsMatch, string Message);

/// <summary>
/// Parser 测试异常
/// </summary>
public class ParserTestException : Exception
{
    public ParserTestException(string message) : base(message)
    {
    }
}
