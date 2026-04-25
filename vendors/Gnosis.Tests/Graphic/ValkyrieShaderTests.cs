using System.Buffers.Binary;
using System.Text;
using Acorn.Spirv.Data;
using Acorn.Spirv.Decode;
using Gnosis.Graphic.Shader;
using Gnosis.Graphic.Shader.Spirv;
using Gnosis.IR.Shader;
using NUnit.Framework;
using Oak.Valkyrie;
using Oak.Valkyrie.Lexer;

namespace Gnosis.Tests.Graphic;

#region 编译管线端到端测试

[TestFixture]
public class ValkyrieShaderPipelineTests
{
    private ValkyrieShaderCompiler _compiler = null!;

    [SetUp]
    public void SetUp()
    {
        _compiler = new ValkyrieShaderCompiler();
    }

    [Test]
    public void Pipeline_VertexFragmentShader_ProducesValidSpirv()
    {
        var source = @"
shader BasicVF {
    vertex vs_main {
    }
    fragment fs_main {
    }
};
";
        var module = _compiler.Compile(source, "BasicVF", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Name, Is.EqualTo("BasicVF"));
        Assert.That(module.Language, Is.EqualTo(ShaderLanguage.Valkyrie));
        Assert.That(module.Target, Is.EqualTo(ShaderTarget.Spirv));
        Assert.That(module.Bytecode, Is.Not.Null);
        Assert.That(module.Bytecode.Length, Is.GreaterThan(20));
        Assert.That(module.Functions.Count, Is.EqualTo(2));
        Assert.That(module.Functions[0].Kind, Is.EqualTo(MicroFunctionKind.Vertex));
        Assert.That(module.Functions[1].Kind, Is.EqualTo(MicroFunctionKind.Fragment));
    }

    [Test]
    public void Pipeline_ComputeShader_ProducesValidSpirv()
    {
        var source = @"
shader ParticleCompute {
    compute cs_main {
    }
};
";
        var module = _compiler.Compile(source, "ParticleCompute", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Functions.Count, Is.EqualTo(1));
        Assert.That(module.Functions[0].Kind, Is.EqualTo(MicroFunctionKind.Compute));
        Assert.That(module.Functions[0].Name, Does.Contain("cs_main"));
    }

    [Test]
    public void Pipeline_ShaderWithUniformsAndTextures_ProducesValidSpirv()
    {
        var source = @"
shader TexturedMesh {
    uniform mvp: mat4;
    uniform tint: vec4;
    texture albedo: sampler2D;
    sampler linearSampler;
    vertex vs_main {
    }
    fragment fs_main {
    }
};
";
        var module = _compiler.Compile(source, "TexturedMesh", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Bytecode.Length, Is.GreaterThan(20));
    }

    [Test]
    public void Pipeline_ShaderWithCBuffer_ProducesValidSpirv()
    {
        var source = @"
cbuffer SceneData {
    viewProj: mat4;
    eyePos: vec3;
    deltaTime: f32;
};

shader CBufferTest {
    vertex vs_main {
    }
    fragment fs_main {
    }
};
";
        var module = _compiler.Compile(source, "CBufferTest", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Bytecode.Length, Is.GreaterThan(20));
    }

    [Test]
    public void Pipeline_ShaderWithStructs_ProducesValidSpirv()
    {
        var source = @"
struct VertexInput {
    position: vec3;
    normal: vec3;
    uv: vec2;
};

struct MaterialParams {
    baseColor: vec4;
    metallic: f32;
    roughness: f32;
};

shader StructShader {
    vertex vs_main {
    }
    fragment fs_main {
    }
};
";
        var module = _compiler.Compile(source, "StructShader", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Bytecode.Length, Is.GreaterThan(20));
    }

    [Test]
    public void Pipeline_FullShaderProgram_ProducesValidSpirv()
    {
        var source = @"
struct TransformData {
    model: mat4;
    view: mat4;
    projection: mat4;
};

cbuffer FrameData {
    viewProj: mat4;
    eyePos: vec3;
    time: f32;
};

uniform baseColor: vec4;
texture albedoMap: sampler2D;
texture normalMap: sampler2D;
sampler pointSampler;
sampler linearSampler;

shader FullProgram {
    vertex vs_main {
    }
    fragment fs_main {
    }
};
";
        var module = _compiler.Compile(source, "FullProgram", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Bytecode.Length, Is.GreaterThan(20));
    }

    [Test]
    public void Pipeline_MultipleOptimizationLevels_AllProduceValidSpirv()
    {
        var source = @"
shader OptTest {
    uniform mvp: mat4;
    vertex vs {
    }
    fragment fs {
    }
};
";
        foreach (OptimizationLevel level in Enum.GetValues(typeof(OptimizationLevel)))
        {
            var module = _compiler.Compile(source, $"OptTest_{level}",
                new ShaderCompileOptions { OptimizationLevel = level });

            Assert.That(module, Is.Not.Null, $"优化级别 {level} 产生 null 模块");
            Assert.That(module.Bytecode.Length, Is.GreaterThan(20), $"优化级别 {level} 产生的 SPIR-V 过短");
        }
    }

