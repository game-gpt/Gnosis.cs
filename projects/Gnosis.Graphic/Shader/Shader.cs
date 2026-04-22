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
        ShaderLanguage language = ShaderLanguage.GgShader,
        ShaderTarget target = ShaderTarget.Spirv)
    {
        Name = name;
        Functions = functions;
        Bytecode = [];
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

public static class BuiltinShaderModules
{
    public static IShaderModule CreateUiShader()
    {
        var vertexFunc = new DelegateMicroFunction(
            "ui_vertex",
            MicroFunctionKind.Vertex,
            execute: (input, index) =>
            {
                return
                [
                    input[0],
                    input[1],
                    input[2],
                    1.0f,
                    input.Length > 3 ? input[3] : 1.0f,
                    input.Length > 4 ? input[4] : 1.0f,
                    input.Length > 5 ? input[5] : 1.0f,
                    input.Length > 6 ? input[6] : 1.0f
                ];
            }
        );

        var fragmentFunc = new DelegateMicroFunction(
            "ui_fragment",
            MicroFunctionKind.Fragment,
            execute: (interpolated, index) =>
            {
                return
                [
                    interpolated.Length > 4 ? interpolated[4] : 1.0f,
                    interpolated.Length > 5 ? interpolated[5] : 1.0f,
                    interpolated.Length > 6 ? interpolated[6] : 1.0f,
                    1.0f
                ];
            }
        );

        return new DelegateShaderModule("ui_uber", [vertexFunc, fragmentFunc]);
    }

    public static IShaderModule CreateMeshShader()
    {
        var vertexFunc = new DelegateMicroFunction(
            "mesh_vertex",
            MicroFunctionKind.Vertex,
            execute: (input, index) =>
            {
                var x = input[0];
                var y = input[1];
                var z = input[2];

                var r = input.Length > 3 ? input[3] : 1.0f;
                var g = input.Length > 4 ? input[4] : 1.0f;
                var b = input.Length > 5 ? input[5] : 1.0f;

                return [x, y, z, 1.0f, r, g, b];
            }
        );

        var fragmentFunc = new DelegateMicroFunction(
            "mesh_fragment",
            MicroFunctionKind.Fragment,
            execute: (interpolated, index) =>
            {
                var r = interpolated.Length > 4 ? interpolated[4] : 1.0f;
                var g = interpolated.Length > 5 ? interpolated[5] : 1.0f;
                var b = interpolated.Length > 6 ? interpolated[6] : 1.0f;

                return [r, g, b, 1.0f];
            }
        );

        return new DelegateShaderModule("mesh_default", [vertexFunc, fragmentFunc]);
    }
}
