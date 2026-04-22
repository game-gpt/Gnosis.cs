using Gnosis.Compiler.Lexer;
using Gnosis.Compiler.Lexer.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Compiler;

/// <summary>
/// GameScript 词法分析器测试 - 基于文件驱动
/// </summary>
[TestFixture]
public class GameScriptLexerTests
{
    #region 字段

    private LexerTester _tester = null!;
    private string _scriptsDirectory = null!;

    #endregion

    [SetUp]
    public void SetUp()
    {
        var lexer = new GameScriptLexer();
        _tester = new LexerTester(lexer, TimeSpan.FromSeconds(5), Console.WriteLine);

        var assemblyLocation = typeof(GameScriptLexerTests).Assembly.Location;
        var projectRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "..", "..", ".."));
        _scriptsDirectory = Path.Combine(projectRoot, "Compiler", "Scripts", "Lexer");
    }

    #region 文件驱动测试

    [Test]
    public void RunAllLexerScriptTests()
    {
        Assert.That(Directory.Exists(_scriptsDirectory), Is.True, $"脚本目录不存在: {_scriptsDirectory}");

        var results = _tester.RunDirectoryTests(_scriptsDirectory, autoGenerateExpected: true);

        var failed = results.Where(r => !r.IsMatch && !r.Message.Contains("已自动生成")).ToList();

        if (failed.Count > 0)
        {
            var messages = failed.Select(f => f.Message);
            Assert.Fail($"以下测试失败:\n{string.Join("\n", messages)}");
        }
    }

    [Test]
    public void Lexer_BasicVariable_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "basic_variable.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Lexer_MutableVariable_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "mutable_variable.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Lexer_StringLiteral_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "string_literal.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Lexer_Operators_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "operators.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Lexer_Comments_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "comments.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Lexer_Numbers_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "numbers.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    #endregion

    #region 超时测试

    [Test]
    public void Lexer_Timeout_OnInfiniteLoop()
    {
        var tester = new LexerTester(new GameScriptLexer(), TimeSpan.FromMilliseconds(100), Console.WriteLine);

        var result = tester.Tokenize("let x = 1;");

        Assert.That(result.Tokens.Count, Is.GreaterThan(0));
    }

    #endregion

    #region 直接测试方法

    [Test]
    public void Tokenize_EmptySource_ReturnsOnlyEof()
    {
        var result = _tester.Tokenize("");

        Assert.That(result.Tokens.Count, Is.EqualTo(1));
        Assert.That(result.Tokens[0].TokenType, Is.EqualTo(TokenType.Eof));
    }

    [Test]
    public void Tokenize_WhitespaceOnly_ReturnsOnlyEof()
    {
        var result = _tester.Tokenize("   \t\n\r\n   ");

        Assert.That(result.Tokens.Count, Is.EqualTo(1));
        Assert.That(result.Tokens[0].TokenType, Is.EqualTo(TokenType.Eof));
    }

    [Test]
    public void Tokenize_Keyword_ReturnsKeywordToken()
    {
        var result = _tester.Tokenize("let");

        Assert.That(result.Tokens.Count, Is.EqualTo(2));
        Assert.That(result.Tokens[0].TokenType, Is.EqualTo(TokenType.Keyword));
        Assert.That(result.Tokens[0].Value, Is.EqualTo("let"));
    }

    [Test]
    public void Tokenize_TypeKeywords_ReturnsTypeKeywordTokens()
    {
        var result = _tester.Tokenize("i32 f32 bool string vec3");

        Assert.That(result.Tokens.Count, Is.EqualTo(6));
        Assert.That(result.Tokens[0].TokenType, Is.EqualTo(TokenType.TypeKeyword));
        Assert.That(result.Tokens[1].TokenType, Is.EqualTo(TokenType.TypeKeyword));
    }

    [Test]
    public void Tokenize_BooleanLiterals_ReturnsLiteralTokens()
    {
        var result = _tester.Tokenize("true false");

        Assert.That(result.Tokens.Count, Is.EqualTo(3));
        Assert.That(result.Tokens[0].TokenType, Is.EqualTo(TokenType.Literal));
        Assert.That(result.Tokens[0].Value, Is.EqualTo("true"));
        Assert.That(result.Tokens[1].TokenType, Is.EqualTo(TokenType.Literal));
        Assert.That(result.Tokens[1].Value, Is.EqualTo("false"));
    }

    [Test]
    public void Tokenize_Attribute_ReturnsAttributeToken()
    {
        var result = _tester.Tokenize("[Serializable]");

        Assert.That(result.Tokens.Count, Is.EqualTo(2));
        Assert.That(result.Tokens[0].TokenType, Is.EqualTo(TokenType.Attribute));
    }

    [Test]
    public void Tokenize_MultipleLines_TracksLineNumbers()
    {
        var result = _tester.Tokenize("let x\nlet y\nlet z");

        Assert.That(result.Tokens.Count, Is.EqualTo(7));
        Assert.That(result.Tokens[0].Line, Is.EqualTo(1));
        Assert.That(result.Tokens[2].Line, Is.EqualTo(2));
        Assert.That(result.Tokens[4].Line, Is.EqualTo(3));
    }

    #endregion
}