    [Test]
    public void Pipeline_ValidateValidSource_ReturnsTrue()
    {
        var source = @"
shader ValidShader {
    vertex main {
    }
    fragment main {
    }
};
";
        var result = _compiler.Validate(source, out var errorMessage);

        Assert.That(result, Is.True, $"验证失败：{errorMessage}");
        Assert.That(errorMessage, Is.Empty);
    }

    [Test]
    public void Pipeline_CompileProducesConsistentResults()
    {
        var source = @"
shader ConsistencyTest {
    uniform mvp: mat4;
    vertex vs {
    }
    fragment fs {
    }
};
";
        var module1 = _compiler.Compile(source, "ConsistencyTest", new ShaderCompileOptions());
        var module2 = _compiler.Compile(source, "ConsistencyTest", new ShaderCompileOptions());

        Assert.That(module1.Bytecode.Length, Is.EqualTo(module2.Bytecode.Length));
        Assert.That(module1.Functions.Count, Is.EqualTo(module2.Functions.Count));
    }
}

#endregion

#region SPIR-V 二进制正确性验证测试

[TestFixture]
public class SpirvBinaryValidationTests
{
    private ValkyrieShaderCompiler _compiler = null!;
    private SpirvValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _compiler = new ValkyrieShaderCompiler();
        _validator = new SpirvValidator();
    }

    [Test]
    public void SpirvBinary_HasValidMagicNumber()
    {
        var source = @"
shader MagicTest {
    vertex vs {
    }
};
";
        var module = _compiler.Compile(source, "MagicTest", new ShaderCompileOptions());
        var spirv = module.Bytecode;

        Assert.That(spirv.Length, Is.GreaterThanOrEqualTo(20));

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(spirv.AsSpan(0, 4));
        Assert.That(magic, Is.EqualTo(0x07230203u), "SPIR-V 魔数不匹配");
    }

    [Test]
    public void SpirvBinary_HasValidVersion()
    {
        var source = @"
shader VersionTest {
    vertex vs {
    }
};
";
        var module = _compiler.Compile(source, "VersionTest", new ShaderCompileOptions());
        var spirv = module.Bytecode;

        var version = BinaryPrimitives.ReadUInt32LittleEndian(spirv.AsSpan(4, 4));
        var major = (version >> 16) & 0xFF;
        var minor = (version >> 8) & 0xFF;

        Assert.That(major, Is.EqualTo(1), "SPIR-V 主版本号应为 1");
        Assert.That(minor, Is.LessThanOrEqualTo(6), "SPIR-V 次版本号不应超过 6");
    }

    [Test]
    public void SpirvBinary_HasNonZeroBound()
    {
        var source = @"
shader BoundTest {
    vertex vs {
    }
    fragment fs {
    }
};
";
        var module = _compiler.Compile(source, "BoundTest", new ShaderCompileOptions());
        var spirv = module.Bytecode;

        var bound = BinaryPrimitives.ReadUInt32LittleEndian(spirv.AsSpan(12, 4));
        Assert.That(bound, Is.GreaterThan(0), "SPIR-V Bound 值应为正数");
    }

    [Test]
    public void SpirvBinary_SchemaFieldIsZero()
    {
        var source = @"
shader SchemaTest {
    vertex vs {
    }
};
";
        var module = _compiler.Compile(source, "SchemaTest", new ShaderCompileOptions());
        var spirv = module.Bytecode;

        var schema = BinaryPrimitives.ReadUInt32LittleEndian(spirv.AsSpan(16, 4));
        Assert.That(schema, Is.EqualTo(0u), "SPIR-V Schema 保留字段应为 0");
    }

    [Test]
    public void SpirvBinary_PassesValidatorCheck()
    {
        var source = @"
shader ValidatorTest {
    uniform mvp: mat4;
    vertex vs {
    }
    fragment fs {
    }
};
";
        var module = _compiler.Compile(source, "ValidatorTest", new ShaderCompileOptions());

        var result = _validator.Validate(module.Bytecode);

        var criticalErrors = result.Errors
            .Where(e => e.Contains("OpCapability") || e.Contains("OpMemoryModel") || e.Contains("魔数"))
            .ToList();

        Assert.That(criticalErrors, Is.Empty, $"SPIR-V 关键验证错误：{string.Join(", ", criticalErrors)}");
    }

    [Test]
    public void SpirvBinary_ContainsCapabilityInstructions()
    {
        var source = @"
shader CapTest {
    vertex vs {
    }
};
";
        var module = _compiler.Compile(source, "CapTest", new ShaderCompileOptions());
        var decoded = new SpirvDecoder(module.Bytecode).Decode();

        var hasCapability = decoded.Instructions.Any(i => i.Opcode == SpirvOpCode.OpCapability);
        Assert.That(hasCapability, Is.True, "SPIR-V 模块应包含 OpCapability 指令");
    }

    [Test]
    public void SpirvBinary_ContainsMemoryModelInstruction()
    {
        var source = @"
shader MemModelTest {
    vertex vs {
    }
};
";
        var module = _compiler.Compile(source, "MemModelTest", new ShaderCompileOptions());
        var decoded = new SpirvDecoder(module.Bytecode).Decode();

        var hasMemoryModel = decoded.Instructions.Any(i => i.Opcode == SpirvOpCode.OpMemoryModel);
        Assert.That(hasMemoryModel, Is.True, "SPIR-V 模块应包含 OpMemoryModel 指令");
    }

    [Test]
    public void SpirvBinary_ContainsEntryPointInstruction()
    {
        var source = @"
shader EntryTest {
    vertex vs_main {
    }
    fragment fs_main {
    }
};
";
        var module = _compiler.Compile(source, "EntryTest", new ShaderCompileOptions());
        var decoded = new SpirvDecoder(module.Bytecode).Decode();

        var entryPoints = decoded.Instructions.Count(i => i.Opcode == SpirvOpCode.OpEntryPoint);
        Assert.That(entryPoints, Is.EqualTo(2), "应包含 2 个 OpEntryPoint 指令");
    }

    [Test]
    public void SpirvBinary_StructShader_ContainsTypeInstructions()
    {
        var source = @"
struct MyStruct {
    pos: vec3;
    uv: vec2;
};

shader TypeTest {
    vertex vs {
    }
};
";
        var module = _compiler.Compile(source, "TypeTest", new ShaderCompileOptions());
        var decoded = new SpirvDecoder(module.Bytecode).Decode();

        var hasStructType = decoded.Instructions.Any(i => i.Opcode == SpirvOpCode.OpTypeStruct);
        Assert.That(hasStructType, Is.True, "含结构体的着色器应包含 OpTypeStruct 指令");
    }

    [Test]
    public void SpirvBinary_DecodeReEncode_RoundTrip()
    {
        var source = @"
shader RoundTripTest {
    uniform mvp: mat4;
    vertex vs {
    }
    fragment fs {
    }
};
";
        var module = _compiler.Compile(source, "RoundTripTest", new ShaderCompileOptions());
        var originalSpirv = module.Bytecode;

        var decoded = new SpirvDecoder(originalSpirv).Decode();

        Assert.That(decoded.MagicNumber, Is.EqualTo(SpirvConstants.MagicNumber));
        Assert.That(decoded.Bound, Is.GreaterThan(0u));
        Assert.That(decoded.Instructions.Count, Is.GreaterThan(0));
    }

    [Test]
    public void SpirvBinary_Disassembler_ProducesReadableOutput()
    {
        var source = @"
shader DisasmTest {
    vertex vs {
    }
};
";
        var module = _compiler.Compile(source, "DisasmTest", new ShaderCompileOptions());
        var disassembler = new SpirvDisassembler();
        var text = disassembler.Disassemble(module.Bytecode);

        Assert.That(text, Is.Not.Null.Or.Empty);
        Assert.That(text, Does.Contain("SPIR-V"), "反汇编输出应包含 SPIR-V 标识");
        Assert.That(text, Does.Contain("OpCapability"), "反汇编输出应包含 OpCapability");
        Assert.That(text, Does.Contain("OpMemoryModel"), "反汇编输出应包含 OpMemoryModel");
    }

    [Test]
    public void SpirvBinary_OptimizedVersion_SmallerOrEqual()
    {
        var source = @"
shader OptSizeTest {
    uniform mvp: mat4;
    uniform tint: vec4;
    texture albedo: sampler2D;
    vertex vs {
    }
    fragment fs {
    }
};
";
        var unoptimized = _compiler.Compile(source, "OptSizeTest",
            new ShaderCompileOptions { OptimizationLevel = OptimizationLevel.None });
        var optimized = _compiler.Compile(source, "OptSizeTest",
            new ShaderCompileOptions { OptimizationLevel = OptimizationLevel.Maximum });

        Assert.That(optimized.Bytecode.Length, Is.LessThanOrEqualTo(unoptimized.Bytecode.Length),
            "优化后的 SPIR-V 不应比未优化的大");
    }
}

