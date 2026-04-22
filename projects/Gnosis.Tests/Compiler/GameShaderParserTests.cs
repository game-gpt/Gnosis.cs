using Gnosis.Testing.Compiler.Parser;
using Gnosis.Toolchain.ScriptCompiler.AST;
using Gnosis.Toolchain.ScriptCompiler.Diagnostics;
using Gnosis.Toolchain.ScriptCompiler.Lexer;
using Gnosis.Toolchain.ScriptCompiler.Parser;
using NUnit.Framework;

namespace Gnosis.Tests.Compiler;

/// <summary>
/// GameShader 语法分析器测试
/// </summary>
[TestFixture]
public class GameShaderParserTests
{
    private ParserTester _tester = null!;

    [SetUp]
    public void SetUp()
    {
        var parser = new GameShaderParser();
        _tester = new ParserTester(parser, TimeSpan.FromSeconds(5), Console.WriteLine);
    }

    #region 空输入与基本结构

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
        var result = _tester.Parse("   \n\t   ");

        Assert.That(result.Ast, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 函数声明

    [Test]
    public void Parse_SimpleMicroFunction_ReturnsFunctionDecl()
    {
        var result = _tester.Parse(@"
            micro main() -> void
            {
                return;
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
        var unit = (CompilationUnit)result.Ast;
        Assert.That(unit.Declarations.Count, Is.GreaterThan(0));
        Assert.That(unit.Declarations[0], Is.InstanceOf<FunctionDecl>());
    }

    [Test]
    public void Parse_VertexShaderFunction_ReturnsFunctionDecl()
    {
        var result = _tester.Parse(@"
            [Vertex]
            micro main(vec3 position) -> vec4
            {
                return vec4(position, 1.0);
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
        var unit = (CompilationUnit)result.Ast;
        var func = (FunctionDecl)unit.Declarations[0];
        Assert.That(func.Attributes.Count, Is.GreaterThan(0));
        Assert.That(func.Attributes[0].Name, Is.EqualTo("Vertex"));
    }

    [Test]
    public void Parse_FragmentShaderFunction_ReturnsFunctionDecl()
    {
        var result = _tester.Parse(@"
            [Fragment]
            micro main() -> vec4
            {
                return vec4(1.0, 0.0, 0.0, 1.0);
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
        var unit = (CompilationUnit)result.Ast;
        var func = (FunctionDecl)unit.Declarations[0];
        Assert.That(func.Attributes[0].Name, Is.EqualTo("Fragment"));
    }

    [Test]
    public void Parse_FunctionWithParameters_ReturnsCorrectParameters()
    {
        var result = _tester.Parse(@"
            micro add(float a, float b) -> float
            {
                return a + b;
            }
        ");

        var unit = (CompilationUnit)result.Ast;
        var func = (FunctionDecl)unit.Declarations[0];
        Assert.That(func.Parameters.Count, Is.EqualTo(2));
        Assert.That(func.Parameters[0].Name, Is.EqualTo("a"));
        Assert.That(func.Parameters[1].Name, Is.EqualTo("b"));
    }

    #endregion

    #region 结构体声明

    [Test]
    public void Parse_StructDecl_ReturnsStructDecl()
    {
        var result = _tester.Parse(@"
            struct VertexOutput {
                vec4 position;
                vec2 uv;
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
        var unit = (CompilationUnit)result.Ast;
        Assert.That(unit.Declarations[0], Is.InstanceOf<StructDecl>());
        var structDecl = (StructDecl)unit.Declarations[0];
        Assert.That(structDecl.Name, Is.EqualTo("VertexOutput"));
    }

    [Test]
    public void Parse_StructWithFields_ReturnsCorrectFields()
    {
        var result = _tester.Parse(@"
            struct Material {
                vec4 diffuse;
                vec4 specular;
                float shininess;
            }
        ");

        var unit = (CompilationUnit)result.Ast;
        var structDecl = (StructDecl)unit.Declarations[0];
        Assert.That(structDecl.Fields.Count, Is.EqualTo(3));
        Assert.That(structDecl.Fields[0].Name, Is.EqualTo("diffuse"));
        Assert.That(structDecl.Fields[1].Name, Is.EqualTo("specular"));
        Assert.That(structDecl.Fields[2].Name, Is.EqualTo("shininess"));
    }

    #endregion

    #region 变量声明

    [Test]
    public void Parse_VariableDecl_ReturnsVariableDecl()
    {
        var result = _tester.Parse(@"
            let x: float = 1.0;
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
        var unit = (CompilationUnit)result.Ast;
        Assert.That(unit.Declarations[0], Is.InstanceOf<VariableDecl>());
    }

    [Test]
    public void Parse_UniformBinding_ReturnsUniformBindingDecl()
    {
        var result = _tester.Parse(@"
            let uniforms: UniformBuffer;
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
        var unit = (CompilationUnit)result.Ast;
        Assert.That(unit.Declarations[0], Is.InstanceOf<UniformBindingDecl>());
    }

    #endregion

    #region 导入声明

    [Test]
    public void Parse_ImportDecl_ReturnsImportDecl()
    {
        var result = _tester.Parse(@"
            import math;
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
        var unit = (CompilationUnit)result.Ast;
        Assert.That(unit.Declarations[0], Is.InstanceOf<ImportDecl>());
        var import = (ImportDecl)unit.Declarations[0];
        Assert.That(import.ModulePath, Is.EqualTo("math"));
    }

    [Test]
    public void Parse_ImportWithAlias_ReturnsImportDeclWithAlias()
    {
        var result = _tester.Parse(@"
            import math as m;
        ");

        var unit = (CompilationUnit)result.Ast;
        var import = (ImportDecl)unit.Declarations[0];
        Assert.That(import.Alias, Is.EqualTo("m"));
    }

    #endregion

    #region 控制流

    [Test]
    public void Parse_IfStatement_ReturnsIfStatement()
    {
        var result = _tester.Parse(@"
            micro test() -> void
            {
                if (true)
                {
                    return;
                }
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Parse_IfElseStatement_ReturnsIfStatement()
    {
        var result = _tester.Parse(@"
            micro test() -> void
            {
                if (true)
                {
                    return;
                }
                else
                {
                    return;
                }
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Parse_ForLoop_ReturnsForStmt()
    {
        var result = _tester.Parse(@"
            micro test() -> void
            {
                for (let i: int = 0; i < 10; i = i + 1)
                {
                    return;
                }
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Parse_WhileLoop_ReturnsWhileStmt()
    {
        var result = _tester.Parse(@"
            micro test() -> void
            {
                while (true)
                {
                    return;
                }
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Parse_DiscardStatement_ReturnsDiscardStmt()
    {
        var result = _tester.Parse(@"
            [Fragment]
            micro main() -> vec4
            {
                discard;
                return vec4(0.0);
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 表达式

    [Test]
    public void Parse_BinaryExpression_ReturnsBinaryExpr()
    {
        var result = _tester.Parse(@"
            micro test() -> float
            {
                return 1.0 + 2.0;
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Parse_MemberAccess_ReturnsMemberAccessExpr()
    {
        var result = _tester.Parse(@"
            micro test() -> float
            {
                let v: vec4 = vec4(1.0);
                return v.x;
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Parse_Swizzle_ReturnsSwizzleExpr()
    {
        var result = _tester.Parse(@"
            micro test() -> vec3
            {
                let v: vec4 = vec4(1.0);
                return v.xyz;
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Parse_FunctionCall_ReturnsCallExpr()
    {
        var result = _tester.Parse(@"
            micro test() -> vec4
            {
                return vec4(1.0, 0.0, 0.0, 1.0);
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 完整着色器

    [Test]
    public void Parse_CompleteVertexShader_ParsesSuccessfully()
    {
        var source = @"
            import math;

            struct VertexOutput {
                vec4 position;
                vec2 uv;
            }

            [Vertex]
            micro main(vec3 position, vec2 uv) -> VertexOutput
            {
                let output: VertexOutput;
                output.position = vec4(position, 1.0);
                output.uv = uv;
                return output;
            }
        ";

        var result = _tester.Parse(source);

        Assert.That(result.Errors.Count, Is.EqualTo(0));
        var unit = (CompilationUnit)result.Ast;
        Assert.That(unit.Declarations.Count, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void Parse_CompleteFragmentShader_ParsesSuccessfully()
    {
        var source = @"
            struct FragmentInput {
                vec4 position;
                vec2 uv;
            }

            let diffuse_texture: texture_2d;

            [Fragment]
            micro main(FragmentInput input) -> vec4
            {
                return vec4(1.0, 0.0, 0.0, 1.0);
            }
        ";

        var result = _tester.Parse(source);

        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 错误处理

    [Test]
    public void Parse_InvalidSyntax_GeneratesErrors()
    {
        var result = _tester.Parse(@"
            micro main(
        ");

        Assert.That(result.Errors.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Parse_MissingSemicolon_GeneratesErrors()
    {
        var result = _tester.Parse(@"
            let x: float = 1.0
            let y: float = 2.0;
        ");

        Assert.That(result.Errors.Count, Is.GreaterThan(0));
    }

    #endregion

    #region 元数据块

    [Test]
    public void Parse_MetaBlock_ParsesSuccessfully()
    {
        var result = _tester.Parse(@"
            <% layout(location = 0) %>
            [Vertex]
            micro main(vec3 position) -> vec4
            {
                return vec4(position, 1.0);
            }
        ");

        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion
}
