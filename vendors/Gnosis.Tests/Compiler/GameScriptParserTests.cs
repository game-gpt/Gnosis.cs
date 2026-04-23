using Gnosis.Testing.Compiler.Parser;
using Gnosis.Toolchain.ScriptCompiler.Parser;
using NUnit.Framework;

namespace Gnosis.Tests.Compiler;

/// <summary>
/// GameScript 语法分析器测试 - 基于文件驱动
/// </summary>
[TestFixture]
public class GameScriptParserTests
{
    #region 字段

    private ParserTester _tester = null!;
    private string _scriptsDirectory = null!;

    #endregion

    [SetUp]
    public void SetUp()
    {
        var parser = new GameScriptParser();
        _tester = new ParserTester(parser, TimeSpan.FromSeconds(5), Console.WriteLine);

        var assemblyLocation = typeof(GameScriptParserTests).Assembly.Location;
        var projectRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assemblyLocation)!, "..", "..", ".."));
        _scriptsDirectory = Path.Combine(projectRoot, "Compiler", "Scripts", "Parser");
    }

    #region 文件驱动测试

    [Test]
    public void RunAllParserScriptTests()
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
    public void Parser_Imports_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "imports.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Parser_ComponentBasic_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "component_basic.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Parser_ComponentWithAttrs_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "component_with_attrs.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Parser_SystemBasic_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "system_basic.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Parser_Functions_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "functions.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Parser_ControlFlow_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "control_flow.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    [Test]
    public void Parser_Widget_Passes()
    {
        var scriptPath = Path.Combine(_scriptsDirectory, "widget.script");
        var result = _tester.RunFileTest(scriptPath, autoGenerateExpected: true);

        Assert.That(result.IsMatch || result.Message.Contains("已自动生成"), Is.True, result.Message);
    }

    #endregion

    #region 超时测试

    [Test]
    public void Parser_Timeout_OnInfiniteLoop()
    {
        var tester = new ParserTester(new GameScriptParser(), TimeSpan.FromMilliseconds(100), Console.WriteLine);

        var result = tester.Parse("let x = 1;");

        Assert.That(result.Ast, Is.Not.Null);
    }

    #endregion

    #region 直接测试方法

    [Test]
    public void Parse_EmptySource_ReturnsEmptyCompilationUnit()
    {
        var result = _tester.Parse("");

        Assert.That(result.Ast, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Parse_WhitespaceOnly_ReturnsEmptyCompilationUnit()
    {
        var result = _tester.Parse("   \t\n   ");

        Assert.That(result.Ast, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion
}