#endregion

#region 后端兼容性测试

[TestFixture]
public class SpirvBackendCompatibilityTests
{
    private ValkyrieShaderCompiler _compiler = null!;

    [SetUp]
    public void SetUp()
    {
        _compiler = new ValkyrieShaderCompiler();
    }

    [Test]
    public void BackendCompatibility_ShaderDesc_IsSpirvFlag()
    {
        var source = @"
shader SpirvFlagTest {
    vertex vs {
    }
};
";
        var module = _compiler.Compile(source, "SpirvFlagTest", new ShaderCompileOptions());

        var desc = new Gnosis.Graphic.RHI.ShaderDesc
        {
            Bytecode = module.Bytecode,
            Stage = ShaderStage.Vertex,
            EntryPoint = "vs",
            IsSpirv = true
        };

        Assert.That(desc.IsSpirv, Is.True, "ShaderDesc.IsSpirv 应为 true");
        Assert.That(desc.Bytecode, Is.EqualTo(module.Bytecode));
        Assert.That(desc.EntryPoint, Is.EqualTo("vs"));
    }

    [Test]
    public void BackendCompatibility_VertexShaderDesc_StageMapping()
    {
        var source = @"
shader StageMapTest {
    vertex vs_main {
    }
    fragment fs_main {
    }
};
";
        var module = _compiler.Compile(source, "StageMapTest", new ShaderCompileOptions());

        var vsDesc = new Gnosis.Graphic.RHI.ShaderDesc
        {
            Bytecode = module.Bytecode,
            Stage = ShaderStage.Vertex,
            EntryPoint = "vs_main",
            IsSpirv = true
        };

        var fsDesc = new Gnosis.Graphic.RHI.ShaderDesc
        {
            Bytecode = module.Bytecode,
            Stage = ShaderStage.Fragment,
            EntryPoint = "fs_main",
            IsSpirv = true
        };

        Assert.That(vsDesc.Stage, Is.EqualTo(ShaderStage.Vertex));
        Assert.That(fsDesc.Stage, Is.EqualTo(ShaderStage.Fragment));
    }

