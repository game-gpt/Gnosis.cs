using Gnosis.Graphic.Shader;
using Gnosis.Toolchain.ScriptCompiler;
using Gnosis.Toolchain.ShaderCompiler;
using NUnit.Framework;

namespace Gnosis.Tests.Compiler;

/// <summary>
/// Shader 编译器集成测试
/// </summary>
[TestFixture]
public class ShaderCompilerTests
{
    private ShaderCompiler _compiler = null!;

    [SetUp]
    public void SetUp()
    {
        _compiler = new ShaderCompiler();
    }

    #region 属性验证

    [Test]
    public void Target_ReturnsSPIRV()
    {
        Assert.That(_compiler.Target, Is.EqualTo(ShaderTarget.SPIRV));
    }

    [Test]
    public void Diagnostics_IsNotNull()
    {
        Assert.That(_compiler.Diagnostics, Is.Not.Null);
    }

    #endregion

    #region Validate 方法

    [Test]
    public void Validate_EmptySource_ReturnsTrue()
    {
        var result = _compiler.Validate("", out var errorMessage);

        Assert.That(result, Is.True);
        Assert.That(errorMessage, Is.Empty);
    }

    [Test]
    public void Validate_ValidVertexShader_ReturnsTrue()
    {
        var source = @"
            [Vertex]
            micro main(vec3 position) -> vec4
            {
                return vec4(position, 1.0);
            }
        ";

        var result = _compiler.Validate(source, out var errorMessage);

        Assert.That(result, Is.True);
        Assert.That(errorMessage, Is.Empty);
    }

    [Test]
    public void Validate_ValidFragmentShader_ReturnsTrue()
    {
        var source = @"
            [Fragment]
            micro main() -> vec4
            {
                return vec4(1.0, 0.0, 0.0, 1.0);
            }
        ";

        var result = _compiler.Validate(source, out var errorMessage);

        Assert.That(result, Is.True);
    }

    [Test]
    public void Validate_InvalidSyntax_ReturnsFalse()
    {
        var source = @"
            micro main(
        ";

        var result = _compiler.Validate(source, out var errorMessage);

        Assert.That(result, Is.False);
        Assert.That(errorMessage, Is.Not.Empty);
    }

    [Test]
    public void Validate_StructDecl_ReturnsTrue()
    {
        var source = @"
            struct VertexOutput {
                vec4 position;
                vec2 uv;
            }
        ";

        var result = _compiler.Validate(source, out var errorMessage);

        Assert.That(result, Is.True);
    }

    [Test]
    public void Validate_ImportDecl_ReturnsTrue()
    {
        var source = @"
            import math;
        ";

        var result = _compiler.Validate(source, out var errorMessage);

        Assert.That(result, Is.True);
    }

    #endregion

    #region CompileToSpirV 方法

    [Test]
    public void CompileToSpirV_SimpleVertexShader_ReturnsNonEmptyBytecode()
    {
        var source = @"
            [Vertex]
            micro main(vec3 position) -> vec4
            {
                return vec4(position, 1.0);
            }
        ";

        var bytecode = _compiler.CompileToSpirV(source);

        Assert.That(bytecode, Is.Not.Null);
        Assert.That(bytecode.Length, Is.GreaterThan(0));
    }

    [Test]
    public void CompileToSpirV_SimpleFragmentShader_ReturnsNonEmptyBytecode()
    {
        var source = @"
            [Fragment]
            micro main() -> vec4
            {
                return vec4(1.0, 0.0, 0.0, 1.0);
            }
        ";

        var bytecode = _compiler.CompileToSpirV(source);

        Assert.That(bytecode, Is.Not.Null);
        Assert.That(bytecode.Length, Is.GreaterThan(0));
    }

    [Test]
    public void CompileToSpirV_WithMacros_ReturnsNonEmptyBytecode()
    {
        var source = @"
            [Vertex]
            micro main(vec3 position) -> vec4
            {
                return vec4(position, 1.0);
            }
        ";

        var macros = new ChannelMacros(new Dictionary<string, string>
        {
            ["CUSTOM_MACRO"] = "1"
        });

        var bytecode = _compiler.CompileToSpirV(source, macros);

        Assert.That(bytecode, Is.Not.Null);
        Assert.That(bytecode.Length, Is.GreaterThan(0));
    }

    [Test]
    public void CompileToSpirV_InvalidSource_ThrowsShaderCompilationException()
    {
        var source = @"
            micro main(
        ";

        Assert.Throws<ShaderCompilationException>(() => _compiler.CompileToSpirV(source));
    }

    [Test]
    public void CompileToSpirV_CompleteShader_ReturnsNonEmptyBytecode()
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

        var bytecode = _compiler.CompileToSpirV(source);

        Assert.That(bytecode, Is.Not.Null);
        Assert.That(bytecode.Length, Is.GreaterThan(0));
    }

    #endregion

    #region Compile 方法

    [Test]
    public void Compile_ValidSource_ReturnsShaderModule()
    {
        var source = @"
            [Vertex]
            micro main(vec3 position) -> vec4
            {
                return vec4(position, 1.0);
            }
        ";

        var options = new ShaderCompileOptions();
        var module = _compiler.Compile(source, "TestModule", options);

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Name, Is.EqualTo("TestModule"));
    }

    [Test]
    public void Compile_WithDefines_ReturnsShaderModule()
    {
        var source = @"
            [Fragment]
            micro main() -> vec4
            {
                return vec4(1.0, 0.0, 0.0, 1.0);
            }
        ";

        var options = new ShaderCompileOptions
        {
            Defines = new Dictionary<string, string>
            {
                ["ENABLE_FOG"] = "1"
            }
        };

        var module = _compiler.Compile(source, "TestModule", options);

        Assert.That(module, Is.Not.Null);
    }

    [Test]
    public void Compile_WithDebugInfo_ReturnsShaderModule()
    {
        var source = @"
            [Fragment]
            micro main() -> vec4
            {
                return vec4(1.0);
            }
        ";

        var options = new ShaderCompileOptions
        {
            DebugInfo = true
        };

        var module = _compiler.Compile(source, "TestModule", options);

        Assert.That(module, Is.Not.Null);
    }

    [Test]
    public void Compile_InvalidSource_ThrowsShaderCompilationException()
    {
        var source = @"
            micro main(
        ";

        var options = new ShaderCompileOptions();

        Assert.Throws<ShaderCompilationException>(() => _compiler.Compile(source, "TestModule", options));
    }

    #endregion

    #region 宏处理

    [Test]
    public void CompileToSpirV_WithChannelMacros_ProcessesMacros()
    {
        var source = @"
            [Vertex]
            micro main(vec3 position) -> vec4
            {
                return vec4(position, 1.0);
            }
        ";

        var macros = new ChannelMacros(new Dictionary<string, string>
        {
            ["RENDER_MODE"] = "RAY_TRACING",
            ["HAS_RAY_TRACING"] = "true"
        });

        var bytecode = _compiler.CompileToSpirV(source, macros);

        Assert.That(bytecode, Is.Not.Null);
        Assert.That(bytecode.Length, Is.GreaterThan(0));
    }

    #endregion

    #region 错误诊断

    [Test]
    public void ShaderCompilationException_ContainsErrors()
    {
        var source = @"
            micro main(
        ";

        try
        {
            _compiler.CompileToSpirV(source);
            Assert.Fail("应抛出 ShaderCompilationException");
        }
        catch (ShaderCompilationException ex)
        {
            Assert.That(ex.Errors.Count, Is.GreaterThan(0));
            Assert.That(ex.Message, Does.Contain("着色器编译失败"));
        }
    }

    #endregion
}
