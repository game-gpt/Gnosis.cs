using System.Text;
using Gnosis.Compiler.Diagnostics;

namespace Gnosis.Compiler.Lexer.Testing;

/// <summary>
/// Lexer 测试工具类，支持基于文件的测试模式
/// </summary>
public class LexerTester
{
    #region 字段

    private readonly ILexer _lexer;
    private readonly TimeSpan _timeout;
    private readonly Action<string>? _logger;

    #endregion

    #region 构造函数

    public LexerTester(ILexer lexer, TimeSpan? timeout = null, Action<string>? logger = null)
    {
        _lexer = lexer;
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
    public LexerFileTestResult RunFileTest(string scriptPath, bool autoGenerateExpected = true)
    {
        var expectedPath = GetExpectedPath(scriptPath);

        Log($"加载测试文件: {scriptPath}");

        var source = File.ReadAllText(scriptPath);

        Log($"源代码长度: {source.Length} 字符");

        LexerTestResult result;

        try
        {
            result = Tokenize(source);
        }
        catch (TimeoutException ex)
        {
            Log($"超时: {ex.Message}");
            return new LexerFileTestResult(
                null,
                null,
                false,
                $"词法分析超时（超过 {_timeout.TotalSeconds} 秒）",
                true
            );
        }
        catch (Exception ex)
        {
            Log($"异常: {ex.Message}");
            return new LexerFileTestResult(
                null,
                null,
                false,
                $"词法分析异常: {ex.Message}",
                false
            );
        }

        if (!File.Exists(expectedPath))
        {
            if (autoGenerateExpected)
            {
                Log($"生成 expected 文件: {expectedPath}");
                SaveExpectedFile(expectedPath, result.Tokens, result.Errors, result.Warnings);

                return new LexerFileTestResult(
                    result.Tokens,
                    result.Tokens,
                    true,
                    $"已自动生成 expected 文件: {expectedPath}",
                    false
                );
            }

            return new LexerFileTestResult(
                result.Tokens,
                null,
                false,
                $"expected 文件不存在: {expectedPath}",
                false
            );
        }

        Log($"加载 expected 文件: {expectedPath}");
        var expected = LoadExpectedFile(expectedPath);

        var diff = CompareResults(result.Tokens, result.Errors, result.Warnings, expected);

        if (diff.IsMatch)
        {
            Log("测试通过");
        }
        else
        {
            Log($"测试失败: {diff.Message}");
        }

        return new LexerFileTestResult(
            result.Tokens,
            expected.Tokens,
            diff.IsMatch,
            diff.Message,
            false
        );
    }

    /// <summary>
    /// 批量运行目录下所有 .script 文件的测试
    /// </summary>
    public IReadOnlyList<LexerFileTestResult> RunDirectoryTests(
        string directory,
        bool autoGenerateExpected = true,
        string searchPattern = "*.script")
    {
        var results = new List<LexerFileTestResult>();

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

    #region 公共方法 - 直接词法分析

    /// <summary>
    /// 对源代码进行词法分析
    /// </summary>
    public LexerTestResult Tokenize(string source)
    {
        Log($"开始词法分析");

        var diagnostics = new DiagnosticSink();
        var lexerWithDiagnostics = new GameScriptLexer(diagnostics);

        IReadOnlyList<Token> tokens = ExecuteWithTimeout(() => lexerWithDiagnostics.Tokenize(source));

        var result = new LexerTestResult(tokens, diagnostics.GetErrors().ToList(), diagnostics.GetWarnings().ToList());

        Log($"词法分析完成，Token 数量: {tokens.Count}");

        return result;
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
        IReadOnlyList<Token> tokens,
        IReadOnlyList<Diagnostic> errors,
        IReadOnlyList<Diagnostic> warnings)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Gnosis Lexer Expected Output");
        sb.AppendLine($"# 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("# 格式: TokenType\\tValue\\tLine\\tColumn");
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

        sb.AppendLine("# === Tokens ===");
        foreach (var token in tokens)
        {
            var escapedValue = EscapeValue(token.Value);
            sb.AppendLine($"{token.TokenType}\t{escapedValue}\t{token.Line}\t{token.Column}");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, sb.ToString());
    }

    /// <summary>
    /// 加载 expected 文件
    /// </summary>
    public static LexerExpectedResult LoadExpectedFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        var tokens = new List<Token>();
        var errors = new List<string>();
        var warnings = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
            {
                if (trimmed.StartsWith("# ERROR: "))
                {
                    errors.Add(trimmed[9..]);
                }
                else if (trimmed.StartsWith("# WARNING: "))
                {
                    warnings.Add(trimmed[11..]);
                }
                continue;
            }

            var parts = trimmed.Split('\t');
            if (parts.Length >= 4)
            {
                var type = Enum.Parse<TokenType>(parts[0]);
                var value = UnescapeValue(parts[1]);
                var lineNum = int.Parse(parts[2]);
                var column = int.Parse(parts[3]);
                tokens.Add(new Token(type, value, lineNum, column));
            }
        }

        return new LexerExpectedResult(tokens, errors, warnings);
    }

