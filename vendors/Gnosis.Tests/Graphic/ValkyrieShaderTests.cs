using Gnosis.Graphic.Shader;
using Gnosis.Graphic.Shader.Spirv;
using Gnosis.IR.Shader;
using NUnit.Framework;

namespace Gnosis.Tests.Graphic;

[TestFixture]
public class ValkyrieShaderCompilerTests
{
    [Test]
    public void Validate_EmptySource_ReturnsTrue()
    {
        var compiler = new ValkyrieShaderCompiler();

        var result = compiler.Validate("", out var errorMessage);

        Assert.That(result, Is.True);
        Assert.That(errorMessage, Is.Empty);
    }

    [Test]
    public void Validate_ValidShaderStruct_ReturnsTrue()
    {
        var compiler = new ValkyrieShaderCompiler();
        var source = @"
struct VertexInput {
    position: vec3;
    uv: vec2;
};
";

        var result = compiler.Validate(source, out var errorMessage);

        Assert.That(result, Is.True, $"验证失败：{errorMessage}");
    }

    [Test]
    public void Compile_EmptyShader_ProducesSpirvModule()
    {
        var compiler = new ValkyrieShaderCompiler();
        var source = @"
shader SimpleShader {
    vertex main {
    }
    fragment main {
    }
};
";

        var module = compiler.Compile(source, "SimpleShader", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Name, Is.EqualTo("SimpleShader"));
        Assert.That(module.Language, Is.EqualTo(ShaderLanguage.Valkyrie));
        Assert.That(module.Target, Is.EqualTo(ShaderTarget.Spirv));
        Assert.That(module.Bytecode, Is.Not.Null);
        Assert.That(module.Bytecode.Length, Is.GreaterThan(0));
    }

    [Test]
    public void Compile_ShaderWithStruct_ProducesSpirvModule()
    {
        var compiler = new ValkyrieShaderCompiler();
        var source = @"
struct UniformBuffer {
    mvp: mat4;
    color: vec4;
};

shader MeshShader {
    vertex vs_main {
    }
    fragment fs_main {
    }
};
";

        var module = compiler.Compile(source, "MeshShader", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Bytecode.Length, Is.GreaterThan(0));
    }

    [Test]
    public void Compile_ComputeShader_ProducesSpirvModule()
    {
        var compiler = new ValkyrieShaderCompiler();
        var source = @"
shader ComputeTest {
    compute cs_main {
    }
};
";

        var module = compiler.Compile(source, "ComputeTest", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Functions.Count, Is.GreaterThan(0));
        Assert.That(module.Functions[0].Kind, Is.EqualTo(MicroFunctionKind.Compute));
    }

    [Test]
    public void Compile_WithOptimization_ProducesValidSpirv()
    {
        var compiler = new ValkyrieShaderCompiler();
        var source = @"
shader OptShader {
    vertex vs {
    }
    fragment fs {
    }
};
";

        var module = compiler.Compile(source, "OptShader",
            new ShaderCompileOptions { OptimizationLevel = OptimizationLevel.Maximum });

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Bytecode.Length, Is.GreaterThan(0));
    }
}

[TestFixture]
public class ValkyrieShaderLoweringTests
{
    [Test]
    public void LowerFromTokens_ShaderDecl_CreatesEntryPoints()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new Oak.Valkyrie.Lexer.ValkyrieLexer(Oak.Valkyrie.ValkyrieLanguage.Shader);
        var source = @"
shader Test {
    vertex vs {
    }
    fragment fs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "Test");

        Assert.That(module.EntryPoints.Count, Is.EqualTo(2));
        Assert.That(module.EntryPoints[0].ExecutionModel, Is.EqualTo(ShaderExecutionModel.Vertex));
        Assert.That(module.EntryPoints[1].ExecutionModel, Is.EqualTo(ShaderExecutionModel.Fragment));
    }

    [Test]
    public void LowerFromTokens_StructDecl_CreatesStructIr()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new Oak.Valkyrie.Lexer.ValkyrieLexer(Oak.Valkyrie.ValkyrieLanguage.Shader);
        var source = @"