    [Test]
    public void BackendCompatibility_ComputeShaderDesc_StageMapping()
    {
        var source = @"
shader ComputeStageTest {
    compute cs_main {
    }
};
";
        var module = _compiler.Compile(source, "ComputeStageTest", new ShaderCompileOptions());

        var csDesc = new Gnosis.Graphic.RHI.ShaderDesc
        {
            Bytecode = module.Bytecode,
            Stage = ShaderStage.Compute,
            EntryPoint = "cs_main",
            IsSpirv = true
        };

        Assert.That(csDesc.Stage, Is.EqualTo(ShaderStage.Compute));
    }

    [Test]
    public void BackendCompatibility_DelegateShaderModule_Properties()
    {
        var source = @"
shader ModulePropTest {
    vertex vs {
    }
    fragment fs {
    }
};
";
        var module = _compiler.Compile(source, "ModulePropTest", new ShaderCompileOptions());

        Assert.That(module, Is.InstanceOf<DelegateShaderModule>());
        Assert.That(module.Name, Is.EqualTo("ModulePropTest"));
        Assert.That(module.Language, Is.EqualTo(ShaderLanguage.Valkyrie));
        Assert.That(module.Target, Is.EqualTo(ShaderTarget.Spirv));
        Assert.That(module.Bytecode, Is.Not.Null);
        Assert.That(module.Bytecode.Length, Is.GreaterThan(0));
    }

    [Test]
    public void BackendCompatibility_DelegateShaderModule_UniformManagement()
    {
        var module = new DelegateShaderModule(
            "UniformTest",
            [],
            ShaderLanguage.Valkyrie,
            ShaderTarget.Spirv,
            []);

        module.SetUniform("mvp", 1.0f);
        module.SetUniform("count", 42);

        Assert.That(module.GetUniform<float>("mvp"), Is.EqualTo(1.0f));
        Assert.That(module.GetUniform<int>("count"), Is.EqualTo(42));
        Assert.That(module.GetUniform<float>("nonexistent"), Is.Null);
    }

    [Test]
    public void BackendCompatibility_SpirvBytecode_VulkanCompatible()
    {
        var source = @"
shader VulkanCompatTest {
    uniform mvp: mat4;
    vertex vs_main {
    }
    fragment fs_main {
    }
};
";
        var module = _compiler.Compile(source, "VulkanCompatTest", new ShaderCompileOptions());
        var spirv = module.Bytecode;

        Assert.That(spirv.Length % 4, Is.EqualTo(0), "SPIR-V 字节码长度应为 4 的倍数");

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(spirv.AsSpan(0, 4));
        Assert.That(magic, Is.EqualTo(0x07230203u), "Vulkan 要求正确的 SPIR-V 魔数");

        var bound = BinaryPrimitives.ReadUInt32LittleEndian(spirv.AsSpan(12, 4));
        Assert.That(bound, Is.GreaterThan(0u), "Vulkan 要求非零 Bound 值");
    }

    [Test]
    public void BackendCompatibility_SpirvBytecode_OpenGLCompatible()
    {
        var source = @"
shader GLCompatTest {
    vertex vs {
    }
    fragment fs {
    }
};
";
        var module = _compiler.Compile(source, "GLCompatTest", new ShaderCompileOptions());
        var spirv = module.Bytecode;

        Assert.That(spirv.Length % 4, Is.EqualTo(0), "OpenGL SPIR-V 加载要求 4 字节对齐");

        var version = BinaryPrimitives.ReadUInt32LittleEndian(spirv.AsSpan(4, 4));
        var major = (version >> 16) & 0xFF;
        Assert.That(major, Is.EqualTo(1), "OpenGL 兼容 SPIR-V 1.x");
    }

    [Test]
    public void BackendCompatibility_ShaderFormatHandler_CompilesValkyrieToSpirv()
    {
        var handler = new ShaderFormatHandler();
        handler.SetCompiler(new ValkyrieShaderCompiler());

        var shaderData = new ShaderData
        {
            Name = "FormatHandlerTest",
            Language = ShaderLanguage.Valkyrie,
            SourceCode = @"
shader FHTest {
    vertex vs {
    }
    fragment fs {
    }
};
"
        };

        var bytecode = handler.CompileAsync(shaderData, ShaderTarget.Spirv).Result;

        Assert.That(bytecode, Is.Not.Null);
        Assert.That(bytecode.Length, Is.GreaterThan(20));

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(bytecode.AsSpan(0, 4));
        Assert.That(magic, Is.EqualTo(0x07230203u));
    }

