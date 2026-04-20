using Gnosis.Compiler.Backend;
using Gnosis.Compiler.Diagnostics;
using Gnosis.Compiler.Frontend;
using Gnosis.Compiler.Formats;
using Gnosis.Compiler.ValueObjects;
using Gnosis.Compiler.ValueObjects.AST;
using NUnit.Framework;

namespace Gnosis.Tests.Compiler;

[TestFixture]
public class BytecodeGeneratorTests
{
    private GgScriptLexer _lexer = null!;
    private GgScriptParser _parser = null!;

    [SetUp]
    public void Setup()
    {
        _lexer = new GgScriptLexer();
        _parser = new GgScriptParser();
    }

    private CompilationUnit ParseSource(string source)
    {
        var tokens = _lexer.Tokenize(source);
        var ast = _parser.Parse(tokens);
        return (CompilationUnit)ast;
    }

    [Test]
    public void Generate_ComponentDecl_ProducesBytecode()
    {
        var source = "component Position { x: f32; y: f32; }";
        var ast = ParseSource(source);

        var diagnostics = new DiagnosticSink();
        var gen = new BytecodeGenerator(diagnostics);
        var module = gen.Generate(ast, ArchTarget.X64, false);

        Assert.That(module.Instructions.Length, Is.GreaterThan(0));
        Assert.That(module.ModuleName, Is.EqualTo("main"));
        Assert.That(diagnostics.HasErrors, Is.False);
    }

    [Test]
    public void Generate_FunctionDecl_ProducesBytecode()
    {
        var source = "micro add(a: i32, b: i32): i32 { return a + b; }";
        var ast = ParseSource(source);

        var diagnostics = new DiagnosticSink();
        var gen = new BytecodeGenerator(diagnostics);
        var module = gen.Generate(ast, ArchTarget.X64, false);

        Assert.That(module.Instructions.Length, Is.GreaterThan(0));
        Assert.That(diagnostics.HasErrors, Is.False);
    }

    [Test]
    public void Generate_SystemDecl_ProducesBytecode()
    {
        var source = @"
system MoveSystem {
    on_update(delta: f32) {
    }
}";
        var ast = ParseSource(source);

        var diagnostics = new DiagnosticSink();
        var gen = new BytecodeGenerator(diagnostics);
        var module = gen.Generate(ast, ArchTarget.X64, false);

        Assert.That(module.Instructions.Length, Is.GreaterThan(0));
        Assert.That(diagnostics.HasErrors, Is.False);
    }

    [Test]
    public void Generate_FullCompilation_ProducesBytecodeAndVmSource()
    {
        var source = "micro main() { let x: i32 = 42; }";
        var ast = ParseSource(source);

        var diagnostics = new DiagnosticSink();
        var gen = new BytecodeGenerator(diagnostics);
        var result = gen.GenerateFull(ast, ArchTarget.X64, false);

        Assert.That(result.Bytecode.Length, Is.GreaterThan(0));
        Assert.That(result.VmSourceCode, Does.Contain("vm_run"));
        Assert.That(result.VmSourceCode, Does.Contain("switch"));
    }

    [Test]
    public void Generate_VmSource_ContainsCCode()
    {
        var source = "micro main() { return 0; }";
        var ast = ParseSource(source);

        var diagnostics = new DiagnosticSink();
        var gen = new BytecodeGenerator(diagnostics);
        var result = gen.GenerateFull(ast, ArchTarget.X64, false);

        Assert.That(result.VmSourceCode, Does.Contain("#include"));
        Assert.That(result.VmSourceCode, Does.Contain("VMState"));
        Assert.That(result.VmSourceCode, Does.Contain("opcode"));
    }
}

[TestFixture]
public class GonParserTests
{
    private GonParser _parser = null!;

    [SetUp]
    public void Setup()
    {
        _parser = new GonParser();
    }

    [Test]
    public void Parse_IntegerValue_ParsedCorrectly()
    {
        var result = _parser.Parse("42");
        Assert.That(result.Type, Is.EqualTo(GonValueType.Integer));
        Assert.That(result.GetInteger(), Is.EqualTo(42));
    }

