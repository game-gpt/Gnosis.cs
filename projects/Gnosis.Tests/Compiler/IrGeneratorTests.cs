using Gnosis.Rendering.Backends;
using Gnosis.Rendering.Backends.ShaderIR;
using Gnosis.Toolchain.ScriptCompiler.AST;
using Gnosis.Toolchain.ScriptCompiler.Diagnostics;
using Gnosis.Toolchain.ScriptCompiler.Backend;
using NUnit.Framework;

namespace Gnosis.Compiler;

[TestFixture]
public class IrGeneratorTests
{
    private IrGenerator _generator = null!;
    private DiagnosticSink _diagnostics = null!;

    [SetUp]
    public void Setup()
    {
        _diagnostics = new DiagnosticSink();
        _generator = new IrGenerator(_diagnostics);
    }

    #region Helper Methods

    private static CompilationUnit CreateCompilationUnit(params AstNode[] declarations)
    {
        return new CompilationUnit(declarations.ToList(), "test.ggs");
    }

    private static TypeAnnotation F32Type() => new("f32", []);

    private static TypeAnnotation I32Type() => new("i32", []);

    private static TypeAnnotation Vec3Type() => new("vec3", [F32Type()]);

    private static TypeAnnotation Vec4Type() => new("vec4", [F32Type()]);

    private static TypeAnnotation VoidType() => new("void", []);

    private static TypeAnnotation BoolType() => new("bool", []);

    private static AttributeDecl FragmentAttr() => new("Fragment", []);

    #endregion

    #region LiteralExpr Tests

