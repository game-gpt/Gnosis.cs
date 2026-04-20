using Gnosis.Compiler.Frontend;
using Gnosis.Compiler.ValueObjects;
using NUnit.Framework;

namespace Gnosis.Tests.Compiler;

[TestFixture]
public class GgScriptLexerTests
{
    private GgScriptLexer _lexer = null!;

    [SetUp]
    public void Setup()
    {
        _lexer = new GgScriptLexer();
    }

    [Test]
    public void Tokenize_EmptySource_ReturnsEof()
    {
        var tokens = _lexer.Tokenize("");
        Assert.That(tokens.Count, Is.EqualTo(1));
        Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Eof));
    }

    [Test]
    public void Tokenize_Keywords_IdentifiedCorrectly()
    {
        var source = "let mut micro component system query widget scene plugin import export return if else loop while";
        var tokens = _lexer.Tokenize(source);

        var keywords = tokens.Where(t => t.TokenType == TokenType.Keyword).Select(t => t.Value).ToList();

        Assert.That(keywords, Does.Contain("let"));
        Assert.That(keywords, Does.Contain("mut"));
        Assert.That(keywords, Does.Contain("micro"));
        Assert.That(keywords, Does.Contain("component"));
        Assert.That(keywords, Does.Contain("system"));
        Assert.That(keywords, Does.Contain("query"));
        Assert.That(keywords, Does.Contain("widget"));
        Assert.That(keywords, Does.Contain("scene"));
        Assert.That(keywords, Does.Contain("plugin"));
        Assert.That(keywords, Does.Contain("import"));
        Assert.That(keywords, Does.Contain("export"));
        Assert.That(keywords, Does.Contain("return"));
        Assert.That(keywords, Does.Contain("if"));
        Assert.That(keywords, Does.Contain("else"));
        Assert.That(keywords, Does.Contain("loop"));
        Assert.That(keywords, Does.Contain("while"));
    }

    [Test]
    public void Tokenize_TypeKeywords_IdentifiedCorrectly()
    {
        var source = "i32 f32 f64 bool string Entity";
        var tokens = _lexer.Tokenize(source);

        var typeKeywords = tokens.Where(t => t.TokenType == TokenType.TypeKeyword).Select(t => t.Value).ToList();

        Assert.That(typeKeywords, Does.Contain("i32"));
        Assert.That(typeKeywords, Does.Contain("f32"));
        Assert.That(typeKeywords, Does.Contain("f64"));
        Assert.That(typeKeywords, Does.Contain("bool"));
        Assert.That(typeKeywords, Does.Contain("string"));
        Assert.That(typeKeywords, Does.Contain("Entity"));
    }

    [Test]
    public void Tokenize_Identifiers_IdentifiedCorrectly()
    {
        var source = "myVar another_name x123";
        var tokens = _lexer.Tokenize(source);

        var identifiers = tokens.Where(t => t.TokenType == TokenType.Identifier).Select(t => t.Value).ToList();

        Assert.That(identifiers, Does.Contain("myVar"));
        Assert.That(identifiers, Does.Contain("another_name"));
        Assert.That(identifiers, Does.Contain("x123"));
    }

    [Test]
    public void Tokenize_IntegerLiterals_ParsedCorrectly()
    {
        var source = "42 0 100";
        var tokens = _lexer.Tokenize(source);

        var numbers = tokens.Where(t => t.TokenType == TokenType.Number).Select(t => t.Value).ToList();

        Assert.That(numbers, Does.Contain("42"));
        Assert.That(numbers, Does.Contain("0"));
        Assert.That(numbers, Does.Contain("100"));
    }

    [Test]
    public void Tokenize_FloatLiterals_ParsedCorrectly()
    {
        var source = "3.14 0.5 1.0f";
        var tokens = _lexer.Tokenize(source);

        var numbers = tokens.Where(t => t.TokenType == TokenType.Number).Select(t => t.Value).ToList();

        Assert.That(numbers.Count, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void Tokenize_HexLiterals_ParsedCorrectly()
    {
        var source = "0xFF 0x1A";
        var tokens = _lexer.Tokenize(source);

        var numbers = tokens.Where(t => t.TokenType == TokenType.Number).Select(t => t.Value).ToList();

        Assert.That(numbers, Does.Contain("0xFF"));
        Assert.That(numbers, Does.Contain("0x1A"));
    }

    [Test]
    public void Tokenize_StringLiterals_ParsedCorrectly()
    {
        var source = "\"hello\" \"world\"";
        var tokens = _lexer.Tokenize(source);

        var strings = tokens.Where(t => t.TokenType == TokenType.String).Select(t => t.Value).ToList();

        Assert.That(strings, Does.Contain("hello"));
        Assert.That(strings, Does.Contain("world"));
    }

    [Test]
    public void Tokenize_StringEscapeSequences_ParsedCorrectly()
    {
        var source = "\"hello\\nworld\" \"tab\\there\"";
        var tokens = _lexer.Tokenize(source);

        var strings = tokens.Where(t => t.TokenType == TokenType.String).Select(t => t.Value).ToList();

        Assert.That(strings[0], Does.Contain("\n"));
        Assert.That(strings[1], Does.Contain("\t"));
    }

    [Test]
    public void Tokenize_BooleanLiterals_IdentifiedAsLiteral()
    {
        var source = "true false";
        var tokens = _lexer.Tokenize(source);

        var literals = tokens.Where(t => t.TokenType == TokenType.Literal).Select(t => t.Value).ToList();

        Assert.That(literals, Does.Contain("true"));
        Assert.That(literals, Does.Contain("false"));
    }

    [Test]
    public void Tokenize_Operators_IdentifiedCorrectly()
    {
        var source = "+ - * / = == != < > <= >= += -=";
        var tokens = _lexer.Tokenize(source);

        var operators = tokens.Where(t => t.TokenType == TokenType.Operator).Select(t => t.Value).ToList();

        Assert.That(operators, Does.Contain("+"));
        Assert.That(operators, Does.Contain("-"));
        Assert.That(operators, Does.Contain("*"));
        Assert.That(operators, Does.Contain("/"));
        Assert.That(operators, Does.Contain("="));
        Assert.That(operators, Does.Contain("=="));
        Assert.That(operators, Does.Contain("!="));
        Assert.That(operators, Does.Contain("<="));
        Assert.That(operators, Does.Contain(">="));
        Assert.That(operators, Does.Contain("+="));
        Assert.That(operators, Does.Contain("-="));
    }

    [Test]
    public void Tokenize_Delimiters_IdentifiedCorrectly()
    {
        var source = "( ) { } [ ] , ; . :";
        var tokens = _lexer.Tokenize(source);

        var delimiters = tokens.Where(t => t.TokenType == TokenType.Delimiter).Select(t => t.Value).ToList();

        Assert.That(delimiters, Does.Contain("("));
        Assert.That(delimiters, Does.Contain(")"));
        Assert.That(delimiters, Does.Contain("{"));
        Assert.That(delimiters, Does.Contain("}"));
        Assert.That(delimiters, Does.Contain("["));
        Assert.That(delimiters, Does.Contain("]"));
        Assert.That(delimiters, Does.Contain(","));
        Assert.That(delimiters, Does.Contain(";"));
        Assert.That(delimiters, Does.Contain("."));
    }

    [Test]
    public void Tokenize_MetaBlocks_IdentifiedCorrectly()
    {
        var source = "<% code_here %> <%= expr %>";
        var tokens = _lexer.Tokenize(source);

        Assert.That(tokens.Any(t => t.TokenType == TokenType.MetaBlockStart), Is.True);
        Assert.That(tokens.Any(t => t.TokenType == TokenType.MetaExpression), Is.True);
    }

    [Test]
    public void Tokenize_LineComments_Skipped()
    {
        var source = "x # this is a comment\ny // another comment\nz";
        var tokens = _lexer.Tokenize(source);

        var identifiers = tokens.Where(t => t.TokenType == TokenType.Identifier).Select(t => t.Value).ToList();

        Assert.That(identifiers, Does.Contain("x"));
        Assert.That(identifiers, Does.Contain("y"));
        Assert.That(identifiers, Does.Contain("z"));
        Assert.That(identifiers.Count, Is.EqualTo(3));
    }

    [Test]
    public void Tokenize_BlockComments_Skipped()
    {
        var source = "x /* block comment */ y";
        var tokens = _lexer.Tokenize(source);

        var identifiers = tokens.Where(t => t.TokenType == TokenType.Identifier).Select(t => t.Value).ToList();

        Assert.That(identifiers, Does.Contain("x"));
        Assert.That(identifiers, Does.Contain("y"));
        Assert.That(identifiers.Count, Is.EqualTo(2));
    }

    [Test]
    public void Tokenize_Attributes_IdentifiedCorrectly()
    {
        var source = "[Encrypted] [Honeypot(trigger = \"on_cheat\")]";
        var tokens = _lexer.Tokenize(source);

        var attrs = tokens.Where(t => t.TokenType == TokenType.Attribute).Select(t => t.Value).ToList();

        Assert.That(attrs.Count, Is.EqualTo(2));
        Assert.That(attrs[0], Does.Contain("Encrypted"));
        Assert.That(attrs[1], Does.Contain("Honeypot"));
    }

    [Test]
    public void Tokenize_ComponentDeclaration_TokenizedCorrectly()
    {
        var source = "component Position { x: f32; y: f32; }";
        var tokens = _lexer.Tokenize(source);

        Assert.That(tokens[0].TokenType, Is.EqualTo(TokenType.Keyword));
        Assert.That(tokens[0].Value, Is.EqualTo("component"));
        Assert.That(tokens[1].TokenType, Is.EqualTo(TokenType.Identifier));
        Assert.That(tokens[1].Value, Is.EqualTo("Position"));
    }

    [Test]
    public void Tokenize_LineColumnTracking_TrackedCorrectly()
    {
        var source = "x\ny\nz";
        var tokens = _lexer.Tokenize(source);

        var x = tokens.First(t => t.Value == "x");
        var y = tokens.First(t => t.Value == "y");
        var z = tokens.First(t => t.Value == "z");

        Assert.That(x.Line, Is.EqualTo(1));
        Assert.That(y.Line, Is.EqualTo(2));
        Assert.That(z.Line, Is.EqualTo(3));
    }
}
