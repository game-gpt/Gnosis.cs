using Gnosis.Toolchain.ScriptCompiler;
using Gnosis.Toolchain.ScriptCompiler.Diagnostics;
using Gnosis.Toolchain.ScriptCompiler.Parser;
using NUnit.Framework;

namespace Gnosis.Tests.Compiler;

/// <summary>
/// GON 配置语言解析器测试
/// </summary>
[TestFixture]
public class GonParserTests
{
    private GonParser _parser = null!;
    private DiagnosticSink _diagnostics = null!;

    [SetUp]
    public void SetUp()
    {
        _diagnostics = new DiagnosticSink();
        _parser = new GonParser(_diagnostics);
    }

    #region 基本类型解析

    [Test]
    public void Parse_Null_ReturnsNullValue()
    {
        var result = _parser.Parse("null");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Null));
    }

    [Test]
    public void Parse_True_ReturnsBooleanTrue()
    {
        var result = _parser.Parse("true");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Boolean));
        Assert.That(result.GetBoolean(), Is.True);
    }

    [Test]
    public void Parse_False_ReturnsBooleanFalse()
    {
        var result = _parser.Parse("false");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Boolean));
        Assert.That(result.GetBoolean(), Is.False);
    }

    [Test]
    public void Parse_Integer_ReturnsIntegerValue()
    {
        var result = _parser.Parse("42");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Integer));
        Assert.That(result.GetInteger(), Is.EqualTo(42));
    }

    [Test]
    public void Parse_NegativeInteger_ReturnsNegativeValue()
    {
        var result = _parser.Parse("-100");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Integer));
        Assert.That(result.GetInteger(), Is.EqualTo(-100));
    }

    [Test]
    public void Parse_Float_ReturnsFloatValue()
    {
        var result = _parser.Parse("3.14f");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Float));
        Assert.That(result.GetFloat(), Is.EqualTo(3.14f).Within(0.001f));
    }

    [Test]
    public void Parse_Double_ReturnsDoubleValue()
    {
        var result = _parser.Parse("2.71828");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Double));
    }

    [Test]
    public void Parse_String_ReturnsStringValue()
    {
        var result = _parser.Parse("\"hello world\"");

        Assert.That(result.Type, Is.EqualTo(GonValueType.String));
        Assert.That(result.GetString(), Is.EqualTo("hello world"));
    }

    [Test]
    public void Parse_StringWithEscapes_ReturnsUnescapedString()
    {
        var result = _parser.Parse("\"line1\\nline2\\ttab\"");

        Assert.That(result.GetString(), Is.EqualTo("line1\nline2\ttab"));
    }

    #endregion

    #region 对象解析

    [Test]
    public void Parse_EmptyObject_ReturnsEmptyObject()
    {
        var result = _parser.Parse("{}");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Object));
        Assert.That(result.Fields, Is.Not.Null);
        Assert.That(result.Fields!.Count, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ObjectWithSingleField_ReturnsObjectWithField()
    {
        var result = _parser.Parse("{ name: \"test\" }");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Object));
        Assert.That(result.Fields!.Count, Is.EqualTo(1));
        Assert.That(result.GetField("name")!.GetString(), Is.EqualTo("test"));
    }

    [Test]
    public void Parse_ObjectWithMultipleFields_ReturnsObjectWithFields()
    {
        var result = _parser.Parse("{ x: 10, y: 20, name: \"point\" }");

        Assert.That(result.Fields!.Count, Is.EqualTo(3));
        Assert.That(result.GetField("x")!.GetInteger(), Is.EqualTo(10));
        Assert.That(result.GetField("y")!.GetInteger(), Is.EqualTo(20));
        Assert.That(result.GetField("name")!.GetString(), Is.EqualTo("point"));
    }

    [Test]
    public void Parse_ObjectWithQuotedFieldName_ReturnsObjectWithField()
    {
        var result = _parser.Parse("{ \"field-name\": 42 }");

        Assert.That(result.GetField("field-name")!.GetInteger(), Is.EqualTo(42));
    }

    [Test]
    public void Parse_ObjectWithTrailingComma_AllowsTrailingComma()
    {
        var result = _parser.Parse("{ a: 1, b: 2, }");

        Assert.That(result.Fields!.Count, Is.EqualTo(2));
    }

    [Test]
    public void Parse_TypedObject_ReturnsTypedObject()
    {
        var result = _parser.Parse("Player { name: \"hero\", level: 5 }");

        Assert.That(result.TypeName, Is.EqualTo("Player"));
        Assert.That(result.Fields!.Count, Is.EqualTo(2));
    }

    [Test]
    public void Parse_VariantObject_ReturnsVariantObject()
    {
        var result = _parser.Parse("Color RGB { r: 255, g: 128, b: 0 }");

        Assert.That(result.TypeName, Is.EqualTo("Color"));
        Assert.That(result.VariantName, Is.EqualTo("RGB"));
    }

    [Test]
    public void Parse_NestedObject_ReturnsNestedObject()
    {
        var result = _parser.Parse("{ outer: { inner: 42 } }");

        var outer = result.GetField("outer")!;
        Assert.That(outer.Type, Is.EqualTo(GonValueType.Object));
        Assert.That(outer.GetField("inner")!.GetInteger(), Is.EqualTo(42));
    }

    #endregion

    #region 数组解析

    [Test]
    public void Parse_EmptyArray_ReturnsEmptyArray()
    {
        var result = _parser.Parse("[]");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Array));
        Assert.That(result.Elements, Is.Not.Null);
        Assert.That(result.Elements!.Count, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ArrayWithElements_ReturnsArray()
    {
        var result = _parser.Parse("[1, 2, 3]");

        Assert.That(result.Elements!.Count, Is.EqualTo(3));
        Assert.That(result.Elements[0].GetInteger(), Is.EqualTo(1));
        Assert.That(result.Elements[1].GetInteger(), Is.EqualTo(2));
        Assert.That(result.Elements[2].GetInteger(), Is.EqualTo(3));
    }

    [Test]
    public void Parse_ArrayWithMixedTypes_ReturnsArray()
    {
        var result = _parser.Parse("[1, \"hello\", true, null]");

        Assert.That(result.Elements!.Count, Is.EqualTo(4));
        Assert.That(result.Elements[0].Type, Is.EqualTo(GonValueType.Integer));
        Assert.That(result.Elements[1].Type, Is.EqualTo(GonValueType.String));
        Assert.That(result.Elements[2].Type, Is.EqualTo(GonValueType.Boolean));
        Assert.That(result.Elements[3].Type, Is.EqualTo(GonValueType.Null));
    }

    [Test]
    public void Parse_ArrayWithTrailingComma_AllowsTrailingComma()
    {
        var result = _parser.Parse("[1, 2, 3,]");

        Assert.That(result.Elements!.Count, Is.EqualTo(3));
    }

    [Test]
    public void Parse_NestedArray_ReturnsNestedArray()
    {
        var result = _parser.Parse("[[1, 2], [3, 4]]");

        Assert.That(result.Elements!.Count, Is.EqualTo(2));
        Assert.That(result.Elements[0].Elements!.Count, Is.EqualTo(2));
        Assert.That(result.Elements[1].Elements!.Count, Is.EqualTo(2));
    }

    #endregion

    #region 注释支持

    [Test]
    public void Parse_LineComment_IgnoresComment()
    {
        var result = _parser.Parse("# 这是注释\n42");

        Assert.That(result.GetInteger(), Is.EqualTo(42));
    }

    [Test]
    public void Parse_BlockComment_IgnoresComment()
    {
        var result = _parser.Parse("<# 块注释 #> 42");

        Assert.That(result.GetInteger(), Is.EqualTo(42));
    }

    [Test]
    public void Parse_NestedBlockComment_HandlesNesting()
    {
        var result = _parser.Parse("<# 外层 <# 内层 #> #> 42");

        Assert.That(result.GetInteger(), Is.EqualTo(42));
    }

    #endregion

    #region 复杂场景

    [Test]
    public void Parse_ComplexConfig_ReturnsCorrectStructure()
    {
        var source = @"
            GameConfig {
                title: """"Gnosis Engine"""",
                version: """"1.0.0"""",
                fullscreen: true,
                resolution: { width: 1920, height: 1080 },
                tags: [""""rpg"""", """"action"""", """"multiplayer""""]
            }
        ";

        var result = _parser.Parse(source);

        Assert.That(result.TypeName, Is.EqualTo("GameConfig"));
        Assert.That(result.GetField("title")!.GetString(), Is.EqualTo("Gnosis Engine"));
        Assert.That(result.GetField("fullscreen")!.GetBoolean(), Is.True);

        var resolution = result.GetField("resolution")!;
        Assert.That(resolution.GetField("width")!.GetInteger(), Is.EqualTo(1920));
        Assert.That(resolution.GetField("height")!.GetInteger(), Is.EqualTo(1080));

        var tags = result.GetField("tags")!;
        Assert.That(tags.Elements!.Count, Is.EqualTo(3));
    }

    [Test]
    public void Parse_WhitespaceOnly_ReturnsNull()
    {
        var result = _parser.Parse("   \n\t   ");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Null));
    }

    [Test]
    public void Parse_EmptyString_ReturnsNull()
    {
        var result = _parser.Parse("");

        Assert.That(result.Type, Is.EqualTo(GonValueType.Null));
    }

    #endregion

    #region 错误处理

    [Test]
    public void Parse_InvalidCharacter_GeneratesError()
    {
        _parser.Parse("@");

        Assert.That(_diagnostics.GetErrors().Count, Is.GreaterThan(0));
    }

    [Test]
    public void Parse_UnclosedObject_GeneratesError()
    {
        _parser.Parse("{ a: 1");

        Assert.That(_diagnostics.GetErrors().Any(e => e.Message.Contains("}")) ||
                    _diagnostics.GetErrors().Any(e => e.Message.Contains("期望")),
                    Is.True);
    }

    [Test]
    public void Parse_UnclosedArray_GeneratesError()
    {
        _parser.Parse("[1, 2");

        Assert.That(_diagnostics.GetErrors().Any(e => e.Message.Contains("]")),
                    Is.True);
    }

    [Test]
    public void Parse_MissingColon_GeneratesError()
    {
        _parser.Parse("{ a 1 }");

        Assert.That(_diagnostics.GetErrors().Any(e => e.Message.Contains(":")),
                    Is.True);
    }

    [Test]
    public void Parse_InvalidIdentifier_GeneratesError()
    {
        _parser.Parse("unknown_identifier");

        Assert.That(_diagnostics.GetErrors().Count, Is.GreaterThan(0));
    }

    #endregion

    #region ToString 验证

    [Test]
    public void ToString_Null_ReturnsNull()
    {
        var result = GonValue.Null();
        Assert.That(result.ToString(), Is.EqualTo("null"));
    }

    [Test]
    public void ToString_Boolean_ReturnsCorrectString()
    {
        Assert.That(GonValue.Boolean(true).ToString(), Is.EqualTo("true"));
        Assert.That(GonValue.Boolean(false).ToString(), Is.EqualTo("false"));
    }

    [Test]
    public void ToString_Integer_ReturnsCorrectString()
    {
        Assert.That(GonValue.Integer(42).ToString(), Is.EqualTo("42"));
    }

    [Test]
    public void ToString_String_ReturnsQuotedString()
    {
        Assert.That(GonValue.String("hello").ToString(), Is.EqualTo("\"hello\""));
    }

    [Test]
    public void ToString_Object_ReturnsFormattedObject()
    {
        var obj = GonValue.Object("Player", null,
            new Dictionary<string, GonValue>
            {
                ["name"] = GonValue.String("hero"),
                ["level"] = GonValue.Integer(10)
            });

        var str = obj.ToString();
        Assert.That(str, Does.Contain("Player"));
        Assert.That(str, Does.Contain("name: \"hero\""));
        Assert.That(str, Does.Contain("level: 10"));
    }

    [Test]
    public void ToString_Array_ReturnsFormattedArray()
    {
        var arr = GonValue.Array([GonValue.Integer(1), GonValue.Integer(2)]);

        Assert.That(arr.ToString(), Is.EqualTo("[ 1, 2 ]"));
    }

    #endregion

    #region GetFieldAs 验证

    [Test]
    public void GetFieldAs_Integer_ReturnsTypedValue()
    {
        var obj = GonValue.Object(null, null,
            new Dictionary<string, GonValue>
            {
                ["count"] = GonValue.Integer(42)
            });

        var count = obj.GetFieldAs<int>("count");
        Assert.That(count, Is.EqualTo(42));
    }

    [Test]
    public void GetFieldAs_Float_ReturnsTypedValue()
    {
        var obj = GonValue.Object(null, null,
            new Dictionary<string, GonValue>
            {
                ["ratio"] = GonValue.Float(3.14f)
            });

        var ratio = obj.GetFieldAs<float>("ratio");
        Assert.That(ratio, Is.EqualTo(3.14f).Within(0.001f));
    }

    [Test]
    public void GetFieldAs_MissingField_ReturnsNull()
    {
        var obj = GonValue.Object(null, null, new Dictionary<string, GonValue>());

        var missing = obj.GetFieldAs<int>("missing");
        Assert.That(missing, Is.Null);
    }

    #endregion
}