    private static string EscapeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var sb = new StringBuilder();
        foreach (var c in value)
        {
            switch (c)
            {
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    private static string UnescapeValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var sb = new StringBuilder();
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                switch (value[i + 1])
                {
                    case 'n':
                        sb.Append('\n');
                        i++;
                        break;
                    case 'r':
                        sb.Append('\r');
                        i++;
                        break;
                    case 't':
                        sb.Append('\t');
                        i++;
                        break;
                    case '\\':
                        sb.Append('\\');
                        i++;
                        break;
                    default:
                        sb.Append(value[i]);
                        break;
                }
            }
            else
            {
                sb.Append(value[i]);
            }
        }
        return sb.ToString();
    }

    #endregion

    #region 结果比较

    private LexerDiffResult CompareResults(
        IReadOnlyList<Token> actualTokens,
        IReadOnlyList<Diagnostic> actualErrors,
        IReadOnlyList<Diagnostic> actualWarnings,
        LexerExpectedResult expected)
    {
        if (actualTokens.Count != expected.Tokens.Count)
        {
            return new LexerDiffResult(
                false,
                $"Token 数量不匹配: 期望 {expected.Tokens.Count}，实际 {actualTokens.Count}"
            );
        }

        for (var i = 0; i < actualTokens.Count; i++)
        {
            var actual = actualTokens[i];
            var expectedToken = expected.Tokens[i];

            if (actual.TokenType != expectedToken.TokenType)
            {
                return new LexerDiffResult(
                    false,
                    $"Token[{i}] 类型不匹配: 期望 {expectedToken.TokenType}，实际 {actual.TokenType}"
                );
            }

            if (actual.Value != expectedToken.Value)
            {
                return new LexerDiffResult(
                    false,
                    $"Token[{i}] 值不匹配: 期望 '{EscapeValue(expectedToken.Value)}'，实际 '{EscapeValue(actual.Value)}'"
                );
            }

            if (actual.Line != expectedToken.Line)
            {
                return new LexerDiffResult(
                    false,
                    $"Token[{i}] 行号不匹配: 期望 {expectedToken.Line}，实际 {actual.Line}"
                );
            }

            if (actual.Column != expectedToken.Column)
            {
                return new LexerDiffResult(
                    false,
                    $"Token[{i}] 列号不匹配: 期望 {expectedToken.Column}，实际 {actual.Column}"
                );
            }
        }

        if (actualErrors.Count != expected.Errors.Count)
        {
            return new LexerDiffResult(
                false,
                $"错误数量不匹配: 期望 {expected.Errors.Count}，实际 {actualErrors.Count}"
            );
        }

        if (actualWarnings.Count != expected.Warnings.Count)
        {
            return new LexerDiffResult(
                false,
                $"警告数量不匹配: 期望 {expected.Warnings.Count}，实际 {actualWarnings.Count}"
            );
        }

        return new LexerDiffResult(true, "匹配成功");
    }

    #endregion

    #region 私有方法

    private T ExecuteWithTimeout<T>(Func<T> action)
    {
        var task = Task.Run(action);

        if (!task.Wait(_timeout))
        {
            throw new TimeoutException($"词法分析超时，超过 {_timeout.TotalSeconds} 秒");
        }

        return task.Result;
    }

    private void Log(string message)
    {
        _logger?.Invoke($"[LexerTester] {message}");
    }

    #endregion
}

/// <summary>
/// Lexer 测试结果
/// </summary>
public record LexerTestResult(
    IReadOnlyList<Token> Tokens,
    IReadOnlyList<Diagnostic> Errors,
    IReadOnlyList<Diagnostic> Warnings
);

/// <summary>
/// Lexer 文件测试结果
/// </summary>
public record LexerFileTestResult(
    IReadOnlyList<Token>? ActualTokens,
    IReadOnlyList<Token>? ExpectedTokens,
    bool IsMatch,
    string Message,
    bool IsTimeout
);

/// <summary>
/// Lexer expected 文件内容
/// </summary>
public record LexerExpectedResult(
    IReadOnlyList<Token> Tokens,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings
);

/// <summary>
/// Lexer 差异结果
/// </summary>
internal record LexerDiffResult(bool IsMatch, string Message);

/// <summary>
/// Lexer 测试异常
/// </summary>
public class LexerTestException : Exception
{
    public LexerTestException(string message) : base(message)
    {
    }
}