    [Test]
    public void BackendCompatibility_MultipleEntryPointNames()
    {
        var source = @"
shader MultiEntryTest {
    vertex vert_main {
    }
    fragment frag_main {
    }
};
";
        var module = _compiler.Compile(source, "MultiEntryTest", new ShaderCompileOptions());

        var funcNames = module.Functions.Select(f => f.Name).ToList();
        Assert.That(funcNames, Does.Contain("MultiEntryTest_vert_main").Or.Contain("vert_main"));
        Assert.That(funcNames, Does.Contain("MultiEntryTest_frag_main").Or.Contain("frag_main"));
    }
}

#endregion

#region 资源绑定正确性测试

[TestFixture]
public class ValkyrieResourceBindingTests
{
    private ValkyrieShaderCompiler _compiler = null!;

    [SetUp]
    public void SetUp()
    {
        _compiler = new ValkyrieShaderCompiler();
    }

    [Test]
    public void ResourceBinding_UniformDecl_CreatesCorrectResource()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
        var source = @"
shader UBTest {
    uniform mvp: mat4;
    vertex vs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "UBTest");

        Assert.That(module.Resources.Count, Is.EqualTo(1));
        Assert.That(module.Resources[0].Name, Is.EqualTo("mvp"));
        Assert.That(module.Resources[0].Kind, Is.EqualTo(ShaderResourceKind.UniformBuffer));
        Assert.That(module.Resources[0].DescriptorSet, Is.EqualTo(0u));
    }

    [Test]
    public void ResourceBinding_MultipleUniforms_IncrementBinding()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
        var source = @"
shader MultiUBTest {
    uniform mvp: mat4;
    uniform tint: vec4;
    uniform time: f32;
    vertex vs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "MultiUBTest");

        Assert.That(module.Resources.Count, Is.EqualTo(3));
        Assert.That(module.Resources[0].Binding, Is.Not.EqualTo(module.Resources[1].Binding));
        Assert.That(module.Resources[1].Binding, Is.Not.EqualTo(module.Resources[2].Binding));
    }

    [Test]
    public void ResourceBinding_TextureDecl_CreatesSampledImageResource()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
        var source = @"
shader TexResTest {
    texture albedo: sampler2D;
    vertex vs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "TexResTest");

        Assert.That(module.Resources.Count, Is.EqualTo(1));
        Assert.That(module.Resources[0].Name, Is.EqualTo("albedo"));
        Assert.That(module.Resources[0].Kind, Is.EqualTo(ShaderResourceKind.SampledImage));
    }

    [Test]
    public void ResourceBinding_SamplerDecl_CreatesSamplerResource()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
        var source = @"
shader SampResTest {
    sampler pointSampler;
    vertex vs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "SampResTest");

        Assert.That(module.Resources.Count, Is.EqualTo(1));
        Assert.That(module.Resources[0].Name, Is.EqualTo("pointSampler"));
        Assert.That(module.Resources[0].Kind, Is.EqualTo(ShaderResourceKind.Sampler));
    }

    [Test]
    public void ResourceBinding_CBufferDecl_CreatesStructAndResource()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
        var source = @"
shader CBResTest {
    cbuffer SceneData {
        viewProj: mat4;
        eyePos: vec3;
        time: f32;
    };
    vertex vs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "CBResTest");

        var ubResources = module.Resources.Where(r => r.Kind == ShaderResourceKind.UniformBuffer).ToList();
        Assert.That(ubResources.Count, Is.GreaterThanOrEqualTo(1));

        var sceneDataStruct = module.Structs.FirstOrDefault(s => s.Name == "SceneData");
        Assert.That(sceneDataStruct, Is.Not.Null, "cbuffer 应创建对应的结构体");
        Assert.That(sceneDataStruct!.Fields.Count, Is.EqualTo(3));
        Assert.That(sceneDataStruct.Fields[0].Name, Is.EqualTo("viewProj"));
        Assert.That(sceneDataStruct.Fields[1].Name, Is.EqualTo("eyePos"));
        Assert.That(sceneDataStruct.Fields[2].Name, Is.EqualTo("time"));
    }

    [Test]
    public void ResourceBinding_VaryingDecl_CreatesInputAttachmentResource()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
        var source = @"
shader VaryingResTest {
    varying color: vec4;
    vertex vs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "VaryingResTest");

        Assert.That(module.Resources.Count, Is.EqualTo(1));
        Assert.That(module.Resources[0].Name, Is.EqualTo("color"));
        Assert.That(module.Resources[0].Kind, Is.EqualTo(ShaderResourceKind.InputAttachment));
    }

    [Test]
    public void ResourceBinding_MixedResources_AllCreated()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
        var source = @"