    [Test]
    public void Parse_FloatValue_ParsedCorrectly()
    {
        var result = _parser.Parse("3.14");
        Assert.That(result.Type, Is.EqualTo(GonValueType.Double));
    }

    [Test]
    public void Parse_FloatSuffix_ParsedCorrectly()
    {
        var result = _parser.Parse("3.14f");
        Assert.That(result.Type, Is.EqualTo(GonValueType.Float));
    }

    [Test]
    public void Parse_BooleanValues_ParsedCorrectly()
    {
        var trueResult = _parser.Parse("true");
        Assert.That(trueResult.Type, Is.EqualTo(GonValueType.Boolean));
        Assert.That(trueResult.GetBoolean(), Is.True);

        var falseResult = _parser.Parse("false");
        Assert.That(falseResult.Type, Is.EqualTo(GonValueType.Boolean));
        Assert.That(falseResult.GetBoolean(), Is.False);
    }

    [Test]
    public void Parse_NullValue_ParsedCorrectly()
    {
        var result = _parser.Parse("null");
        Assert.That(result.Type, Is.EqualTo(GonValueType.Null));
    }

    [Test]
    public void Parse_StringValue_ParsedCorrectly()
    {
        var result = _parser.Parse("\"hello\"");
        Assert.That(result.Type, Is.EqualTo(GonValueType.String));
        Assert.That(result.GetString(), Is.EqualTo("hello"));
    }

    [Test]
    public void Parse_ObjectWithClassName_ParsedCorrectly()
    {
        var result = _parser.Parse("Position { x: 100.0, y: 200.0 }");
        Assert.That(result.Type, Is.EqualTo(GonValueType.Object));
        Assert.That(result.TypeName, Is.EqualTo("Position"));
        Assert.That(result.Fields, Is.Not.Null);
        Assert.That(result.Fields!.Count, Is.EqualTo(2));
    }

    [Test]
    public void Parse_ObjectWithoutClassName_ParsedCorrectly()
    {
        var result = _parser.Parse("{ name: \"test\", value: 42 }");
        Assert.That(result.Type, Is.EqualTo(GonValueType.Object));
        Assert.That(result.TypeName, Is.Null);
        Assert.That(result.Fields, Is.Not.Null);
        Assert.That(result.Fields!.Count, Is.EqualTo(2));
    }

    [Test]
    public void Parse_ObjectWithVariant_ParsedCorrectly()
    {
        var result = _parser.Parse("Item Weapon { damage: 10 }");
        Assert.That(result.Type, Is.EqualTo(GonValueType.Object));
        Assert.That(result.TypeName, Is.EqualTo("Item"));
        Assert.That(result.VariantName, Is.EqualTo("Weapon"));
    }

    [Test]
    public void Parse_Array_ParsedCorrectly()
    {
        var result = _parser.Parse("[1, 2, 3]");
        Assert.That(result.Type, Is.EqualTo(GonValueType.Array));
        Assert.That(result.Elements, Is.Not.Null);
        Assert.That(result.Elements!.Count, Is.EqualTo(3));
    }

    [Test]
    public void Parse_NestedObject_ParsedCorrectly()
    {
        var result = _parser.Parse("Player { position: Position { x: 1.0, y: 2.0 } }");
        Assert.That(result.Type, Is.EqualTo(GonValueType.Object));
        Assert.That(result.Fields!["position"].Type, Is.EqualTo(GonValueType.Object));
    }

    [Test]
    public void Parse_UnsignedInteger_ParsedCorrectly()
    {
        var result = _parser.Parse("42u");
        Assert.That(result.Type, Is.EqualTo(GonValueType.UnsignedInteger));
    }

    [Test]
    public void Parse_FieldAccess_WorksCorrectly()
    {
        var result = _parser.Parse("Player { name: \"Alice\", level: 10 }");
        var name = result.GetField("name");
        var level = result.GetField("level");

        Assert.That(name, Is.Not.Null);
        Assert.That(name!.GetString(), Is.EqualTo("Alice"));
        Assert.That(level, Is.Not.Null);
        Assert.That(level!.GetInteger(), Is.EqualTo(10));
    }
}

