using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

public delegate float[] MicroFunctionExecute(ReadOnlySpan<float> input, int index);

public sealed class DelegateMicroFunction : IMicroFunction
{
    public string Name { get; }
    public MicroFunctionKind Kind { get; }
    public IReadOnlyList<ShaderParameter> InputParameters { get; }
    public ShaderParameter OutputParameter { get; }
    public IReadOnlyList<ShaderUniform> Uniforms { get; }
    public IReadOnlyList<ShaderAttribute> Attributes { get; }
    public IReadOnlyList<ShaderSampler> Samplers { get; }

    public MicroFunctionExecute? Execute { get; set; }

    public DelegateMicroFunction(
        string name,
        MicroFunctionKind kind,
        MicroFunctionExecute? execute = null,
        IReadOnlyList<ShaderParameter>? inputParameters = null,
        ShaderParameter? outputParameter = null,
        IReadOnlyList<ShaderUniform>? uniforms = null,
        IReadOnlyList<ShaderAttribute>? attributes = null,
        IReadOnlyList<ShaderSampler>? samplers = null)
    {
        Name = name;
        Kind = kind;
        Execute = execute;
        InputParameters = inputParameters ?? new List<ShaderParameter>();
        OutputParameter = outputParameter ?? new ShaderParameter { Name = "output", Type = ShaderParameterType.Vec4 };
        Uniforms = uniforms ?? new List<ShaderUniform>();
        Attributes = attributes ?? new List<ShaderAttribute>();
        Samplers = samplers ?? new List<ShaderSampler>();
    }
}

public sealed class DelegateShaderModule : IShaderModule
{
    public string Name { get; }
    public IReadOnlyList<IMicroFunction> Functions { get; }
    public byte[] Bytecode { get; }
    public ShaderLanguage Language { get; }
    public ShaderTarget Target { get; }

    private readonly Dictionary<string, object> _uniforms = new();

    public DelegateShaderModule(
        string name,
        IReadOnlyList<IMicroFunction> functions,
        ShaderLanguage language = ShaderLanguage.Valkyrie,
        ShaderTarget target = ShaderTarget.Spirv,
        byte[]? bytecode = null)
    {
        Name = name;
        Functions = functions;
        Bytecode = bytecode ?? [];
        Language = language;
        Target = target;
    }

    public void SetUniform<T>(string name, T value) where T : struct
    {
        _uniforms[name] = value;
    }

    public T? GetUniform<T>(string name) where T : struct
    {
        if (_uniforms.TryGetValue(name, out var value) && value is T typed)
        {
            return typed;
        }
        return null;
    }

    public void Dispose()
    {
    }
}

/// <summary>
/// 内置着色器模块，通过 GGShader 编译管线加载 .shader 文件
/// </summary>
public static class BuiltinShaderModules
{
    #region 内部状态

    private static IShaderCompiler? _compiler;
    private static IShaderModule? _uiShader;
    private static IShaderModule? _meshShader;

    #endregion

    #region 初始化

    /// <summary>
    /// 设置着色器编译器实例
    /// </summary>
    public static void Initialize(IShaderCompiler compiler)
    {
        _compiler = compiler;
    }

    #endregion

    #region 内置着色器

    /// <summary>
    /// 创建 UI Uber Shader，通过 GGShader 编译管线编译 ui_uber.shader
    /// 支持 SDF 字体渲染、矩形、描边、阴影
    /// </summary>
    public static IShaderModule CreateUiShader()
    {
        if (_compiler != null)
        {
            var source = ReadShaderSource("ui_uber.shader");
            if (!string.IsNullOrEmpty(source))
            {
                return _compiler.Compile(source, "ui_uber", new ShaderCompileOptions());
            }
        }

        return CreateUiShaderFallback();
    }

    /// <summary>
    /// 创建默认网格着色器，通过 GGShader 编译管线编译 mesh_default.shader
    /// </summary>
    public static IShaderModule CreateMeshShader()
    {
        if (_compiler != null)
        {
            var source = ReadShaderSource("mesh_default.shader");
            if (!string.IsNullOrEmpty(source))
            {
                return _compiler.Compile(source, "mesh_default", new ShaderCompileOptions());
            }
        }

        return CreateMeshShaderFallback();
    }

    #endregion

    #region 回退实现

    /// <summary>
    /// UI Uber Shader 回退实现，当编译管线不可用时使用
    /// </summary>
    private static IShaderModule CreateUiShaderFallback()
    {
        var vertexFunc = new DelegateMicroFunction(
            "vs_main",
            MicroFunctionKind.Vertex
        );

        var fragmentFunc = new DelegateMicroFunction(
            "fs_main",
            MicroFunctionKind.Fragment,
            uniforms:
            [
                new ShaderUniform { Name = "sdf_spread", Type = ShaderUniformType.Float },
                new ShaderUniform { Name = "outline_width", Type = ShaderUniformType.Float },
                new ShaderUniform { Name = "outline_color", Type = ShaderUniformType.Float4 },
                new ShaderUniform { Name = "shadow_offset", Type = ShaderUniformType.Float2 },
                new ShaderUniform { Name = "shadow_width", Type = ShaderUniformType.Float },
                new ShaderUniform { Name = "shadow_color", Type = ShaderUniformType.Float4 },
                new ShaderUniform { Name = "style_id", Type = ShaderUniformType.Int },
                new ShaderUniform { Name = "smoothness", Type = ShaderUniformType.Float }
            ],
            samplers:
            [
                new ShaderSampler { Name = "sdf_atlas", Binding = 0, Type = ShaderSamplerType.Sampler2D }
            ]
        );

        return new DelegateShaderModule("ui_uber", [vertexFunc, fragmentFunc]);
    }

    /// <summary>
    /// 默认网格着色器回退实现，当编译管线不可用时使用
    /// </summary>
    private static IShaderModule CreateMeshShaderFallback()
    {
        var vertexFunc = new DelegateMicroFunction(
            "vs_main",
            MicroFunctionKind.Vertex
        );

        var fragmentFunc = new DelegateMicroFunction(
            "fs_main",
            MicroFunctionKind.Fragment
        );

        return new DelegateShaderModule("mesh_default", [vertexFunc, fragmentFunc]);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 读取着色器源文件，优先从嵌入资源读取，回退到文件系统
    /// </summary>
    private static string ReadShaderSource(string shaderFileName)
    {
        var resourceName = $"Gnosis.Graphic.shaders.{shaderFileName.Replace('/', '.').Replace('\\', '.')}";
        var assembly = typeof(BuiltinShaderModules).Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        var searchPaths = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "shaders", shaderFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "shaders", shaderFileName),
            Path.Combine(Directory.GetCurrentDirectory(), "projects", "Gnosis.Graphic", "shaders", shaderFileName)
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        return string.Empty;
    }

    #endregion
}