shader MixedResTest {
    uniform mvp: mat4;
    cbuffer SceneData {
        viewProj: mat4;
    };
    texture albedo: sampler2D;
    sampler linearSampler;
    varying color: vec4;
    vertex vs {
    }
    fragment fs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "MixedResTest");

        Assert.That(module.Resources.Count, Is.EqualTo(5));
        Assert.That(module.Resources.Count(r => r.Kind == ShaderResourceKind.UniformBuffer), Is.EqualTo(2));
        Assert.That(module.Resources.Count(r => r.Kind == ShaderResourceKind.SampledImage), Is.EqualTo(1));
        Assert.That(module.Resources.Count(r => r.Kind == ShaderResourceKind.Sampler), Is.EqualTo(1));
        Assert.That(module.Resources.Count(r => r.Kind == ShaderResourceKind.InputAttachment), Is.EqualTo(1));
    }

    [Test]
    public void ResourceBinding_CBufferFieldTypes_CorrectlyResolved()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
        var source = @"
shader FieldTypeTest {
    cbuffer TypeTest {
        a: f32;
        b: vec2;
        c: vec3;
        d: vec4;
        e: mat4;
    };
    vertex vs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "FieldTypeTest");

        var typeTestStruct = module.Structs.FirstOrDefault(s => s.Name == "TypeTest");
        Assert.That(typeTestStruct, Is.Not.Null);
        Assert.That(typeTestStruct!.Fields.Count, Is.EqualTo(5));
        Assert.That(typeTestStruct.Fields[0].Name, Is.EqualTo("a"));
        Assert.That(typeTestStruct.Fields[1].Name, Is.EqualTo("b"));
        Assert.That(typeTestStruct.Fields[2].Name, Is.EqualTo("c"));
        Assert.That(typeTestStruct.Fields[3].Name, Is.EqualTo("d"));
        Assert.That(typeTestStruct.Fields[4].Name, Is.EqualTo("e"));
    }
}

#endregion

#region 错误处理和边界条件测试

[TestFixture]
public class ValkyrieErrorHandlingTests
{
    private ValkyrieShaderCompiler _compiler = null!;

    [SetUp]
    public void SetUp()
    {
        _compiler = new ValkyrieShaderCompiler();
    }

    [Test]
    public void ErrorHandling_EmptySource_CompilesWithoutError()
    {
        Assert.DoesNotThrow(() =>
        {
            _compiler.Compile("", "Empty", new ShaderCompileOptions());
        });
    }

    [Test]
    public void ErrorHandling_EmptySource_ValidatesTrue()
    {
        var result = _compiler.Validate("", out var errorMessage);
        Assert.That(result, Is.True);
    }

    [Test]
    public void ErrorHandling_ValidateStructOnly_ReturnsTrue()
    {
        var source = @"
struct OnlyStruct {
    x: f32;
    y: f32;
};
";
        var result = _compiler.Validate(source, out var errorMessage);
        Assert.That(result, Is.True, $"验证失败：{errorMessage}");
    }

    [Test]
    public void ErrorHandling_ShaderWithNoStages_CompilesWithoutError()
    {
        var source = @"
shader NoStage {
};
";
        Assert.DoesNotThrow(() =>
        {
            _compiler.Compile(source, "NoStage", new ShaderCompileOptions());
        });
    }

    [Test]
    public void ErrorHandling_SingleVertexShader_Compiles()
    {
        var source = @"
shader SingleVert {
    vertex vs {
    }
};
";
        var module = _compiler.Compile(source, "SingleVert", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Functions.Count, Is.EqualTo(1));
        Assert.That(module.Functions[0].Kind, Is.EqualTo(MicroFunctionKind.Vertex));
    }

    [Test]
    public void ErrorHandling_SingleFragmentShader_Compiles()
    {
        var source = @"
shader SingleFrag {
    fragment fs {
    }
};
";
        var module = _compiler.Compile(source, "SingleFrag", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Functions.Count, Is.EqualTo(1));
        Assert.That(module.Functions[0].Kind, Is.EqualTo(MicroFunctionKind.Fragment));
    }

    [Test]
    public void ErrorHandling_SingleComputeShader_Compiles()
    {
        var source = @"
shader SingleComp {
    compute cs {
    }
};
";
        var module = _compiler.Compile(source, "SingleComp", new ShaderCompileOptions());

        Assert.That(module, Is.Not.Null);
        Assert.That(module.Functions.Count, Is.EqualTo(1));
        Assert.That(module.Functions[0].Kind, Is.EqualTo(MicroFunctionKind.Compute));
    }

    [Test]
    public void ErrorHandling_EmptyCBuffer_Compiles()
    {
        var source = @"
shader EmptyCB {
    cbuffer EmptyBuffer {
    };
    vertex vs {
    }
};
";
        Assert.DoesNotThrow(() =>
        {
            _compiler.Compile(source, "EmptyCB", new ShaderCompileOptions());
        });
    }

    [Test]
    public void ErrorHandling_EmptyStruct_Compiles()
    {
        var source = @"
struct EmptyStruct {
};

shader EmptyStructShader {
    vertex vs {
    }
};
";
        Assert.DoesNotThrow(() =>
        {
            _compiler.Compile(source, "EmptyStructShader", new ShaderCompileOptions());
        });
    }

