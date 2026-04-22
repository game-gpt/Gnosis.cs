using Gnosis.Testing.Compiler.Lexer;
using Gnosis.Toolchain.ScriptCompiler.Lexer;
using NUnit.Framework;

namespace Gnosis.Tests.Compiler;

/// <summary>
/// GameShader 词法分析器测试
/// </summary>
[TestFixture]
public class GameShaderLexerTests
{
    private LexerTester _tester = null!;

    [SetUp]
    public void SetUp()
    {
        var lexer = new GameShaderLexer();
        _tester = new LexerTester(lexer, TimeSpan.FromSeconds(5), Console.WriteLine);
    }

    #region 基本 Token 类型

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
    public void Tokenize_Keywords_ReturnsKeywordTokens()
    {
        var result = _tester.Tokenize("let struct import return if else for while loop discard");

        var keywords = result.Tokens.Where(t => t.TokenType == TokenType.Keyword).ToList();
        Assert.That(keywords.Count, Is.EqualTo(11));
        Assert.That(keywords[0].Value, Is.EqualTo("let"));
        Assert.That(keywords[1].Value, Is.EqualTo("struct"));
        Assert.That(keywords[2].Value, Is.EqualTo("import"));
        Assert.That(keywords[3].Value, Is.EqualTo("return"));
        Assert.That(keywords[4].Value, Is.EqualTo("if"));
        Assert.That(keywords[5].Value, Is.EqualTo("else"));
        Assert.That(keywords[6].Value, Is.EqualTo("for"));
        Assert.That(keywords[7].Value, Is.EqualTo("while"));
        Assert.That(keywords[8].Value, Is.EqualTo("loop"));
        Assert.That(keywords[9].Value, Is.EqualTo("discard"));
    }

    [Test]
    public void Tokenize_ShaderSpecificKeywords_ReturnsKeywordTokens()
    {
        var result = _tester.Tokenize("micro");

        var keywords = result.Tokens.Where(t => t.TokenType == TokenType.Keyword).ToList();
        Assert.That(keywords.Count, Is.EqualTo(1));
        Assert.That(keywords[0].Value, Is.EqualTo("micro"));
    }

    [Test]
    public void Tokenize_Identifiers_ReturnsIdentifierTokens()
    {
        var result = _tester.Tokenize("position color main uv normal");

        var identifiers = result.Tokens.Where(t => t.TokenType == TokenType.Identifier).ToList();
        Assert.That(identifiers.Count, Is.EqualTo(5));
        Assert.That(identifiers[0].Value, Is.EqualTo("position"));
        Assert.That(identifiers[1].Value, Is.EqualTo("color"));
    }

    [Test]
    public void Tokenize_Numbers_ReturnsNumberTokens()
    {
        var result = _tester.Tokenize("42 3.14 2.5e10 100f 50i 200u");

        var numbers = result.Tokens.Where(t => t.TokenType == TokenType.Number).ToList();
        Assert.That(numbers.Count, Is.EqualTo(6));
        Assert.That(numbers[0].Value, Is.EqualTo("42"));
        Assert.That(numbers[1].Value, Is.EqualTo("3.14"));
    }

    [Test]
    public void Tokenize_Strings_ReturnsStringTokens()
    {
        var result = _tester.Tokenize("\"hello\" 'world'");

        var strings = result.Tokens.Where(t => t.TokenType == TokenType.String).ToList();
        Assert.That(strings.Count, Is.EqualTo(2));
        Assert.That(strings[0].Value, Is.EqualTo("hello"));
        Assert.That(strings[1].Value, Is.EqualTo("world"));
    }

    [Test]
    public void Tokenize_Literals_ReturnsLiteralTokens()
    {
        var result = _tester.Tokenize("true false null");

        var literals = result.Tokens.Where(t => t.TokenType == TokenType.Literal).ToList();
        Assert.That(literals.Count, Is.EqualTo(3));
        Assert.That(literals[0].Value, Is.EqualTo("true"));
        Assert.That(literals[1].Value, Is.EqualTo("false"));
        Assert.That(literals[2].Value, Is.EqualTo("null"));
    }

    #endregion

    #region 运算符与分隔符

    [Test]
    public void Tokenize_Operators_ReturnsOperatorTokens()
    {
        var result = _tester.Tokenize("+ - * / % == != <= >= && || -> =>");

        var operators = result.Tokens.Where(t => t.TokenType == TokenType.Operator).ToList();
        Assert.That(operators.Count, Is.GreaterThanOrEqualTo(12));
    }

    [Test]
    public void Tokenize_Delimiters_ReturnsDelimiterTokens()
    {
        var result = _tester.Tokenize("( ) { } [ ] , ; .");

        var delimiters = result.Tokens.Where(t => t.TokenType == TokenType.Delimiter).ToList();
        Assert.That(delimiters.Count, Is.EqualTo(10));
    }

    [Test]
    public void Tokenize_ShiftOperators_ReturnsCorrectTokens()
    {
        var result = _tester.Tokenize("<< >>");

        var operators = result.Tokens.Where(t => t.TokenType == TokenType.Operator).ToList();
        Assert.That(operators.Count, Is.EqualTo(2));
        Assert.That(operators[0].Value, Is.EqualTo("<<"));
        Assert.That(operators[1].Value, Is.EqualTo(">>"));
    }

    #endregion

    #region 注释

