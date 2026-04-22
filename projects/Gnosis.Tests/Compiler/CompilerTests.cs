using Gnosis.Toolchain.ScriptCompiler;
using Gnosis.Toolchain.ScriptCompiler.AST;
using Gnosis.Toolchain.ScriptCompiler.Diagnostics;
using Gnosis.Toolchain.ScriptCompiler.Lexer;
using Gnosis.Toolchain.ScriptCompiler.Parser;
using Gnosis.Toolchain.ScriptCompiler.ScriptFrontend;
using Gnosis.Toolchain.ScriptCompiler.Backend;
using NUnit.Framework;

namespace Gnosis.Tests.Compiler;

/// <summary>
/// 编译器主入口集成测试
/// </summary>
[TestFixture]
public class CompilerTests
{
    private Compiler _compiler = null!;

    [SetUp]
    public void SetUp()
    {
        _compiler = new Compiler();
    }

    #region 基本编译流程

    [Test]
    public void Compile_EmptySource_ReturnsEmptyResult()
    {
        var result = _compiler.Compile("");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Compile_WhitespaceOnly_ReturnsEmptyResult()
    {
        var result = _compiler.Compile("   \n\t   ");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Compile_SimpleScript_ReturnsBytecode()
    {
        var source = @"
            let x: int = 42;
            let y: int = x + 1;
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Bytecode, Is.Not.Null);
    }

    [Test]
    public void Compile_ScriptWithFunction_ReturnsBytecode()
    {
        var source = @"
            fn add(a: int, b: int) -> int
            {
                return a + b;
            }

            let result: int = add(1, 2);
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 组件声明

    [Test]
    public void Compile_ComponentDecl_ReturnsBytecode()
    {
        var source = @"
            component Transform {
                vec3 position;
                vec3 rotation;
                vec3 scale;
            }
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Compile_ComponentWithAttributes_ReturnsBytecode()
    {
        var source = @"
            [Serializable]
            [Networked]
            component PlayerData {
                string name;
                int level;
                float health;
            }
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 系统声明

    [Test]
    public void Compile_SystemDecl_ReturnsBytecode()
    {
        var source = @"
            system MovementSystem {
                query: all(Transform, Velocity);

                fn update(deltaTime: float) -> void
                {
                    return;
                }
            }
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 导入声明

    [Test]
    public void Compile_ImportDecl_ReturnsBytecode()
    {
        var source = @"
            import math;
            import physics as phys;

            let x: float = 1.0;
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 控制流

    [Test]
    public void Compile_IfStatement_ReturnsBytecode()
    {
        var source = @"
            let x: int = 1;
            if (x > 0)
            {
                x = x + 1;
            }
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Compile_IfElseStatement_ReturnsBytecode()
    {
        var source = @"
            let x: int = 1;
            if (x > 0)
            {
                x = x + 1;
            }
            else
            {
                x = x - 1;
            }
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Compile_ForLoop_ReturnsBytecode()
    {
        var source = @"
            let sum: int = 0;
            for (let i: int = 0; i < 10; i = i + 1)
            {
                sum = sum + i;
            }
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Compile_WhileLoop_ReturnsBytecode()
    {
        var source = @"
            let x: int = 0;
            while (x < 10)
            {
                x = x + 1;
            }
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Compile_LoopStatement_ReturnsBytecode()
    {
        var source = @"
            let items: list<int> = [1, 2, 3];
            for item in items
            {
                let x: int = item;
            }
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 表达式

    [Test]
    public void Compile_BinaryExpression_ReturnsBytecode()
    {
        var source = @"
            let a: int = 1 + 2;
            let b: int = 3 * 4;
            let c: bool = a > b;
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Compile_LogicalExpression_ReturnsBytecode()
    {
        var source = @"
            let a: bool = true;
            let b: bool = false;
            let c: bool = a && b;
            let d: bool = a || b;
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Compile_MemberAccess_ReturnsBytecode()
    {
        var source = @"
            component Transform {
                vec3 position;
            }

            let t: Transform;
            let x: float = t.position.x;
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    [Test]
    public void Compile_FunctionCall_ReturnsBytecode()
    {
        var source = @"
            fn max(a: int, b: int) -> int
            {
                if (a > b)
                {
                    return a;
                }
                return b;
            }

            let result: int = max(10, 20);
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 结构体声明

    [Test]
    public void Compile_StructDecl_ReturnsBytecode()
    {
        var source = @"
            struct Point {
                float x;
                float y;
            }

            let p: Point;
            p.x = 1.0;
            p.y = 2.0;
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 插件声明

    [Test]
    public void Compile_PluginDecl_ReturnsBytecode()
    {
        var source = @"
            plugin MyPlugin;
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 小部件声明

    [Test]
    public void Compile_WidgetDecl_ReturnsBytecode()
    {
        var source = @"
            widget Button {
                text: """"Click Me"""",
                width: 100,
                height: 30
            }
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
    }

    #endregion

    #region 错误处理

    [Test]
    public void Compile_InvalidSyntax_ReturnsErrors()
    {
        var source = @"
            let x: int =
        ";

        var result = _compiler.Compile(source);

        Assert.That(result.Errors.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Compile_UndefinedVariable_ReturnsErrors()
    {
        var source = @"
            let x: int = y + 1;
        ";

        var result = _compiler.Compile(source);

        Assert.That(result.Errors.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Compile_TypeMismatch_ReturnsErrors()
    {
        var source = @"
            let x: int = """"hello"""";
        ";

        var result = _compiler.Compile(source);

        Assert.That(result.Errors.Count, Is.GreaterThan(0));
    }

    #endregion

    #region 完整场景

    [Test]
    public void Compile_CompleteGameScript_ReturnsBytecode()
    {
        var source = @"
            import math;
            import physics;

            [Serializable]
            component Transform {
                vec3 position;
                vec3 rotation;
                vec3 scale;
            }

            component Velocity {
                vec3 value;
            }

            system MovementSystem {
                query: all(Transform, Velocity);

                fn update(deltaTime: float) -> void
                {
                    for entity in query
                    {
                        let transform: Transform = entity.Transform;
                        let velocity: Velocity = entity.Velocity;
                        transform.position = transform.position + velocity.value * deltaTime;
                    }
                }
            }

            fn main() -> void
            {
                let x: int = 0;
                while (x < 10)
                {
                    x = x + 1;
                }
                return;
            }
        ";

        var result = _compiler.Compile(source);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Errors.Count, Is.EqualTo(0));
        Assert.That(result.Bytecode, Is.Not.Null);
    }

    #endregion
}