    [Test]
    public void ErrorHandling_SpirvValidator_InvalidData_ReturnsFalse()
    {
        var validator = new SpirvValidator();
        var invalidData = new byte[20];

        var result = validator.Validate(invalidData);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Count, Is.GreaterThan(0));
    }

    [Test]
    public void ErrorHandling_SpirvValidator_ShortData_ReturnsFalse()
    {
        var validator = new SpirvValidator();
        var shortData = new byte[10];

        var result = validator.Validate(shortData);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ErrorHandling_SpirvValidator_EmptyData_ReturnsFalse()
    {
        var validator = new SpirvValidator();
        var emptyData = Array.Empty<byte>();

        var result = validator.Validate(emptyData);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ErrorHandling_SpirvValidator_WrongMagic_ReturnsFalse()
    {
        var validator = new SpirvValidator();
        var wrongMagic = new byte[20];
        BinaryPrimitives.WriteUInt32LittleEndian(wrongMagic.AsSpan(0, 4), 0xDEADBEEFu);

        var result = validator.Validate(wrongMagic);

        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public void ErrorHandling_ShaderFormatHandler_NoCompiler_Throws()
    {
        var handler = new ShaderFormatHandler();
        var shaderData = new ShaderData
        {
            Name = "NoCompiler",
            Language = ShaderLanguage.Valkyrie,
            SourceCode = "shader X { vertex v {} };"
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.CompileAsync(shaderData, ShaderTarget.Spirv);
        });

        Assert.That(ex!.Message, Does.Contain("未设置着色器编译器"));
    }

    [Test]
    public void ErrorHandling_Lowering_VariousTypeNames_Resolved()
    {
        var lowering = new ValkyrieShaderLowering(new ShaderCompileOptions());
        var lexer = new ValkyrieLexer(ValkyrieLanguage.Shader);
        var source = @"
shader TypeResolveTest {
    uniform a: f32;
    uniform b: i32;
    uniform c: u32;
    uniform d: bool;
    vertex vs {
    }
};
";
        var tokens = lexer.Tokenize(source);
        var module = lowering.LowerFromTokens(tokens, "TypeResolveTest");

        Assert.That(module.Resources.Count, Is.EqualTo(4));
    }
}

#endregion

#region SPIR-V 生成器单元测试

[TestFixture]
public class SpirvGeneratorUnitTests
{
    [Test]
    public void Generator_EmptyModule_ProducesMinimalSpirv()
    {
        var module = new ShaderModuleIr
        {
            Name = "Minimal",
            Language = ShaderLanguage.Valkyrie,
            Target = ShaderTarget.Spirv
        };

        var generator = new SpirvGenerator();
        var spirv = generator.Generate(module);

        Assert.That(spirv, Is.Not.Null);
        Assert.That(spirv.Length, Is.GreaterThanOrEqualTo(20));

        var magic = BinaryPrimitives.ReadUInt32LittleEndian(spirv.AsSpan(0, 4));
        Assert.That(magic, Is.EqualTo(0x07230203u));
    }

    [Test]
    public void Generator_ModuleWithVertexEntryPoint_ContainsVertexExecutionModel()
    {
        var module = new ShaderModuleIr
        {
            Name = "VertOnly",
            Language = ShaderLanguage.Valkyrie,
            Target = ShaderTarget.Spirv
        };

        module.Functions.Add(new ShaderFunctionIr
        {
            Name = "vs_main",
            ReturnType = ShaderIrType.Void,
            IsEntryPoint = true,
            EntryPointModel = ShaderExecutionModel.Vertex
        });
        module.EntryPoints.Add(new ShaderEntryPointIr
        {
            Name = "vs_main",
            ExecutionModel = ShaderExecutionModel.Vertex,
            FunctionName = "vs_main"
        });

        var generator = new SpirvGenerator();
        var spirv = generator.Generate(module);
        var decoded = new SpirvDecoder(spirv).Decode();

        var entryPoints = decoded.Instructions.Where(i => i.Opcode == SpirvOpCode.OpEntryPoint).ToList();
        Assert.That(entryPoints.Count, Is.EqualTo(1));
        Assert.That(entryPoints[0].Operands[0], Is.EqualTo((uint)SpirvExecutionModel.Vertex));
    }

    [Test]
    public void Generator_ModuleWithFragmentEntryPoint_ContainsFragmentExecutionModel()
    {
        var module = new ShaderModuleIr
        {
            Name = "FragOnly",
            Language = ShaderLanguage.Valkyrie,
            Target = ShaderTarget.Spirv
        };

        module.Functions.Add(new ShaderFunctionIr
        {
            Name = "fs_main",
            ReturnType = ShaderIrType.Void,
            IsEntryPoint = true,
            EntryPointModel = ShaderExecutionModel.Fragment
        });
        module.EntryPoints.Add(new ShaderEntryPointIr
        {
            Name = "fs_main",
            ExecutionModel = ShaderExecutionModel.Fragment,
            FunctionName = "fs_main"
        });

        var generator = new SpirvGenerator();
        var spirv = generator.Generate(module);
        var decoded = new SpirvDecoder(spirv).Decode();

        var entryPoints = decoded.Instructions.Where(i => i.Opcode == SpirvOpCode.OpEntryPoint).ToList();
        Assert.That(entryPoints.Count, Is.EqualTo(1));
        Assert.That(entryPoints[0].Operands[0], Is.EqualTo((uint)SpirvExecutionModel.Fragment));
    }

    [Test]
    public void Generator_ModuleWithComputeEntryPoint_ContainsComputeExecutionModel()
    {
        var module = new ShaderModuleIr
        {
            Name = "CompOnly",
            Language = ShaderLanguage.Valkyrie,
            Target = ShaderTarget.Spirv
        };

        module.Functions.Add(new ShaderFunctionIr
        {
            Name = "cs_main",
            ReturnType = ShaderIrType.Void,
            IsEntryPoint = true,
            EntryPointModel = ShaderExecutionModel.GLCompute
        });
        module.EntryPoints.Add(new ShaderEntryPointIr
        {
            Name = "cs_main",
            ExecutionModel = ShaderExecutionModel.GLCompute,
            FunctionName = "cs_main"
        });

        var generator = new SpirvGenerator();
        var spirv = generator.Generate(module);
        var decoded = new SpirvDecoder(spirv).Decode();

        var entryPoints = decoded.Instructions.Where(i => i.Opcode == SpirvOpCode.OpEntryPoint).ToList();
        Assert.That(entryPoints.Count, Is.EqualTo(1));
        Assert.That(entryPoints[0].Operands[0], Is.EqualTo((uint)SpirvExecutionModel.GLCompute));
    }

    [Test]
    public void Generator_ModuleWithStruct_ContainsTypeStructInstruction()
    {
        var module = new ShaderModuleIr
        {
            Name = "StructGen",
            Language = ShaderLanguage.Valkyrie,
            Target = ShaderTarget.Spirv
        };

        var structIr = new ShaderStructIr { Name = "TestStruct" };
        structIr.Fields.Add(new ShaderStructFieldIr
        {
            Name = "pos",
            Type = ShaderIrType.Vec3(),
            Offset = 0
        });
        structIr.Fields.Add(new ShaderStructFieldIr
        {
            Name = "uv",
            Type = ShaderIrType.Vec2(),
            Offset = 12
        });
        module.Structs.Add(structIr);

        module.Functions.Add(new ShaderFunctionIr
        {
            Name = "vs",
            ReturnType = ShaderIrType.Void,
            IsEntryPoint = true,
            EntryPointModel = ShaderExecutionModel.Vertex
        });
        module.EntryPoints.Add(new ShaderEntryPointIr
        {
            Name = "vs",
            ExecutionModel = ShaderExecutionModel.Vertex,
            FunctionName = "vs"
        });

        var generator = new SpirvGenerator();
        var spirv = generator.Generate(module);
        var decoded = new SpirvDecoder(spirv).Decode();

        var structInstrs = decoded.Instructions.Count(i => i.Opcode == SpirvOpCode.OpTypeStruct);
        Assert.That(structInstrs, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Generator_ModuleWithMultipleEntryPoints_AllPresent()
    {
        var module = new ShaderModuleIr
        {
            Name = "MultiEntry",
            Language = ShaderLanguage.Valkyrie,
            Target = ShaderTarget.Spirv
        };

        foreach (var (name, model) in new[]
        {
            ("vs", ShaderExecutionModel.Vertex),
            ("fs", ShaderExecutionModel.Fragment),
            ("cs", ShaderExecutionModel.GLCompute)
        })
        {
            module.Functions.Add(new ShaderFunctionIr
            {
                Name = name,
                ReturnType = ShaderIrType.Void,
                IsEntryPoint = true,
                EntryPointModel = model
            });
            module.EntryPoints.Add(new ShaderEntryPointIr
            {
                Name = name,
                ExecutionModel = model,
                FunctionName = name
            });
        }

        var generator = new SpirvGenerator();
        var spirv = generator.Generate(module);
        var decoded = new SpirvDecoder(spirv).Decode();

        var entryPoints = decoded.Instructions.Where(i => i.Opcode == SpirvOpCode.OpEntryPoint).ToList();
        Assert.That(entryPoints.Count, Is.EqualTo(3));
    }

    [Test]
    public void Generator_RayTracingShader_AddsRayTracingCapability()
    {
        var module = new ShaderModuleIr
        {
            Name = "RayTrace",
            Language = ShaderLanguage.Valkyrie,
            Target = ShaderTarget.Spirv
        };

        module.Functions.Add(new ShaderFunctionIr
        {
            Name = "rg_main",
            ReturnType = ShaderIrType.Void,
            IsEntryPoint = true,
            EntryPointModel = ShaderExecutionModel.RayGenerationKHR
        });
        module.EntryPoints.Add(new ShaderEntryPointIr
        {
            Name = "rg_main",
            ExecutionModel = ShaderExecutionModel.RayGenerationKHR,
            FunctionName = "rg_main"
        });

        var generator = new SpirvGenerator();
        var spirv = generator.Generate(module);
        var decoded = new SpirvDecoder(spirv).Decode();

        var capabilities = decoded.Instructions
            .Where(i => i.Opcode == SpirvOpCode.OpCapability)
            .Select(i => (SpirvCapability)i.Operands[0])
            .ToList();

        Assert.That(capabilities, Does.Contain(SpirvCapability.Shader));
        Assert.That(capabilities, Does.Contain(SpirvCapability.RayTracingKHR));
    }
}

#endregion