    [Test]
    public void Tokenize_LineComment_IgnoresComment()
    {
        var result = _tester.Tokenize("let x = 1 # 这是注释\nlet y = 2");

        var keywords = result.Tokens.Where(t => t.TokenType == TokenType.Keyword).ToList();
        Assert.That(keywords.Count, Is.EqualTo(2));
    }

    [Test]
    public void Tokenize_BlockComment_IgnoresComment()
    {
        var result = _tester.Tokenize("let x = 1 <# 块注释 #> let y = 2");

        var keywords = result.Tokens.Where(t => t.TokenType == TokenType.Keyword).ToList();
        Assert.That(keywords.Count, Is.EqualTo(2));
    }

    [Test]
    public void Tokenize_NestedBlockComment_HandlesNesting()
    {
        var result = _tester.Tokenize("let x = 1 <# 外层 <# 内层 #> #> let y = 2");

        var keywords = result.Tokens.Where(t => t.TokenType == TokenType.Keyword).ToList();
        Assert.That(keywords.Count, Is.EqualTo(2));
    }

    #endregion

    #region 属性与元数据块

    [Test]
    public void Tokenize_Attribute_ReturnsAttributeToken()
    {
        var result = _tester.Tokenize("[Vertex]");

        var attributes = result.Tokens.Where(t => t.TokenType == TokenType.Attribute).ToList();
        Assert.That(attributes.Count, Is.EqualTo(1));
        Assert.That(attributes[0].Value, Is.EqualTo("[Vertex]"));
    }

    [Test]
    public void Tokenize_MetaBlock_ReturnsMetaBlockToken()
    {
        var result = _tester.Tokenize("<% layout(location = 0) %>");

        var metaBlocks = result.Tokens.Where(t => t.TokenType == TokenType.MetaBlockStart).ToList();
        Assert.That(metaBlocks.Count, Is.EqualTo(1));
    }

    [Test]
    public void Tokenize_MetaExpression_ReturnsMetaExpressionToken()
    {
        var result = _tester.Tokenize("<%= some_expr %>");

        var metaExprs = result.Tokens.Where(t => t.TokenType == TokenType.MetaExpression).ToList();
        Assert.That(metaExprs.Count, Is.EqualTo(1));
    }

    #endregion

    #region 行号追踪

    [Test]
    public void Tokenize_MultipleLines_TracksLineNumbers()
    {
        var result = _tester.Tokenize("let x\nlet y\nlet z");

        var keywords = result.Tokens.Where(t => t.TokenType == TokenType.Keyword).ToList();
        Assert.That(keywords[0].Line, Is.EqualTo(1));
        Assert.That(keywords[1].Line, Is.EqualTo(2));
        Assert.That(keywords[2].Line, Is.EqualTo(3));
    }

    #endregion

    #region 着色器代码片段

    [Test]
    public void Tokenize_SimpleVertexShader_ReturnsCorrectTokens()
    {
        var source = @"
            [Vertex]
            micro main(vec3 position) -> vec4
            {
                return vec4(position, 1.0);
            }
        ";

        var result = _tester.Tokenize(source);

        Assert.That(result.Tokens.Count, Is.GreaterThan(10));
        Assert.That(result.Tokens.Any(t => t.TokenType == TokenType.Attribute && t.Value.Contains("Vertex")), Is.True);
        Assert.That(result.Tokens.Any(t => t.TokenType == TokenType.Keyword && t.Value == "micro"), Is.True);
        Assert.That(result.Tokens.Any(t => t.TokenType == TokenType.Keyword && t.Value == "return"), Is.True);
    }

    [Test]
    public void Tokenize_SimpleFragmentShader_ReturnsCorrectTokens()
    {
        var source = @"
            [Fragment]
            micro main() -> vec4
            {
                return vec4(1.0, 0.0, 0.0, 1.0);
            }
        ";

        var result = _tester.Tokenize(source);

        Assert.That(result.Tokens.Any(t => t.TokenType == TokenType.Attribute && t.Value.Contains("Fragment")), Is.True);
    }

    [Test]
    public void Tokenize_StructDefinition_ReturnsCorrectTokens()
    {
        var source = @"
            struct VertexOutput {
                vec4 position;
                vec2 uv;
            }
        ";

        var result = _tester.Tokenize(source);

        Assert.That(result.Tokens.Any(t => t.TokenType == TokenType.Keyword && t.Value == "struct"), Is.True);
        Assert.That(result.Tokens.Any(t => t.TokenType == TokenType.Identifier && t.Value == "VertexOutput"), Is.True);
    }

    [Test]
    public void Tokenize_UniformBinding_ReturnsCorrectTokens()
    {
        var source = @"
            let uniforms: UniformBuffer;
        ";

        var result = _tester.Tokenize(source);

        Assert.That(result.Tokens.Any(t => t.TokenType == TokenType.Keyword && t.Value == "let"), Is.True);
        Assert.That(result.Tokens.Any(t => t.TokenType == TokenType.Identifier && t.Value == "UniformBuffer"), Is.True);
    }

    #endregion

    #region 错误处理

    [Test]
    public void Tokenize_InvalidCharacter_GeneratesError()
    {
        var result = _tester.Tokenize("@");

        Assert.That(result.Errors.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Tokenize_UnclosedString_GeneratesError()
    {
        var result = _tester.Tokenize("\"unclosed");

        Assert.That(result.Errors.Count, Is.GreaterThan(0));
    }

    #endregion
}