[TestFixture]
public class GgWidgetCompilerTests
{
    private GgWidgetCompiler _compiler = null!;

    [SetUp]
    public void Setup()
    {
        _compiler = new GgWidgetCompiler();
    }

    [Test]
    public void Compile_ScriptSetup_ParsesProperties()
    {
        var source = @"
<script setup>
let count: number = 0;
let name: string = ""hello"";
</script>

<template>
  <div>{{ count }}</div>
</template>
";
        var result = _compiler.Compile(source, "Counter.ggw");

        Assert.That(result.Name, Is.EqualTo("Counter"));
        Assert.That(result.Properties.Count, Is.EqualTo(2));
        Assert.That(result.Properties[0].Name, Is.EqualTo("count"));
        Assert.That(result.Properties[1].Name, Is.EqualTo("name"));
    }

    [Test]
    public void Compile_TemplateBlock_ParsesRenderMethod()
    {
        var source = @"
<template>
  <div>Hello</div>
</template>
";
        var result = _compiler.Compile(source, "HelloWidget.ggw");

        Assert.That(result.RenderMethod, Is.Not.Null);
        Assert.That(result.RenderMethod!.Name, Is.EqualTo("render"));
    }

    [Test]
    public void Compile_StyleBlock_ParsesClasses()
    {
        var source = @"
<template>
  <div class=""container"">Hello</div>
</template>

<style>
.container { padding: 10px; }
</style>
";
        var result = _compiler.Compile(source, "StyledWidget.ggw");

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void Compile_EmptySource_ReturnsWidgetDecl()
    {
        var result = _compiler.Compile("", "EmptyWidget.ggw");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("EmptyWidget"));
    }
}

[TestFixture]
public class SemanticAnalyzerTests
{
    private GgScriptLexer _lexer = null!;
    private GgScriptParser _parser = null!;
    private SemanticAnalyzer _analyzer = null!;
    private DiagnosticSink _diagnostics = null!;

    [SetUp]
    public void Setup()
    {
        _lexer = new GgScriptLexer();
        _parser = new GgScriptParser();
        _diagnostics = new DiagnosticSink();
        _analyzer = new SemanticAnalyzer(_diagnostics);
    }

    [Test]
    public void Analyze_DuplicateComponent_ReportsError()
    {
        var source = @"
component Position { x: f32; }
component Position { y: f32; }
";
        var ast = ParseSource(source);
        _analyzer.Analyze(ast);

        Assert.That(_diagnostics.HasErrors, Is.True);
    }

    [Test]
    public void Analyze_ValidComponent_NoErrors()
    {
        var source = "component Position { x: f32; y: f32; }";
        var ast = ParseSource(source);
        _analyzer.Analyze(ast);

        Assert.That(_diagnostics.HasErrors, Is.False);
    }

    [Test]
    public void Analyze_QueryUndefinedComponent_ReportsError()
    {
        var source = @"
system TestSystem {
    query = Query.all(UndefinedComponent);
    on_update(delta: f32) {
    }
}
";
        var ast = ParseSource(source);
        _analyzer.Analyze(ast);

        Assert.That(_diagnostics.HasErrors, Is.True);
    }

    [Test]
    public void Analyze_HoneypotWithoutEncrypted_ReportsWarning()
    {
        var source = @"
component PlayerData {
    [Honeypot(trigger = ""on_cheat"")]
    _gold_fake: i32;
}
";
        var ast = ParseSource(source);
        _analyzer.Analyze(ast);

        Assert.That(_diagnostics.GetWarnings().Any(), Is.True);
    }

    [Test]
    public void Analyze_InvalidLifecycleMethod_ReportsWarning()
    {
        var source = @"
system TestSystem {
    on_invalid_method() {
    }
}
";
        var ast = ParseSource(source);
        _analyzer.Analyze(ast);

        Assert.That(_diagnostics.GetWarnings().Any(), Is.True);
    }

    private CompilationUnit ParseSource(string source)
    {
        var tokens = _lexer.Tokenize(source);
        var ast = _parser.Parse(tokens);
        return (CompilationUnit)ast;
    }
}