struct MyVertex {
    pos: vec3;
    uv: vec2;
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "Test");

        Assert.That(module.Structs.Count, Is.EqualTo(1));
        Assert.That(module.Structs[0].Name, Is.EqualTo("MyVertex"));
        Assert.That(module.Structs[0].Fields.Count, Is.EqualTo(2));
        Assert.That(module.Structs[0].Fields[0].Name, Is.EqualTo("pos"));
        Assert.That(module.Structs[0].Fields[1].Name, Is.EqualTo("uv"));
    }

    [Test]
    public void LowerFromTokens_UniformDecl_CreatesResourceIr()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new Oak.Valkyrie.Lexer.ValkyrieLexer(Oak.Valkyrie.ValkyrieLanguage.Shader);
        var source = @"
shader ResTest {
    uniform mvp: mat4;
    uniform tint: vec4;
    vertex vs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "ResTest");

        Assert.That(module.Resources.Count, Is.EqualTo(2));
        Assert.That(module.Resources[0].Name, Is.EqualTo("mvp"));
        Assert.That(module.Resources[0].Kind, Is.EqualTo(ShaderResourceKind.UniformBuffer));
        Assert.That(module.Resources[1].Name, Is.EqualTo("tint"));
    }

    [Test]
    public void LowerFromTokens_TextureAndSampler_CreatesResourceIr()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new Oak.Valkyrie.Lexer.ValkyrieLexer(Oak.Valkyrie.ValkyrieLanguage.Shader);
        var source = @"
shader TexTest {
    texture albedo: sampler2D;
    sampler linearSampler;
    vertex vs {
    }
    fragment fs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "TexTest");

        Assert.That(module.Resources.Count(r => r.Kind == ShaderResourceKind.SampledImage), Is.EqualTo(1));
        Assert.That(module.Resources.Count(r => r.Kind == ShaderResourceKind.Sampler), Is.EqualTo(1));
    }

    [Test]
    public void LowerFromTokens_CBuffer_CreatesUniformBufferResource()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new Oak.Valkyrie.Lexer.ValkyrieLexer(Oak.Valkyrie.ValkyrieLanguage.Shader);
        var source = @"
shader CBTest {
    cbuffer SceneData {
        viewProj: mat4;
        eyePos: vec3;
    };
    vertex vs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "CBTest");

        Assert.That(module.Resources.Count(r => r.Kind == ShaderResourceKind.UniformBuffer), Is.GreaterThanOrEqualTo(1));
        Assert.That(module.Structs.Count(s => s.Name == "SceneData"), Is.EqualTo(1));
    }
}

[TestFixture]
public class SpirvGeneratorTests
{
    [Test]
    public void Generate_EmptyModule_ProducesValidSpirvHeader()
    {
        var module = new ShaderModuleIr
        {
            Name = "Empty",
            Language = ShaderLanguage.Valkyrie,
            Target = ShaderTarget.Spirv
        };

        var generator = new SpirvGenerator();
        var spirv = generator.Generate(module);

        Assert.That(spirv, Is.Not.Null);
        Assert.That(spirv.Length, Is.GreaterThan(20));

        var magic = BitConverter.ToUInt32(spirv, 0);
        Assert.That(magic, Is.EqualTo(0x07230203u));
    }

    [Test]
    public void Generate_ModuleWithEntryPoint_ProducesValidSpirv()
    {
        var module = new ShaderModuleIr
        {
            Name = "Triangle",
            Language = ShaderLanguage.Valkyrie,
            Target = ShaderTarget.Spirv
        };

        var func = new ShaderFunctionIr
        {
            Name = "vs_main",
            ReturnType = ShaderIrType.Void,
            IsEntryPoint = true,
            EntryPointModel = ShaderExecutionModel.Vertex
        };

        module.Functions.Add(func);
        module.EntryPoints.Add(new ShaderEntryPointIr
        {
            Name = "vs_main",
            ExecutionModel = ShaderExecutionModel.Vertex,
            FunctionName = "vs_main"
        });

        var generator = new SpirvGenerator();
        var spirv = generator.Generate(module);

        Assert.That(spirv.Length, Is.GreaterThan(0));

        var magic = BitConverter.ToUInt32(spirv, 0);
        Assert.That(magic, Is.EqualTo(0x07230203u));
    }
}