    [Test]
    public void VisitLiteralExpr_IntLiteral_ContainsValue()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                F32Type(),
                new BlockStmt([
                    new ReturnStatement(new LiteralExpr(LiteralType.Number, 42))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        Assert.That(ir.Functions, Has.Count.EqualTo(1));
        var load = ir.Functions[0].Instructions.OfType<LoadInstruction>().FirstOrDefault();
        Assert.That(load, Is.Not.Null);
        Assert.That(load.Value, Is.EqualTo((uint)42));
        Assert.That(load.ResultType, Is.InstanceOf<ShaderIrType.IntType>());
    }

    [Test]
    public void VisitLiteralExpr_FloatLiteral_ContainsValue()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                F32Type(),
                new BlockStmt([
                    new ReturnStatement(new LiteralExpr(LiteralType.Number, 3.14f))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var load = ir.Functions[0].Instructions.OfType<LoadInstruction>().FirstOrDefault();
        Assert.That(load, Is.Not.Null);
        Assert.That(load.Value, Is.Not.Null);
        Assert.That(load.ResultType, Is.InstanceOf<ShaderIrType.FloatType>());
    }

    [Test]
    public void VisitLiteralExpr_BoolLiteral_ContainsValue()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                F32Type(),
                new BlockStmt([
                    new ReturnStatement(new LiteralExpr(LiteralType.Boolean, true))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var load = ir.Functions[0].Instructions.OfType<LoadInstruction>().FirstOrDefault();
        Assert.That(load, Is.Not.Null);
        Assert.That(load.Value, Is.EqualTo((uint)1));
        Assert.That(load.ResultType, Is.InstanceOf<ShaderIrType.BoolType>());
    }

    #endregion

    #region IdentifierExpr Tests

    [Test]
    public void VisitIdentifierExpr_ResolvesActualType()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                I32Type(),
                new BlockStmt([
                    new VariableDecl("count", I32Type(), new LiteralExpr(LiteralType.Number, 0), false),
                    new ReturnStatement(new IdentifierNode("count"))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var loads = ir.Functions[0].Instructions.OfType<LoadInstruction>().ToList();
        var identifierLoad = loads.FirstOrDefault(l => l.ResultType is ShaderIrType.IntType);
        Assert.That(identifierLoad, Is.Not.Null, "标识符加载应使用 IntType 而非硬编码 FloatType");
    }

    #endregion

    #region CompareInstruction Tests

    [Test]
    public void VisitBinaryExpr_Comparison_GeneratesCompareInstruction()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                F32Type(),
                new BlockStmt([
                    new VariableDecl("x", F32Type(), new LiteralExpr(LiteralType.Number, 1.0f), false),
                    new VariableDecl("y", F32Type(), new LiteralExpr(LiteralType.Number, 2.0f), false),
                    new ReturnStatement(new BinaryExpr(new IdentifierNode("x"), "<", new IdentifierNode("y")))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var compare = ir.Functions[0].Instructions.OfType<CompareInstruction>().FirstOrDefault();
        Assert.That(compare, Is.Not.Null, "比较运算应生成 CompareInstruction");
        Assert.That(compare.OpCode, Is.EqualTo(ShaderIrOpCode.LessThan));
        Assert.That(compare.ResultType, Is.InstanceOf<ShaderIrType.BoolType>());
    }

    [Test]
    public void VisitBinaryExpr_LogicalAnd_GeneratesLogicalInstruction()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                F32Type(),
                new BlockStmt([
                    new VariableDecl("a", BoolType(), new LiteralExpr(LiteralType.Boolean, true), false),
                    new VariableDecl("b", BoolType(), new LiteralExpr(LiteralType.Boolean, false), false),
                    new ReturnStatement(new BinaryExpr(new IdentifierNode("a"), "&&", new IdentifierNode("b")))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var logical = ir.Functions[0].Instructions.OfType<LogicalInstruction>().FirstOrDefault();
        Assert.That(logical, Is.Not.Null, "逻辑运算应生成 LogicalInstruction");
        Assert.That(logical.OpCode, Is.EqualTo(ShaderIrOpCode.LogicalAnd));
    }

    #endregion

    #region StructDecl Tests

    [Test]
    public void VisitStructDecl_GeneratesShaderStructIr()
    {
        var ast = CreateCompilationUnit(
            new StructDecl("VertexOutput",
                new List<FieldDecl>
                {
                    new("position", Vec4Type(), null, []),
                    new("uv", new TypeAnnotation("vec2", [F32Type()]), null, [])
                },
                []));

        var ir = _generator.Generate(ast);

        Assert.That(ir.Structs, Has.Count.EqualTo(1));
        Assert.That(ir.Structs[0].Name, Is.EqualTo("VertexOutput"));
        Assert.That(ir.Structs[0].Fields, Has.Count.EqualTo(2));
        Assert.That(ir.Structs[0].Fields[0].Name, Is.EqualTo("position"));
        Assert.That(ir.Structs[0].Fields[0].Offset, Is.EqualTo(0));
        Assert.That(ir.Structs[0].Fields[1].Name, Is.EqualTo("uv"));
        Assert.That(ir.Structs[0].Fields[1].Offset, Is.EqualTo(16));
    }

    #endregion

    #region ForStmt Tests

    [Test]
    public void VisitForStmt_GeneratesLoopMergeAndBranches()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                VoidType(),
                new BlockStmt([
                    new ForStmt(
                        new VariableDecl("i", I32Type(), new LiteralExpr(LiteralType.Number, 0), false),
                        new BinaryExpr(new IdentifierNode("i"), "<", new LiteralExpr(LiteralType.Number, 10)),
                        new AssignmentExpr(new IdentifierNode("i"), "=", new BinaryExpr(new IdentifierNode("i"), "+", new LiteralExpr(LiteralType.Number, 1))),
                        new BlockStmt([]))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var loopMerge = ir.Functions[0].Instructions.OfType<LoopMergeInstruction>().FirstOrDefault();
        Assert.That(loopMerge, Is.Not.Null, "for 循环应生成 LoopMergeInstruction");

        var branchCond = ir.Functions[0].Instructions.OfType<BranchConditionalInstruction>().FirstOrDefault();
        Assert.That(branchCond, Is.Not.Null, "for 循环应生成条件分支");

        var labels = ir.Functions[0].Instructions.OfType<LabelInstruction>().ToList();
        Assert.That(labels.Count, Is.GreaterThanOrEqualTo(4), "for 循环应包含 header/body/continue/merge 标签");
    }

    #endregion

    #region WhileStmt Tests

    [Test]
    public void VisitWhileStmt_GeneratesLoopMergeAndBranches()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                VoidType(),
                new BlockStmt([
                    new WhileStmt(
                        new BinaryExpr(new IdentifierNode("x"), ">", new LiteralExpr(LiteralType.Number, 0)),
                        new BlockStmt([]))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var loopMerge = ir.Functions[0].Instructions.OfType<LoopMergeInstruction>().FirstOrDefault();
        Assert.That(loopMerge, Is.Not.Null, "while 循环应生成 LoopMergeInstruction");

        var branchCond = ir.Functions[0].Instructions.OfType<BranchConditionalInstruction>().FirstOrDefault();
        Assert.That(branchCond, Is.Not.Null, "while 循环应生成条件分支");
    }

    #endregion

    #region DiscardStmt Tests

    [Test]
    public void VisitDiscardStmt_GeneratesDiscardInstruction()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                VoidType(),
                new BlockStmt([
                    new DiscardStmt()
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var discard = ir.Functions[0].Instructions.OfType<DiscardInstruction>().FirstOrDefault();
        Assert.That(discard, Is.Not.Null, "discard 语句应生成 DiscardInstruction");
    }

    #endregion

    #region SwizzleExpr Tests

    [Test]
    public void VisitSwizzleExpr_Xyzw_GeneratesVectorSwizzle()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                Vec3Type(),
                new BlockStmt([
                    new VariableDecl("v", Vec4Type(), null, false),
                    new ReturnStatement(new SwizzleExpr(new IdentifierNode("v"), "xyz"))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var swizzle = ir.Functions[0].Instructions.OfType<VectorSwizzleInstruction>().FirstOrDefault();
        Assert.That(swizzle, Is.Not.Null, "xyz swizzle 应生成 VectorSwizzleInstruction");
        Assert.That(swizzle.Components, Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void VisitSwizzleExpr_Rgba_GeneratesVectorSwizzle()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                Vec3Type(),
                new BlockStmt([
                    new VariableDecl("color", Vec4Type(), null, false),
                    new ReturnStatement(new SwizzleExpr(new IdentifierNode("color"), "rgb"))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var swizzle = ir.Functions[0].Instructions.OfType<VectorSwizzleInstruction>().FirstOrDefault();
        Assert.That(swizzle, Is.Not.Null, "rgb swizzle 应生成 VectorSwizzleInstruction");
        Assert.That(swizzle.Components, Is.EqualTo(new[] { 0, 1, 2 }));
    }

    [Test]
    public void VisitSwizzleExpr_SingleComponent_GeneratesCompositeExtract()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                F32Type(),
                new BlockStmt([
                    new VariableDecl("v", Vec4Type(), null, false),
                    new ReturnStatement(new SwizzleExpr(new IdentifierNode("v"), "x"))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var extract = ir.Functions[0].Instructions.OfType<CompositeExtractInstruction>().FirstOrDefault();
        Assert.That(extract, Is.Not.Null, "单分量 swizzle 应生成 CompositeExtractInstruction");
        Assert.That(extract.Indices, Is.EqualTo(new[] { 0 }));
    }

    #endregion

    #region IndexExpr Tests

    [Test]
    public void VisitIndexExpr_GeneratesAccessChainInstruction()
    {
        var ast = CreateCompilationUnit(
            new FunctionDecl("main", [],
                F32Type(),
                new BlockStmt([
                    new VariableDecl("arr", Vec4Type(), null, false),
                    new VariableDecl("i", I32Type(), new LiteralExpr(LiteralType.Number, 0), false),
                    new ReturnStatement(new TermIndexExpression(new IdentifierNode("arr"), new IdentifierNode("i")))
                ]),
                [FragmentAttr()]));

        var ir = _generator.Generate(ast);

        var access = ir.Functions[0].Instructions.OfType<AccessChainInstruction>().FirstOrDefault();
        Assert.That(access, Is.Not.Null, "索引访问应生成 AccessChainInstruction");
    }

    #endregion

    #region UniformBindingDecl Tests

    [Test]
    public void VisitUniformBindingDecl_GeneratesGlobalVariableAndResource()
    {
        var ast = CreateCompilationUnit(
            new UniformBindingDecl("uniforms", "uniform",
                new TypeAnnotation("UniformBuffer", []),
                0, 0, []));

        var ir = _generator.Generate(ast);

        Assert.That(ir.GlobalVariables, Has.Count.EqualTo(1));
        Assert.That(ir.GlobalVariables[0].Name, Is.EqualTo("uniforms"));
        Assert.That(ir.GlobalVariables[0].Storage, Is.EqualTo(StorageClass.Uniform));
        Assert.That(ir.GlobalVariables[0].Resource, Is.Not.Null);
        Assert.That(ir.GlobalVariables[0].Resource!.Kind, Is.EqualTo(ShaderResourceKind.UniformBuffer));
        Assert.That(ir.GlobalVariables[0].Resource!.DescriptorSet, Is.EqualTo(0));
        Assert.That(ir.GlobalVariables[0].Resource!.Binding, Is.EqualTo(0));
    }

    [Test]
    public void VisitUniformBindingDecl_Texture_GeneratesCorrectStorage()
    {
        var ast = CreateCompilationUnit(
            new UniformBindingDecl("diffuse_texture", "texture_2d",
                new TypeAnnotation("texture_2d", [F32Type()]),
                0, 1, []));

        var ir = _generator.Generate(ast);

        Assert.That(ir.GlobalVariables[0].Storage, Is.EqualTo(StorageClass.UniformConstant));
        Assert.That(ir.GlobalVariables[0].Resource!.Kind, Is.EqualTo(ShaderResourceKind.Texture));
    }

    #endregion

    #region Non-Shader Node Diagnostics Tests

    [Test]
    public void VisitSystemDecl_EmitsWarning()
    {
        var ast = CreateCompilationUnit(
            new SystemDecl("MoveSystem",
                [],
                [],
                []));

        _generator.Generate(ast);

        Assert.That(_diagnostics.GetWarnings().Any(), Is.True, "着色器中遇到 SystemDecl 应发出警告");
    }

    [Test]
    public void VisitLoopStmt_EmitsWarning()
    {
        var ast = CreateCompilationUnit(
            new LoopStmt("item", new IdentifierNode("items"),
                new BlockStmt([])));

        _generator.Generate(ast);

        Assert.That(_diagnostics.GetWarnings().Any(), Is.True, "着色器中遇到 for-each 循环应发出警告");
    }

    #endregion
}
