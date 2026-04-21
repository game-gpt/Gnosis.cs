using Gnosis.Compiler;
using Gnosis.Compiler.AST;
using Gnosis.Compiler.Frontend;
using NUnit.Framework;

namespace Gnosis.Testing.Compiler;

[TestFixture]
public class GgScriptParserTests
{
    private GgScriptParser _parser = null!;
    private GgScriptLexer _lexer = null!;

    [SetUp]
    public void Setup()
    {
        _parser = new GgScriptParser();
        _lexer = new GgScriptLexer();
    }

    private AstNode ParseSource(string source)
    {
        var tokens = _lexer.Tokenize(source);
        return _parser.Parse(tokens);
    }

    [Test]
    public void Parse_EmptySource_ReturnsEmptyCompilationUnit()
    {
        var ast = ParseSource("");

        Assert.That(ast, Is.InstanceOf<CompilationUnit>());
        var unit = (CompilationUnit)ast;
        Assert.That(unit.Declarations.Count, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ComponentDecl_ParsedCorrectly()
    {
        var source = "component Position { x: f32; y: f32; }";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        Assert.That(unit.Declarations.Count, Is.EqualTo(1));
        Assert.That(unit.Declarations[0], Is.InstanceOf<ComponentDecl>());

        var comp = (ComponentDecl)unit.Declarations[0];
        Assert.That(comp.Name, Is.EqualTo("Position"));
        Assert.That(comp.Fields.Count, Is.EqualTo(2));
        Assert.That(comp.Fields[0].Name, Is.EqualTo("x"));
        Assert.That(comp.Fields[0].FieldType.Name, Is.EqualTo("f32"));
        Assert.That(comp.Fields[1].Name, Is.EqualTo("y"));
    }

    [Test]
    public void Parse_ComponentWithDefaultValues_ParsedCorrectly()
    {
        var source = "component Health { current: i32 = 100; max: i32 = 100; }";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        var comp = (ComponentDecl)unit.Declarations[0];
        Assert.That(comp.Fields[0].DefaultValue, Is.Not.Null);
    }

    [Test]
    public void Parse_ComponentWithAttributes_ParsedCorrectly()
    {
        var source = "[Encrypted] component PlayerData { gold: i32; }";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        var comp = (ComponentDecl)unit.Declarations[0];
        Assert.That(comp.Attributes.Count, Is.EqualTo(1));
        Assert.That(comp.Attributes[0].Name, Is.EqualTo("Encrypted"));
    }

    [Test]
    public void Parse_SystemDecl_ParsedCorrectly()
    {
        var source = @"
system MoveSystem {
    on_update(delta: f32) {
    }
}";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        Assert.That(unit.Declarations.Count, Is.EqualTo(1));
        Assert.That(unit.Declarations[0], Is.InstanceOf<SystemDecl>());

        var sys = (SystemDecl)unit.Declarations[0];
        Assert.That(sys.Name, Is.EqualTo("MoveSystem"));
        Assert.That(sys.LifecycleMethods.Count, Is.EqualTo(1));
        Assert.That(sys.LifecycleMethods[0].Name, Is.EqualTo("on_update"));
    }

    [Test]
    public void Parse_FunctionDecl_ParsedCorrectly()
    {
        var source = "micro add(a: i32, b: i32): i32 { return a + b; }";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        Assert.That(unit.Declarations[0], Is.InstanceOf<FunctionDecl>());

        var func = (FunctionDecl)unit.Declarations[0];
        Assert.That(func.Name, Is.EqualTo("add"));
        Assert.That(func.Parameters.Count, Is.EqualTo(2));
        Assert.That(func.ReturnType, Is.Not.Null);
        Assert.That(func.ReturnType!.Name, Is.EqualTo("i32"));
    }

    [Test]
    public void Parse_VariableDecl_ParsedCorrectly()
    {
        var source = "let x: i32 = 42; let mut y: f32 = 3.14;";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        Assert.That(unit.Declarations.Count, Is.EqualTo(2));

        var var1 = (VariableDecl)unit.Declarations[0];
        Assert.That(var1.Name, Is.EqualTo("x"));
        Assert.That(var1.IsMutable, Is.False);

        var var2 = (VariableDecl)unit.Declarations[1];
        Assert.That(var2.Name, Is.EqualTo("y"));
        Assert.That(var2.IsMutable, Is.True);
    }

    [Test]
    public void Parse_IfStatement_ParsedCorrectly()
    {
        var source = "micro test() { if x > 0 { return 1; } else { return 0; } }";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        var func = (FunctionDecl)unit.Declarations[0];
        var body = func.Body!;
        Assert.That(body.Statements[0], Is.InstanceOf<IfStatement>());

        var ifStmt = (IfStatement)body.Statements[0];
        Assert.That(ifStmt.Condition, Is.InstanceOf<BinaryExpr>());
        Assert.That(ifStmt.ThenBlock, Is.Not.Null);
        Assert.That(ifStmt.ElseBlock, Is.Not.Null);
    }

    [Test]
    public void Parse_LoopStatement_ParsedCorrectly()
    {
        var source = "micro test() { loop item in items { } }";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        var func = (FunctionDecl)unit.Declarations[0];
        var body = func.Body!;
        Assert.That(body.Statements[0], Is.InstanceOf<LoopStmt>());

        var loopStmt = (LoopStmt)body.Statements[0];
        Assert.That(loopStmt.IteratorName, Is.EqualTo("item"));
    }

    [Test]
    public void Parse_WhileStatement_ParsedCorrectly()
    {
        var source = "micro test() { while x > 0 { } }";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        var func = (FunctionDecl)unit.Declarations[0];
        var body = func.Body!;
        Assert.That(body.Statements[0], Is.InstanceOf<WhileStmt>());
    }

    [Test]
    public void Parse_QueryExpr_ParsedCorrectly()
    {
        var source = @"
system DamageSystem {
    query_all = Query.all(Health, Damage);
    on_update(delta: f32) {
    }
}";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        var sys = (SystemDecl)unit.Declarations[0];
        Assert.That(sys.Queries.Count, Is.EqualTo(1));
        Assert.That(sys.Queries[0].Kind, Is.EqualTo(QueryKind.All));
        Assert.That(sys.Queries[0].ComponentTypes.Count, Is.EqualTo(2));
    }

    [Test]
    public void Parse_SceneDecl_ParsedCorrectly()
    {
        var source = @"
scene GameMain {
    on_load() {
    }
    on_update(delta: f32) {
    }
}";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        Assert.That(unit.Declarations[0], Is.InstanceOf<SceneDecl>());

        var scene = (SceneDecl)unit.Declarations[0];
        Assert.That(scene.Name, Is.EqualTo("GameMain"));
        Assert.That(scene.LifecycleMethods.Count, Is.EqualTo(2));
    }

    [Test]
    public void Parse_ImportDecl_ParsedCorrectly()
    {
        var source = "import Gnosis.Core; import Gnosis.ECS as ECS;";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        Assert.That(unit.Declarations.Count, Is.EqualTo(2));

        var import1 = (ImportDecl)unit.Declarations[0];
        Assert.That(import1.ModulePath, Is.EqualTo("Gnosis.Core"));
        Assert.That(import1.Alias, Is.Null);

        var import2 = (ImportDecl)unit.Declarations[1];
        Assert.That(import2.ModulePath, Is.EqualTo("Gnosis.ECS"));
        Assert.That(import2.Alias, Is.EqualTo("ECS"));
    }

    [Test]
    public void Parse_BinaryExpression_ParsedWithCorrectPrecedence()
    {
        var source = "micro test() { let x = 1 + 2 * 3; }";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        var func = (FunctionDecl)unit.Declarations[0];
        var body = func.Body!;
        var varDecl = (VariableDecl)body.Statements[0];
        var init = (BinaryExpr)varDecl.Initializer!;

        Assert.That(init.Operator, Is.EqualTo("+"));
        Assert.That(init.Left, Is.InstanceOf<LiteralExpr>());
        Assert.That(init.Right, Is.InstanceOf<BinaryExpr>());
    }

    [Test]
    public void Parse_MemberAccess_ParsedCorrectly()
    {
        var source = "micro test() { entity.get(); }";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        var func = (FunctionDecl)unit.Declarations[0];
        var body = func.Body!;
        var exprStmt = (TermExpressionStatement)body.Statements[0];
        var call = (TermCallExpression)exprStmt.Expression;
        var memberAccess = (MemberAccessExpr)call.Callee;

        Assert.That(memberAccess.MemberName, Is.EqualTo("get"));
    }

    [Test]
    public void Parse_PluginDecl_ParsedCorrectly()
    {
        var source = @"
plugin WeChatChannel {
    requires_arch = [""WASM""]
    provides_macros = [""WECHAT""]
    provides_capabilities = [""WeChatLogin""]
}";
        var ast = ParseSource(source);

        var unit = (CompilationUnit)ast;
        Assert.That(unit.Declarations[0], Is.InstanceOf<PluginDecl>());

        var plugin = (PluginDecl)unit.Declarations[0];
        Assert.That(plugin.Name, Is.EqualTo("WeChatChannel"));
        Assert.That(plugin.RequiresArch.Count, Is.EqualTo(1));
        Assert.That(plugin.RequiresArch[0], Is.EqualTo("WASM"));
    }
}
